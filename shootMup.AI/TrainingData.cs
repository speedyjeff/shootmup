using System;
using System.Collections.Generic;
using System.Text.Json;

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

        public string ToJson() => JsonSerializer.Serialize(this, TrainingJson.SerializerOptions);
    }

    internal static class TrainingJson
    {
        internal static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
    }
}
