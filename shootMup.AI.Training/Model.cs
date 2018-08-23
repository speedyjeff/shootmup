using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace shootMup.AI.Training
{
    public static class Model
    {
        public static int Check(string path)
        {
            // load each model and then give a few predictions and check the results
            var rand = new Random();
            var data = new List<TrainingData>();
            foreach (var kvp in shootMup.Common.AITraining.GetTrainingFiles(path))
            {
                var file = kvp.Key;
                var count = kvp.Value;

                if (count > 0)
                {
                    foreach (var d in shootMup.Common.AITraining.GetTraingingData(file))
                    {
                        if (d.Result)
                        {
                            // sample ~15%
                            if (rand.Next() % 6 == 0)
                            {
                                // test data set
                                data.Add(d);
                            }

                            if (data.Count > 1000) break;
                        }
                    }
                }
            }

            foreach (var modelType in new shootMup.Common.ModelValue[]
                {
                    shootMup.Common.ModelValue.Action,
                    shootMup.Common.ModelValue.XY,
                    shootMup.Common.ModelValue.Angle
                }
            )
            {
                var modelPath = "";
                if (modelType == shootMup.Common.ModelValue.Action) modelPath = "action.model";
                else if (modelType == shootMup.Common.ModelValue.XY) modelPath = "xy.model";
                else if (modelType == shootMup.Common.ModelValue.Angle) modelPath = "angle.model";
                else throw new Exception("Unknown model type : " + modelType);

                var model = shootMup.Common.ModelMLNet.Load(Path.Combine(path, modelPath));

                var delta = 0f;
                var count = 0;
                var timer = new Stopwatch();
                timer.Start();
                foreach(var d in data)
                {
                    if (modelType == shootMup.Common.ModelValue.XY)
                    {
                        float xdelta, ydelta;
                        model.Predict(d.AsModelDataSet(), out xdelta, out ydelta);
                        delta += Math.Abs(xdelta - d.Xdelta);
                        delta += Math.Abs(ydelta - d.Ydelta);
                        count += 2;
                    }
                    else
                    {
                        var value = model.Predict(d.AsModelDataSet());

                        if (modelType == shootMup.Common.ModelValue.Action) delta += Math.Abs(value - (float)d.Action);
                        else delta += Math.Abs(value - d.Angle);
                        count++;
                    }
                }
                timer.Stop();

                Console.WriteLine("{0} has an average delta of {1:f2}.  This ran in {2}ms or {3:f2}ms per prediction", modelType, delta / (float)count, timer.ElapsedMilliseconds,  (float)timer.ElapsedMilliseconds/(float)data.Count);
            }

            return 0;
        }

        public static int TrainAndEvaulate(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return -1;

            var rand = new Random();
            var data = new List<ModelDataSet>();
            var testData = new List<ModelDataSet>();
            foreach(var kvp in shootMup.Common.AITraining.GetTrainingFiles(path))
            {
                var file = kvp.Key;
                var count = kvp.Value;

                if (count > 0)
                {
                    foreach (var d in shootMup.Common.AITraining.GetTraingingData(file))
                    {
                        if (d.Result)
                        {
                            // sample ~15%
                            if (rand.Next() % 6 == 0)
                            {
                                // test data set
                                testData.Add(d.AsModelDataSet());
                            }
                            else
                            {
                                // training data set
                                data.Add(d.AsModelDataSet());
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Training data set ({0} items) and test data set ({1} items)", data.Count, testData.Count);

            // train
            var actions = shootMup.Common.ModelMLNet.Train(data, ModelValue.Action);
            actions.Save(Path.Combine(path, "action.model"));
            // evaluate
            var eval = actions.Evaluate(testData);
            Console.WriteLine("Actions RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            // train
            var xy = shootMup.Common.ModelMLNet.Train(data, ModelValue.XY);
            xy.Save(Path.Combine(path, "xy.model"));
            // evaluate
            eval = xy.Evaluate(testData);
            Console.WriteLine("XY RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            // train
            var angle = shootMup.Common.ModelMLNet.Train(data, ModelValue.Angle);
            angle.Save(Path.Combine(path, "angle.model"));
            // evaluate
            eval = angle.Evaluate(testData);
            Console.WriteLine("Angle RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            return 0;
        }

    }
}
