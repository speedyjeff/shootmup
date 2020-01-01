using engine.Common;
using engine.Common.Entities;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace shootMup.Bots
{
    public static class AITraining
    {
        static AITraining()
        {
            Data = new Dictionary<int, TrainingData>();
            Output = new Dictionary<int, StreamWriter>();
            Start = DateTime.Now;
            DataLock = new ReaderWriterLockSlim();
            OutputLock = new ReaderWriterLockSlim();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

#if SMALL_ENCODING
            SHA = SHA256.Create();
#endif
        }

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

        public static void CaptureBefore(Player player, ActionDetails details)
        {
            var data = GetData(player);

            // capture the angle to the center
            data.CenterAngle = details.AngleToCenter;

            // capture the user health, shield, weapon status, inzone, Z
            data.Angle = player.Angle;
            data.Health = player.Health;
            data.InZone = details.InZone;
            data.Xdelta = details.XDelta;
            data.Ydelta = details.YDelta;
            if (player.Primary != null && player.Primary is RangeWeapon)
            {
                data.Primary = player.Primary.GetType().Name;
                data.PrimaryAmmo = (player.Primary as RangeWeapon).Ammo;
                data.PrimaryClip = (player.Primary as RangeWeapon).Clip;
            }
            else
            {
                data.Primary = "";
                data.PrimaryAmmo = data.PrimaryClip = 0;
            }
            if (player.Secondary != null && player.Secondary.Length == 1 && player.Secondary[0] != null && player.Secondary[0] is RangeWeapon)
            {
                data.Secondary = player.Secondary.GetType().Name;
                data.SecondaryAmmo = (player.Secondary[0] as RangeWeapon).Ammo;
                data.SecondaryClip = (player.Secondary[0] as RangeWeapon).Clip;
            }
            else
            {
                data.Secondary = "";
                data.SecondaryAmmo = data.SecondaryClip = 0;
            }
            data.Shield = player.Shield;
            data.Z = player.Z;

            // capture what the user sees
            data.Proximity = AITraining.ComputeProximity(player, details.Elements).Values.ToList();
        }

        public static void CaptureAfter(Player player, ActionEnum action, bool result)
        {
            var data = GetData(player);

            data.Action = (int)action;
            data.Result = result;

            var output = GetOutput(player);
            var json = data.ToJson();
            output.WriteLine(json);
        }

        public static void CaptureWinners(List<string> winners)
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
            var sb = new StringBuilder();
            sb.Append('[');
            for(int i=0; i<winners.Count; i++)
            {
                sb.AppendFormat("\"{0}\"", winners[i]);
                if (i < winners.Count - 1) sb.Append(',');
            }
            sb.Append(']');
            var json = sb.ToString();
            output.WriteLine(json);
        }

        public static Dictionary<string, int> GetTrainingFiles(string path)
        {
            path = Path.GetFullPath(path);

            // gather all the humans and the top winning AI
            var map = new Dictionary<string, int>();
            foreach (var file in Directory.GetFiles(path))
            {
                if (file.EndsWith(".winner"))
                {
                    var json = File.ReadAllText(file);
                    var prefix = file.Substring(0, file.LastIndexOf('.'));
                    // open the file and build a map of files to consider
                    var start = json[0] == '[' ? 1 : 0;
                    var end = 0;
                    while (start < json.Length)
                    {
                        end = json.IndexOf(',', start);

                        if (end < 0) end = json.Length;

                        var i1 = json.IndexOf('[', start);
                        var i2 = json.IndexOf(']', start);

                        // "name [#]"
                        if (i1 > 0 && i2 > 0)
                        {
                            var name = json.Substring(start + 1, i1 - start - 1).Trim();
                            var number = json.Substring(i1 + 1, i2 - i1 - 1);
                            int value;
                            var fullpath = Path.Combine(path, prefix + "." + name);

                            // check if the file exists
                            if (File.Exists(fullpath))
                            {
                                if (Int32.TryParse(number, out value))
                                {
                                    if (!map.ContainsKey(fullpath)) map.Add(fullpath, value);
                                    else map[fullpath] = value;
                                }
                            }
                        }

                        // advance
                        start = end + 1;
                    }
                }
                else if (!file.EndsWith(".model"))
                {
                    if (!map.ContainsKey(file)) map.Add(file, 0);
                }
            }
            return map;
        }

        public static IEnumerable<TrainingData> GetTraingingData(string file)
        {
            foreach (var json in File.ReadAllLines(file))
            {
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TrainingData>(json);
                yield return data;
            }
        }

        #region private
        private static Dictionary<int, TrainingData> Data;
        private static Dictionary<int, StreamWriter> Output;
        private static DateTime Start;
        private static ReaderWriterLockSlim OutputLock;
        private static ReaderWriterLockSlim DataLock;

        private const string TrainingPath = "training";

        private static TrainingData GetData(Player player)
        {
            DataLock.EnterUpgradeableReadLock();
            try
            {
                TrainingData data = null;
                if (!Data.TryGetValue(player.Id, out data))
                {
                    DataLock.EnterWriteLock();
                    try
                    {
                        data = new TrainingData();
                        Data.Add(player.Id, data);
                    }
                    finally
                    {
                        DataLock.ExitWriteLock();
                    }
                }
                return data;
            }
            finally
            {
                DataLock.ExitUpgradeableReadLock();
            }
        }

        private static StreamWriter GetOutput(Player player)
        {
            try
            {
                OutputLock.EnterUpgradeableReadLock();

                StreamWriter output = null;
                if (!Output.TryGetValue(player.Id, out output))
                {
                    try
                    {
                        OutputLock.EnterWriteLock();
                        if (!Directory.Exists(TrainingPath)) Directory.CreateDirectory(TrainingPath);
                        output = File.CreateText(Path.Combine(TrainingPath, string.Format("{0:yyyy-MM-dd_HH-mm-ss}.{1}", Start, player.Name)));
                        Output.Add(player.Id, output);
                    }
                    finally
                    {
                        OutputLock.ExitWriteLock();
                    }
                }
                return output;
            }
            finally
            {
                OutputLock.ExitUpgradeableReadLock();
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (var file in Output.Values) file.Flush();
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
