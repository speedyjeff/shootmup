using System;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Runtime.Api;

namespace shootMup.Common
{
    // all columns must have the same type (float)
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

        [Column("35")]
        public float Action;

        [Column("36")]
        public float MoveAngle;

        [Column("37")]
        public float FaceAngle;
    }

    public class ModelDataSetPrediction
    {
        [ColumnName("Score")]
        public float Value;
    }
}
