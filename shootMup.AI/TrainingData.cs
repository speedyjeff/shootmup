using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Bots
{
    // this structure is stored on disk, so changes need to be made carefully
    public class TrainingData
    {
        // environment
        public float CenterAngle;
        public bool InZone;
        public List<ElementProximity> Proximity;

        // player details
        public float Health;
        public float Shield;
        public float Z;
        public string Primary;
        public int PrimaryClip;
        public int PrimaryAmmo;
        public string Secondary;
        public int SecondaryAmmo;
        public int SecondaryClip;

        // action
        public int Action;
        public float Xdelta;
        public float Ydelta;
        public float Angle;

        // success factor
        public bool Result;

        public string ToJson()
        {
            var proximity = "[]";
            if (Proximity != null && Proximity.Count > 0)
            {
                var sb = new StringBuilder(1024);
                sb.Append('[');
                for(int i=0; i<Proximity.Count; i++)
                {
                    sb.AppendFormat("{{\"Id\":{0},\"Name\":\"{1}\",\"Angle\":{2},\"Distance\":{3}}}",
                        Proximity[i].Id,
                        Proximity[i].Name,
                        Proximity[i].Angle,
                        Proximity[i].Distance);
                    if (i <= (Proximity.Count - 1)) sb.Append(',');
                }
                sb.Append(']');
                proximity = sb.ToString();
            }

            return String.Format("{{\"CenterAngle\":{0}\",\"InZone\":{1},\"Proximity\":{2},\"Health\":{3},\"Shield\":{4},\"Z\":{5},\"Primary\":\"{6}\",\"PrimaryClip\":{7},\"PrimaryAmmo\":{8},\"Secondary\":\"{9}\",\"SecondaryAmmo\":{10},\"SecondaryClip\":{11},\"Action\":{12},\"Xdelta\":{13},\"Ydelta\":{14},\"Angle\":{15},\"Result\":{16}}}",
                            CenterAngle,
                            InZone,
                            proximity,
                            Health,
                            Shield,
                            Z,
                            Primary,
                            PrimaryClip,
                            PrimaryAmmo,
                            Secondary,
                            SecondaryClip,
                            SecondaryAmmo,
                            Action,
                            Xdelta,
                            Ydelta,
                            Angle,
                            Result);
        }
    }
}
