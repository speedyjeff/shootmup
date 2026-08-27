using engine.Common;
using engine.Common.Entities;
using shootMup.Common;
using System.Collections.Generic;
using System.Linq;

namespace shootMup.Bots.RL
{
    // Builds the 26-float state vector by reusing the existing ModelDataSet
    // feature extraction (already normalized to 0..1). Keeps RL code decoupled
    // from ML.NET types.
    public static class RLState
    {
        public const int FeatureCount = 26;

        public static float[] Build(Player self, List<Element> elements, float angleToCenter, bool inZone)
        {
            var pname = "";
            var pammo = 0;
            var pclip = 0;
            var sname = "";
            var sammo = 0;
            var sclip = 0;

            if (self.Primary is RangeWeapon pw)
            {
                pname = pw.GetType().Name;
                pammo = pw.Ammo;
                pclip = pw.Clip;
            }
            if (self.Secondary != null && self.Secondary.Length == 1 && self.Secondary[0] is RangeWeapon sw)
            {
                sname = sw.GetType().Name;
                sammo = sw.Ammo;
                sclip = sw.Clip;
            }

            var proximity = AITraining.ComputeProximity(self, elements).Values.ToList();
            // ModelDataSet.AsModelDataSet expects older naming for some pickups.
            // Map current engine type names to the legacy names it understands.
            foreach (var p in proximity)
            {
                switch (p.Name)
                {
                    case "Shield": p.Name = "Helmet"; break;
                    case "Health": p.Name = "Bandage"; break;
                }
            }

            var data = new TrainingData()
            {
                CenterAngle = angleToCenter,
                InZone = inZone,
                Health = self.Health,
                Shield = self.Shield,
                Z = self.Z,
                Primary = pname,
                PrimaryAmmo = pammo,
                PrimaryClip = pclip,
                Secondary = sname,
                SecondaryAmmo = sammo,
                SecondaryClip = sclip,
                Proximity = proximity
            };

            ModelDataSet m;
            try
            {
                m = data.AsModelDataSet();
            }
            catch (System.Exception)
            {
                // Unknown pickup type — degrade gracefully so training doesn't crash.
                data.Proximity = new List<ElementProximity>();
                m = data.AsModelDataSet();
            }
            var v = new float[FeatureCount];
            for (int i = 0; i < FeatureCount; i++) v[i] = m.Feature(i);
            return v;
        }
    }
}
