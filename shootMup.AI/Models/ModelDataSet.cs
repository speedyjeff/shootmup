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
        public float Shield;
        [Column("4")]
        public float Primary;
        [Column("5")]
        public float PrimaryAmmo;
        [Column("6")]
        public float PrimaryClip;
        [Column("7")]
        public float Secondary;
        [Column("8")]
        public float SecondaryAmmo;
        [Column("9")]
        public float SecondaryClip;

        // proximity

        // ammo
        [Column("10")]
        public float Angle_1;
        [Column("11")]
        public float Distance_1;

        // Bandage
        [Column("12")]
        public float Angle_2;
        [Column("13")]
        public float Distance_2;

        // Helmet
        [Column("14")]
        public float Angle_3;
        [Column("15")]
        public float Distance_3;

        // Ak47
        [Column("16")]
        public float Angle_4;
        [Column("17")]
        public float Distance_4;

        // Shotgun
        [Column("18")]
        public float Angle_5;
        [Column("19")]
        public float Distance_5;

        // Pistol
        [Column("20")]
        public float Angle_6;
        [Column("21")]
        public float Distance_6;

        // Obstacle
        [Column("22")]
        public float Angle_7;
        [Column("23")]
        public float Distance_7;

        // Player
        [Column("24")]
        public float Angle_8;
        [Column("25")]
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
            return 26;
        }

        public static float Feature(this ModelDataSet data, int column)
        {
            switch (column)
            {
                case 0: return data.CenterAngle;
                case 1: return data.InZone;
                case 2: return data.Health;
                case 3: return data.Shield;
                case 4: return data.Primary;
                case 5: return data.PrimaryAmmo;
                case 6: return data.PrimaryClip;
                case 7: return data.Secondary;
                case 8: return data.SecondaryAmmo;
                case 9: return data.SecondaryClip;
                // ammo
                case 10: return data.Angle_1;
                case 11: return data.Distance_1;
                // bandage
                case 12: return data.Angle_2;
                case 13: return data.Distance_2;
                // helmet
                case 14: return data.Angle_3;
                case 15: return data.Distance_3;
                // ak47
                case 16: return data.Angle_4;
                case 17: return data.Distance_4;
                // shotgun
                case 18: return data.Angle_5;
                case 19: return data.Distance_5;
                // pistol
                case 20: return data.Angle_6;
                case 21: return data.Distance_6;
                // ostabcle
                case 22: return data.Angle_7;
                case 23: return data.Distance_7;
                // player
                case 24: return data.Angle_8;
                case 25: return data.Distance_8;
                // default
                default: throw new Exception("Unknown column : " + column);
            }
        }

        public static string Name(this ModelDataSet data, int column)
        {
            switch (column)
            {
                case 0: return "CenterAngle";
                case 1: return "InZone";
                case 2: return "Health";
                case 3: return "Shield";
                case 4: return "Primary";
                case 5: return "PrimaryAmmo";
                case 6: return "PrimaryClip";
                case 7: return "Secondary";
                case 8: return "SecondaryAmmo";
                case 9: return "SecondaryClip";
                // ammo
                case 10: return "Angle_1";
                case 11: return "Distance_1";
                // bandage
                case 12: return "Angle_2";
                case 13: return "Distance_2";
                // helmet
                case 14: return "Angle_3";
                case 15: return "Distance_3";
                // ak47
                case 16: return "Angle_4";
                case 17: return "Distance_4";
                // shotgun
                case 18: return "Angle_5";
                case 19: return "Distance_5";
                // pistol
                case 20: return "Angle_6";
                case 21: return "Distance_6";
                // ostabcle
                case 22: return "Angle_7";
                case 23: return "Distance_7";
                // player
                case 24: return "Angle_8";
                case 25: return "Distance_8";
                // default
                default: throw new Exception("Unknown column : " + column);
            }
        }

        public static string ToJson(this ModelDataSet data)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(data);
        }

        public static int ComputeHash(this ModelDataSet data)
        {
            var json = data.ToJson();
            return json.GetHashCode();
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
                CenterAngle = Normalize(data.CenterAngle, 360f),
                InZone = data.InZone ? 1f : 0,
                Health = Normalize(data.Health, (float)Constants.MaxHealth),
                Shield = Normalize(data.Shield, (float)Constants.MaxShield),
                Primary = Normalize(data.Primary),
                PrimaryAmmo = data.PrimaryAmmo >= Constants.MaxAmmo ? 1 : Normalize((float)data.PrimaryAmmo, (float)Constants.MaxAmmo),
                PrimaryClip = Normalize(data.Primary, data.PrimaryClip),
                Secondary = Normalize(data.Secondary),
                SecondaryAmmo = data.SecondaryAmmo >= Constants.MaxAmmo ? 1 : Normalize((float)data.SecondaryAmmo, (float)Constants.MaxAmmo),
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
                        result.Angle_1 = Normalize(elem.Angle, 360f);
                        result.Distance_1 = Normalize(elem.Distance, (float)Constants.ProximityViewWidth);
                        break;
                    case "Bandage":
                        result.Angle_2 = Normalize(elem.Angle, 360f);
                        result.Distance_2 = Normalize(elem.Distance, (float)Constants.ProximityViewWidth);
                        break;
                    case "Helmet":
                        result.Angle_3 = Normalize(elem.Angle, 360f);
                        result.Distance_3 = Normalize(elem.Distance, (float)Constants.ProximityViewWidth);
                        break;
                    case "AK47":
                        result.Angle_4 = Normalize(elem.Angle, 360f);
                        result.Distance_4 = Normalize(elem.Distance, (float)Constants.ProximityViewWidth);
                        break;
                    case "Shotgun":
                        result.Angle_5 = Normalize(elem.Angle, 360f);
                        result.Distance_5 = Normalize(elem.Distance, (float)Constants.ProximityViewWidth);
                        break;
                    case "Pistol":
                        result.Angle_6 = Normalize(elem.Angle, 360f);
                        result.Distance_6 = Normalize(elem.Distance, (float)Constants.ProximityViewWidth);
                        break;
                    case "Obstacle":
                        result.Angle_7 = Normalize(elem.Angle, 360f);
                        result.Distance_7 = Normalize(elem.Distance, (float)Constants.ProximityViewWidth);
                        break;
                    case "Player":
                        result.Angle_8 = Normalize(elem.Angle, 360f);
                        result.Distance_8 = Normalize(elem.Distance, (float)Constants.ProximityViewWidth);
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
                case "ak47": return Normalize(1f, (float)count);
                case "shotgun": return Normalize(2f, (float)count);
                case "pistol": return Normalize(3f, (float)count);
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
                case "ak47": return Normalize((float)clip, 20f);
                case "shotgun": return Normalize((float)clip, 2f);
                case "pistol": return Normalize((float)clip, 6f);
                default: throw new Exception("Unknown input : " + input);
            }
        }

        private static float Normalize(float value, float max)
        {
            return value / max;
            //return (float)Math.Round(value / max, 1);
        }
        #endregion
    }

    public class ModelDataSetPrediction
    {
        [ColumnName("Score")]
        public float Score;
    }
}
