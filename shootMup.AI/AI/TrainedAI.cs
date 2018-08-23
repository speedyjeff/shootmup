using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace shootMup.Common
{
    public class TrainedAI : AI
    {
        public TrainedAI() : base()
        {
            // never record training data for these AI
            RecordTraining = false;
        }

        static TrainedAI()
        { 
            // get models
            ActionModel = Model.Load(Path.Combine("Model", "Prebuilt", "action.model"));
            XYModel = Model.Load(Path.Combine("Model", "Prebuilt", "xy.model"));
            AngleModel = Model.Load(Path.Combine("Model", "Prebuilt", "angle.model"));            
        }

        public override ActionEnum Action(List<Element> elements, float angleToCenter, bool inZone, ref float xdelta, ref float ydelta, ref float angle)
        {
            // construct a view of the current world
            var data = new TrainingData()
            {
                // core data
                CenterAngle = angleToCenter,
                InZone = inZone,
                Health = Health,
                Sheld = Sheld,
                Z = Z,
                Primary = Primary != null ? Primary.GetType().Name : "",
                PrimaryAmmo = Primary != null ? Primary.Ammo : 0,
                PrimaryClip = Primary != null ? Primary.Clip : 0,
                Secondary = Secondary != null ? Secondary.GetType().Name : "",
                SecondaryAmmo = Secondary != null ? Secondary.Ammo : 0,
                SecondaryClip = Secondary != null ? Secondary.Clip : 0
            };
            data.Proximity = AITraining.ComputeProximity(this, elements).Values.ToList();

            // use the model to predict its actions
            var modeldataset = data.AsModelDataSet();
            int iAction = 0;
            lock (ActionModel)
            {
                iAction = (int)ActionModel.Predict(modeldataset);
                angle = AngleModel.Predict(modeldataset);
                XYModel.Predict(modeldataset, out xdelta, out ydelta);
            }

            // do some sanity checking...
            if (iAction < 0 || iAction >= (int)ActionEnum.ZoneDamage) throw new Exception("Unknown action : " + iAction);
            while (Math.Abs(xdelta) + Math.Abs(ydelta) > 1.00001)
            {
                if (xdelta > ydelta)
                {
                    if (xdelta < 0) xdelta += 0.001f;
                    else xdelta -= 0.001f;
                }
                else
                {
                    if (ydelta < 0) ydelta += 0.001f;
                    else ydelta -= 0.001f;
                }
            }
            if (angle < 0) angle *= -1;
            angle = angle % 360;

            return (ActionEnum)iAction;
        }

        #region private
        private static Model AngleModel;
        private static Model XYModel;
        private static Model ActionModel;
        #endregion
    }
}
