using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace shootMup.Common
{
    public class ElementProximity
    {
        public int Id;
        public string Name;
        public float Angle;
        public float Distance;
    }

    // this structure is stored on disk, so changes need to be made carefully
    public class TrainingData
    {
        // environment
        public float CenterAngle;
        public bool InZone;
        public List<ElementProximity> Proximity;

        // player details
        public float Health;
        public float Sheld;
        public float Z;
        public string Primary;
        public int PrimaryClip;
        public int PrimaryAmmo;
        public string Secondary;
        public int SecondaryAmmo;
        public int SecondaryClip;

        // action
        public ActionEnum Action;
        public float Xdelta;
        public float Ydelta;
        public float Angle;

        // success factor
        public bool Result;
    }

    public class AITraining
    {
        static AITraining()
        {
            Data = new Dictionary<int, TrainingData>();
            Output = new Dictionary<int, StreamWriter>();
            Start = DateTime.Now;

#if SMALL_ENCODING
            SHA = SHA256.Create();
#endif
        }

        public const string TrainingPath = "training";

        public static Dictionary<Type, ElementProximity> ComputeProximity(Player self, List<Element> elements)
        {
            var closest = new Dictionary<Type, ElementProximity>();

            foreach (var elem in elements)
            {
                if (elem.Id == self.Id) continue;

                ElementProximity proximity = null;
                Type type = elem.GetType();

                // consolidate player types 
                if (elem is Player) type = typeof(Player);
                // consolidate obstacle types
                if (elem is Obstacle) type = typeof(Obstacle);

                if (!closest.TryGetValue(type, out proximity))
                {
                    proximity = new ElementProximity()
                    {
                        Name = type.Name,
                        Id = elem.Id,
                        Distance = float.MaxValue
                    };
                    closest.Add(type, proximity);
                }

                // calculate the distance between thees
                var distance = Collision.DistanceBetweenPoints(self.X, self.Y, elem.X, elem.Y);
                var angle = Collision.CalculateAngleFromPoint(self.X, self.Y, elem.X, elem.Y);

                // retain only the closest
                if (distance < proximity.Distance)
                {
                    proximity.Id = elem.Id;
                    proximity.Angle = angle;
                    proximity.Distance = distance;
                }
            }

            return closest;
        }

        public static void CaptureBefore(Player player, List<Element> elements, float angleToCenter, bool inZone)
        {
            var data = GetData(player);

            // capture the angle to the center
            data.CenterAngle = angleToCenter;

            // capture the user health, sheld, weapon status, inzone, Z
            data.Angle = player.Angle;
            data.Health = player.Health;
            data.InZone = inZone;
            data.Primary = player.Primary != null ? player.Primary.GetType().Name : "";
            data.PrimaryAmmo = player.Primary != null ? player.Primary.Ammo : 0;
            data.PrimaryClip = player.Primary != null ? player.Primary.Clip : 0;
            data.Secondary = player.Secondary != null ? player.Secondary.GetType().Name : "";
            data.SecondaryAmmo = player.Secondary != null ? player.Secondary.Ammo : 0;
            data.SecondaryClip = player.Secondary != null ? player.Secondary.Clip : 0;
            data.Sheld = player.Sheld;
            data.Z = player.Z;

            // capture what the user sees
            data.Proximity = AITraining.ComputeProximity(player, elements).Values.ToList();
        }

        public static void CaptureAfter(Player player, ActionEnum action, float xdelta, float ydelta, float angle, bool result)
        {
            var data = GetData(player);

            data.Action = action;
            data.Result = result;
            data.Xdelta = xdelta;
            data.Ydelta = ydelta;

            var output = GetOutput(player);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            output.WriteLine(json);
            output.Flush();
        }

        public static void CaptureWinners(string[] winners)
        {
            StreamWriter output = null;
            lock (Output)
            {
                if (!Output.TryGetValue(-1, out output))
                {
                    output = File.CreateText(Path.Combine(TrainingPath, string.Format("{0:yyyy-MM-dd_hh-mm-ss}.winner", Start)));
                    Output.Add(-1, output);
                }
            }
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(winners);
            output.WriteLine(json);
        }

        public static IEnumerable<TrainingData> GetTrainingData()
        {
            // enumerate the 'right' files and return them

            // gather all the humans and the top winning AI
            var map = new HashSet<string>();
            foreach (var file in Directory.GetFiles(TrainingPath))
            {
                if (file.EndsWith(".winner"))
                {
                    var json = File.ReadAllText(file);
                    var prefix = file.Substring(0, file.IndexOf('.'));
                    map.Add(prefix + "." + "You");
                    // open the file and build a map of files to consider
                    var start = json[0] == '[' ? 1 : 0;
                    var end = 0;
                    while(start < json.Length)
                    {
                        end = json.IndexOf(',', start);

                        if (end < 0) end = json.Length;

                        var i1 = json.IndexOf('[', start);
                        var i2 = json.IndexOf(']', start);

                        // "name [#]"
                        if (i1 > 0 && i2 > 0)
                        {
                            var name = json.Substring(start+1, i1-start-1).Trim();
                            var number = json.Substring(i1+1, i2-i1-1);

                            if (Convert.ToInt32(number) > 0)
                            {
                                map.Add(prefix + "." + name);
                            }
                        }

                        // advance
                        start = end + 1;
                    }
                }
            }

            // return files that were successful and for actions that made sense
            foreach (var file in Directory.GetFiles(TrainingPath))
            {
                if (map.Contains(file))
                {
                    foreach (var json in File.ReadAllLines(file))
                    {
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TrainingData>(json);
                        if (data.Result)
                        {
                            yield return data;
                        }
                    }
                }
            }
        }

#region private
        private static Dictionary<int, TrainingData> Data;
        private static Dictionary<int, StreamWriter> Output;
        private static DateTime Start;

        public static TrainingData GetData(Player player)
        {
            lock (Data)
            {
                TrainingData data = null;
                if (!Data.TryGetValue(player.Id, out data))
                {
                    data = new TrainingData();
                    Data.Add(player.Id, data);
                }
                return data;
            }
        }

        public static StreamWriter GetOutput(Player player)
        {
            lock (Output)
            {
                StreamWriter output = null;
                if (!Output.TryGetValue(player.Id, out output))
                {
                    if (!Directory.Exists(TrainingPath)) Directory.CreateDirectory(TrainingPath);
                    output = File.CreateText(Path.Combine(TrainingPath, string.Format("{0:yyyy-MM-dd_hh-mm-ss}.{1}", Start, player.Name)));
                    Output.Add(player.Id, output);
                }
                return output;
            }
        }

#if SMALL_ENCODING
        private static SHA256 SHA;

        struct Identity
        {
            public string Id;
            public long Hash;
            public string Encoded;
        }

        private static string CreateEncoding()
        {
            return "";
        }

        private static long ComputeHash(long[] values)
        {
            long hash = 17;
            foreach(var v in values)
            {
                hash = (hash * 31) + v;
            }

            return hash;
        }

        private static string ComputeHash(string encoded)
        {
            var buffer = System.Text.Encoding.ASCII.GetBytes(encoded);
            var hash = SHA.ComputeHash(buffer);
            return BitConverter.ToString(hash);
        }
#endif
#endregion
    }
}
