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
            var sb = new StringBuilder(1024);

            sb.AppendFormat("{{ \"CenterAngle\":{0},", CenterAngle);
            sb.AppendFormat("\"InZone\":{0},", InZone ? "true" : "false");

            if (Proximity != null && Proximity.Count > 0)
            {

                sb.Append("\"Proximity\":[");
                for (int i = 0; i < Proximity.Count; i++)
                {
                    sb.AppendFormat("{{\"Id\":{0},\"Name\":\"{1}\",\"Angle\":{2},\"Distance\":{3}}}",
                        Proximity[i].Id,
                        Proximity[i].Name,
                        Proximity[i].Angle,
                        Proximity[i].Distance);
                    if (i <= (Proximity.Count - 1)) sb.Append(',');
                }
                sb.Append("],");
            }

            sb.AppendFormat("\"Health\":{0},", Health);
            sb.AppendFormat("\"Shield\":{0},", Shield);
            sb.AppendFormat("\"Z\":{0},", Z);
            sb.AppendFormat("\"Primary\":\"{0}\",", Primary);
            sb.AppendFormat("\"PrimaryClip\":{0},", PrimaryClip);
            sb.AppendFormat("\"PrimaryAmmo\":{0},", PrimaryAmmo);
            sb.AppendFormat("\"Secondary\":\"{0}\",", Secondary);
            sb.AppendFormat("\"SecondaryClip\":{0},", SecondaryClip);
            sb.AppendFormat("\"SecondaryAmmo\":{0},", SecondaryAmmo);
            sb.AppendFormat("\"Action\":{0},", Action);
            sb.AppendFormat("\"Xdelta\":{0},", Xdelta);
            sb.AppendFormat("\"Ydelta\":{0},", Ydelta);
            sb.AppendFormat("\"Angle\":{0},", Angle);
            sb.AppendFormat("\"Result\":{0} }}", Result ? "true" : "false");

            return sb.ToString();
        }
    }
}
