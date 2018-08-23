using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Train
// Purge

namespace shootMup.AI.Training
{
    class Program
    {
        public static int Usage()
        {
            Console.WriteLine("Usage: [verb] [parameters]");
            Console.WriteLine("  train [directory to input/output] - trains the model");
            Console.WriteLine("  purge [directory to input]        - removes unnecessary training data");
            Console.WriteLine("  check [directory to model]        - loads a trained model and gives it a try");
            return -1;
        }

        public static int Main(string[] args)
        {
            for(int i=0; i<args.Length; i++)
            {
                if (string.Equals(args[i], "train", StringComparison.OrdinalIgnoreCase))
                {
                    // train the model
                    var directory = i + 1 < args.Length ? args[i + 1] : "";
                    return Model.TrainAndEvaulate(directory);
                }
                else if (string.Equals(args[i], "check", StringComparison.OrdinalIgnoreCase))
                {
                    // purge the input
                    var directory = i + 1 < args.Length ? args[i + 1] : "";
                    return Model.Check(directory);
                }
                else if (string.Equals(args[i], "purge", StringComparison.OrdinalIgnoreCase))
                {
                    // purge the input
                    var directory = i + 1 < args.Length ? args[i + 1] : "";
                    var deleted = Purge.Execute(directory);

                    if (deleted >= 0) return 0;
                }
            }

            // the verb was not understood
            return Usage();
        }
    }
}
