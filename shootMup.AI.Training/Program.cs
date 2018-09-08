using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Train
// Purge

namespace shootMup.Bots.Training
{
    class Program
    {
        public static int Usage()
        {
            Console.WriteLine("Usage: [verb] [parameters]");
            Console.WriteLine("  train [ml|cv] [directory to input/output] - trains the model");
            Console.WriteLine("  purge [directory to input]        - removes unnecessary training data");
            Console.WriteLine("  check [directory to model]        - loads a trained model and gives it a try");
            Console.WriteLine("  serialize [count] [action|angle|xy] [directory to input]");
            Console.WriteLine("                                    - dump the data into a csv file");
            return -1;
        }

        public static int Main(string[] args)
        {
            for(int i=0; i<args.Length; i++)
            {
                if (string.Equals(args[i], "train", StringComparison.OrdinalIgnoreCase))
                {
                    // train the model
                    var type = i + 1 < args.Length ? args[i + 1] : "";
                    var directory = i + 2 < args.Length ? args[i + 2] : "";
                    return ModelBuilding.TrainAndEvaulate(type, directory);
                }
                else if (string.Equals(args[i], "check", StringComparison.OrdinalIgnoreCase))
                {
                    // purge the input
                    var type = i + 1 < args.Length ? args[i + 1] : "";
                    var directory = i + 2 < args.Length ? args[i + 2] : "";
                    return ModelBuilding.Check(type, directory);
                }
                else if (string.Equals(args[i], "purge", StringComparison.OrdinalIgnoreCase))
                {
                    // purge the input
                    var directory = i + 1 < args.Length ? args[i + 1] : "";
                    var deleted = Purge.Execute(directory);

                    if (deleted >= 0) return 0;
                }
                else if (string.Equals(args[i], "run", StringComparison.OrdinalIgnoreCase))
                {
                    // do a trial run
                    return Executor.Run();
                }
                else if (string.Equals(args[i], "test", StringComparison.OrdinalIgnoreCase))
                {
                    float x1, y1;
                    float xdelta, ydelta;
                    foreach(var angle in new float[] { 0, 45, 90, 135, 180, 225, 270, 315, 359})
                    {
                        Common.Collision.CalculateLineByAngle(0, 0, angle, 1, out x1, out y1, out xdelta, out ydelta);

                        var sum = (float)(Math.Abs(xdelta) + Math.Abs(ydelta));
                        xdelta = xdelta / sum;
                        ydelta = ydelta / sum;

                        Console.WriteLine("0: {0} {1},{2}", angle, xdelta, ydelta);
                    }

                    foreach (var pair in new float[][]
                    {
                        new float[] { 1, 0 },
                        new float[] { 0, 1 },
                        new float[] { -1, 0 },
                        new float[] { 0, -1 },
                        new float[] {0.5f, 0.5f },
                        new float[] { -0.5f, 0.5f },
                        new float[] { 0.5f, -0.5f },
                        new float[] { -0.5f, -0.5f }
                        }
                        )
                    {
                        var angle = Common.Collision.CalculateAngleFromPoint(0, 0, pair[0], pair[1]);

                        Console.WriteLine("1: {0} {1},{2}", angle, pair[0], pair[1]);
                    }

                    return 0;
                }
                else if (string.Equals(args[i], "serialize", StringComparison.OrdinalIgnoreCase))
                {
                    var count = Convert.ToInt32(i + 1 < args.Length ? args[i + 1] : "0");
                    var type = i + 2 < args.Length ? args[i + 2] : "";
                    var directory = i + 3 < args.Length ? args[i + 3] : "";
                    return ModelBuilding.Serialize(directory, type, count);
                }

            }

            // the verb was not understood
            return Usage();
        }
    }
}
