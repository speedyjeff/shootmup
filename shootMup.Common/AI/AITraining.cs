using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace shootMup.Common
{
    public class ElementProximity
    {
        public int Id;
        public Type Type;
        public float X;
        public float Y;
        public float Angle;
        public float Distance;
    }

    public class AITraining
    {
        static AITraining()
        {
            SHA = SHA256.Create();
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
                        Type = type,
                        Id = elem.Id,
                        X = elem.X,
                        Y = elem.Y,
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
                    proximity.X = elem.X;
                    proximity.Y = elem.Y;
                    proximity.Angle = angle;
                    proximity.Distance = distance;
                }
            }

            return closest;
        }

        #region private
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
            long hash = 13;
            foreach(var v in values)
            {
                hash = (hash + v) * 7;
            }

            return hash;
        }

        private static string ComputeHash(string encoded)
        {
            var buffer = System.Text.Encoding.ASCII.GetBytes(encoded);
            var hash = SHA.ComputeHash(buffer);
            return BitConverter.ToString(hash);
        }
        #endregion
    }
}
