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
        [ColumnName("Primary")]
        public string Primary;

        [Column("6")]
        public float PrimaryAmmo;

        [Column("7")]
        public float PrimaryClip;

        [Column("8")]
        [ColumnName("Secondary")]
        public string Secondary;

        [Column("9")]
        public float SecondaryAmmo;

        [Column("10")]
        public float SecondaryClip;

        // proximity

        [Column("11")]
        [ColumnName("Ammo")]
        public string Name_1;
        [Column("12")]
        public float Angle_1;
        [Column("13")]
        public float Distance_1;

        [Column("14")]
        [ColumnName("Bandage")]
        public string Name_2;
        [Column("15")]
        public float Angle_2;
        [Column("16")]
        public float Distance_2;

        [Column("17")]
        [ColumnName("Helmet")]
        public string Name_3;
        [Column("18")]
        public float Angle_3;
        [Column("19")]
        public float Distance_3;

        [Column("20")]
        [ColumnName("Ak47")]
        public string Name_4;
        [Column("21")]
        public float Angle_4;
        [Column("22")]
        public float Distance_4;

        [Column("23")]
        [ColumnName("Shotgun")]
        public string Name_5;
        [Column("24")]
        public float Angle_5;
        [Column("25")]
        public float Distance_5;

        [Column("26")]
        [ColumnName("Pistol")]
        public string Name_6;
        [Column("27")]
        public float Angle_6;
        [Column("28")]
        public float Distance_6;

        [Column("29")]
        [ColumnName("Obstacle")]
        public string Name_7;
        [Column("30")]
        public float Angle_7;
        [Column("31")]
        public float Distance_7;

        [Column("32")]
        [ColumnName("Player")]
        public string Name_8;
        [Column("33")]
        public float Angle_8;
        [Column("34")]
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
            return 35;
        }

        public static int Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;

            switch(input.ToLower())
            {
                case "ak47": return 1;
                case "shotgun": return 2;
                case "pistol": return 3;
                default: throw new Exception("Unknown input : " + input);
            }
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
                case 5: return Normalize(data.Primary);
                case 6: return data.PrimaryAmmo;
                case 7: return data.PrimaryClip;
                case 8: return Normalize(data.Secondary);
                case 9: return data.SecondaryAmmo;
                case 10: return data.SecondaryClip;
                // ammo
                case 11: return 0;
                case 12: return data.Angle_1;
                case 13: return data.Distance_1;
                // bandage
                case 14: return 0;
                case 15: return data.Angle_2;
                case 16: return data.Distance_2;
                // helmet
                case 17: return 0;
                case 18: return data.Angle_3;
                case 19: return data.Distance_3;
                // ak47
                case 20: return 0;
                case 21: return data.Angle_4;
                case 22: return data.Distance_4;
                // shotgun
                case 23: return 0;
                case 24: return data.Angle_5;
                case 25: return data.Distance_5;
                // pistol
                case 26: return 0;
                case 27: return data.Angle_6;
                case 28: return data.Distance_6;
                // ostabcle
                case 29: return 0;
                case 30: return data.Angle_7;
                case 31: return data.Distance_7;
                // player
                case 32: return 0;
                case 33: return data.Angle_8;
                case 34: return data.Distance_8;
                // default
                default: throw new Exception("Unknown column : " + column);
            }
        }
    }

    public static class TrainingDataExtentions
    {
        public static ModelDataSet AsModelDataSet(this TrainingData data)
        {
            var result = new ModelDataSet()
            {
                // core data
                CenterAngle = data.CenterAngle,
                InZone = data.InZone ? 1f : 0,
                Health = data.Health,
                Sheld = data.Sheld,
                Z = data.Z,
                Primary = data.Primary,
                PrimaryAmmo = data.PrimaryAmmo,
                PrimaryClip = data.PrimaryClip,
                Secondary = data.Secondary,
                SecondaryAmmo = data.SecondaryAmmo,
                SecondaryClip = data.SecondaryClip,
            };

            // outcome
            result.Action = (int)data.Action;
            result.FaceAngle = data.Angle;
            result.MoveAngle = Collision.CalculateAngleFromPoint(0, 0, data.Xdelta, data.Ydelta);

            // proximity
            foreach (var elem in data.Proximity)
            {
                switch (elem.Name)
                {
                    case "Ammo":
                        result.Name_1 = elem.Name;
                        result.Angle_1 = elem.Angle;
                        result.Distance_1 = elem.Distance;
                        break;
                    case "Bandage":
                        result.Name_2 = elem.Name;
                        result.Angle_2 = elem.Angle;
                        result.Distance_2 = elem.Distance;
                        break;
                    case "Helmet":
                        result.Name_3 = elem.Name;
                        result.Angle_3 = elem.Angle;
                        result.Distance_3 = elem.Distance;
                        break;
                    case "AK47":
                        result.Name_4 = elem.Name;
                        result.Angle_4 = elem.Angle;
                        result.Distance_4 = elem.Distance;
                        break;
                    case "Shotgun":
                        result.Name_5 = elem.Name;
                        result.Angle_5 = elem.Angle;
                        result.Distance_5 = elem.Distance;
                        break;
                    case "Pistol":
                        result.Name_6 = elem.Name;
                        result.Angle_6 = elem.Angle;
                        result.Distance_6 = elem.Distance;
                        break;
                    case "Obstacle":
                        result.Name_7 = elem.Name;
                        result.Angle_7 = elem.Angle;
                        result.Distance_7 = elem.Distance;
                        break;
                    case "Player":
                        result.Name_8 = elem.Name;
                        result.Angle_8 = elem.Angle;
                        result.Distance_8 = elem.Distance;
                        break;
                    default:
                        throw new Exception("Unknown proximity element type : " + elem.Name);
                }
            }

            return result;
        }
    }

    public class ModelDataSetPrediction
    {
        [ColumnName("Score")]
        public float Value;
    }
}
