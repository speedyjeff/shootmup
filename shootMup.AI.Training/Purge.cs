using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using shootMup.Common;
using shootMup.Bots;

namespace shootMup.Bots.Training
{
    public static class Purge
    {
        public static int Execute(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return -1;

            var considered = 0;
            var deleted = 0;
            var map = new HashSet<string>();
            foreach (var kvp in AITraining.GetTrainingFiles(path))
            {
                considered++;
                if (kvp.Value <= 0)
                {
                    // remove inputs that had no kills
                    Console.WriteLine("Removed {0}", kvp.Key);
                    File.Delete(kvp.Key);
                    deleted++;
                }
            }

            Console.WriteLine("Removed {0} of {1} files", deleted, considered);

            return deleted;
        }
    }
}
