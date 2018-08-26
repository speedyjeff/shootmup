using System;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Runtime.Api;
using shootMup.Common;

namespace shootMup.Bots
{
    // all columns must have the same type (float)
    // add new columns to the end
    // make sure to update the number of features value
    public class ModelDataSet
    {
        // environment

        [Column("0")]
        public float CenterAngle;
        [Column("1")]
        public float InZone;

        // player core stats

        [Column("2")]
        public float Health;
        [Column("3")]
        public float Sheld;
        [Column("4")]
        public float Z;
        [Column("5")]
        public float Primary;
        [Column("6")]
        public float PrimaryAmmo;
        [Column("7")]
        public float PrimaryClip;
        [Column("8")]
        public float Secondary;
        [Column("9")]
        public float SecondaryAmmo;
        [Column("10")]
        public float SecondaryClip;

        // proximity

        // ammo
        [Column("11")]
        public float Angle_1;
        [Column("12")]
        public float Distance_1;

        // Bandage
        [Column("13")]
        public float Angle_2;
        [Column("14")]
        public float Distance_2;

        // Helmet
        [Column("15")]
        public float Angle_3;
        [Column("16")]
        public float Distance_3;

        // Ak47
        [Column("17")]
        public float Angle_4;
        [Column("18")]
        public float Distance_4;

        // Shotgun
        [Column("19")]
        public float Angle_5;
        [Column("20")]
        public float Distance_5;

        // Pistol
        [Column("21")]
        public float Angle_6;
        [Column("22")]
        public float Distance_6;

        // Obstacle
        [Column("23")]
        public float Angle_7;
        [Column("24")]
        public float Distance_7;

        // Player
        [Column("25")]
        public float Angle_8;
        [Column("26")]
        public float Distance_8;

        // outcomes
        public float Action;
        public float MoveAngle;
        public float FaceAngle;
    }

    public static class ModelDataSetExtensions
    {
        public static int Features(this ModelDataSet data)
        {
            return 27;
        }

        public static float Feature(this ModelDataSet data, int column)
        {
            switch (column)
            {
                case 0: return data.CenterAngle;
                case 1: return data.InZone;
                case 2: return data.Health;
                case 3: return data.Sheld;
                case 4: return data.Z;
                case 5: return data.Primary;
                case 6: return data.PrimaryAmmo;
                case 7: return data.PrimaryClip;
                case 8: return data.Secondary;
                case 9: return data.SecondaryAmmo;
                case 10: return data.SecondaryClip;
                // ammo
                case 11: return data.Angle_1;
                case 12: return data.Distance_1;
                // bandage
                case 13: return data.Angle_2;
                case 14: return data.Distance_2;
                // helmet
                case 15: return data.Angle_3;
                case 16: return data.Distance_3;
                // ak47
                case 17: return data.Angle_4;
                case 18: return data.Distance_4;
                // shotgun
                case 19: return data.Angle_5;
                case 20: return data.Distance_5;
                // pistol
                case 21: return data.Angle_6;
                case 22: return data.Distance_6;
                // ostabcle
                case 23: return data.Angle_7;
                case 24: return data.Distance_7;
                // player
                case 25: return data.Angle_8;
                case 26: return data.Distance_8;
                // default
                default: throw new Exception("Unknown column : " + column);
            }
        }
    }

    public static class TrainingDataExtentions
    {
        public static ModelDataSet AsModelDataSet(this TrainingData data)
        {
            // transform to ModelDataSet and Normalize (0...1)

            var result = new ModelDataSet()
            {
                // core data
                CenterAngle = data.CenterAngle / 360f,
                InZone = data.InZone ? 1f : 0,
                Health = data.Health / (float)Constants.MaxHealth,
                Sheld = data.Sheld / (float)Constants.MaxSheld,
                Z = data.Z / Constants.Sky,
                Primary = Normalize(data.Primary),
                PrimaryAmmo = data.PrimaryAmmo >= Constants.MaxAmmo ? 1 : (float)data.PrimaryAmmo / (float)Constants.MaxAmmo,
                PrimaryClip = Normalize(data.Primary, data.PrimaryClip),
                Secondary = Normalize(data.Secondary),
                SecondaryAmmo = data.SecondaryAmmo >= Constants.MaxAmmo ? 1 : (float)data.SecondaryAmmo / (float)Constants.MaxAmmo,
                SecondaryClip = Normalize(data.Secondary, data.SecondaryClip),
            };

            // outcome (not normalize)
            result.Action = (float)data.Action;
            result.FaceAngle = data.Angle;
            result.MoveAngle = Collision.CalculateAngleFromPoint(0, 0, data.Xdelta, data.Ydelta);

            // proximity
            foreach (var elem in data.Proximity)
            {
                switch (elem.Name)
                {
                    case "Ammo":
                        result.Angle_1 = elem.Angle / 360f;
                        result.Distance_1 = elem.Distance / (float)Constants.ProximityViewWidth;
                        break;
                    case "Bandage":
                        result.Angle_2 = elem.Angle / 360f;
                        result.Distance_2 = elem.Distance / (float)Constants.ProximityViewWidth;
                        break;
                    case "Helmet":
                        result.Angle_3 = elem.Angle / 360f;
                        result.Distance_3 = elem.Distance / (float)Constants.ProximityViewWidth;
                        break;
                    case "AK47":
                        result.Angle_4 = elem.Angle / 360f;
                        result.Distance_4 = elem.Distance / (float)Constants.ProximityViewWidth;
                        break;
                    case "Shotgun":
                        result.Angle_5 = elem.Angle / 360f;
                        result.Distance_5 = elem.Distance / (float)Constants.ProximityViewWidth;
                        break;
                    case "Pistol":
                        result.Angle_6 = elem.Angle / 360f;
                        result.Distance_6 = elem.Distance / (float)Constants.ProximityViewWidth;
                        break;
                    case "Obstacle":
                        result.Angle_7 = elem.Angle / 360f;
                        result.Distance_7 = elem.Distance / (float)Constants.ProximityViewWidth;
                        break;
                    case "Player":
                        result.Angle_8 = elem.Angle / 360f;
                        result.Distance_8 = elem.Distance / (float)Constants.ProximityViewWidth;
                        break;
                    default:
                        throw new Exception("Unknown proximity element type : " + elem.Name);
                }
            }

            return result;
        }

        #region private
        private static float SegmentAngle(float angle, float quad)
        {
            // break the angle down into a smaller set of possible values (quardanents)
            var delta = angle % quad;
            if (delta < (quad / 2))
                return angle - delta;
            else
                return angle + (quad - delta);
        }

        private static float Normalize(string input)
        {
            // normalize name
            if (string.IsNullOrWhiteSpace(input)) return 0;
            float count = 3;

            switch (input.ToLower())
            {
                case "ak47": return 1f / count;
                case "shotgun": return 2f / count;
                case "pistol": return 3f / count;
                default: throw new Exception("Unknown input : " + input);
            }
        }

        private static float Normalize(string input, int clip)
        {
            // normalize clip capacity
            if (string.IsNullOrWhiteSpace(input) || clip == 0) return 0;

            // TODO ugh hard coded values
            //   these values come from the ClipCapcity for each gun
            switch (input.ToLower())
            {
                case "ak47": return clip / 20f;
                case "shotgun": return clip / 2f;
                case "pistol": return clip / 6f;
                default: throw new Exception("Unknown input : " + input);
            }
        }
        #endregion
    }

    public class ModelDataSetPrediction
    {
        [ColumnName("Score")]
        public float Value;
    }
}
