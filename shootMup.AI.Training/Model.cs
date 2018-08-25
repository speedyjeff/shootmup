using shootMup.Bots;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace shootMup.Bots.Training
{
    public static class ModelBuilding
    {
        public static int Check(string type, string path)
        {
            // load each model and then give a few predictions and check the results
            var rand = new Random();
            var data = new List<TrainingData>();
            foreach (var kvp in AITraining.GetTrainingFiles(path))
            {
                var file = kvp.Key;
                var count = kvp.Value;

                if (count > 0)
                {
                    foreach (var d in AITraining.GetTraingingData(file))
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

            foreach (var modelType in new ModelValue[]
                {
                    ModelValue.Action,
                    ModelValue.XY,
                    ModelValue.Angle
                }
            )
            {
                var map = new Dictionary<string, int>();

                var modelPath = "";
                if (modelType == ModelValue.Action) modelPath = string.Format("action.{0}.model", type);
                else if (modelType == ModelValue.XY) modelPath = string.Format("xy.{0}.model", type);
                else if (modelType == ModelValue.Angle) modelPath = string.Format("angle.{0}.model", type);
                else throw new Exception("Unknown model type : " + modelType);

                Model model = null;
                if (type.Equals("ml", StringComparison.OrdinalIgnoreCase)) model = new ModelMLNet(Path.Combine(path, modelPath));
                else model = new ModelOpenCV(Path.Combine(path, modelPath));

                var delta = 0f;
                var count = 0;
                var timer = new Stopwatch();
                timer.Start();
                foreach(var d in data)
                {
                    var key = "";
                    if (modelType == ModelValue.XY)
                    {
                        float xdelta, ydelta;
                        model.Predict(d.AsModelDataSet(), out xdelta, out ydelta);
                        delta += Math.Abs(xdelta - d.Xdelta);
                        delta += Math.Abs(ydelta - d.Ydelta);
                        count += 2;

                        key = string.Format("{0},{1} {2},{3}", Math.Round(d.Xdelta, 1), Math.Round(d.Ydelta, 1), Math.Round(xdelta, 1), Math.Round(ydelta, 1));
                    }
                    else
                    {
                        var value = model.Predict(d.AsModelDataSet());

                        if (modelType == ModelValue.Action) delta += Math.Abs(value - (float)d.Action);
                        else delta += Math.Abs(value - d.Angle);
                        count++;

                        if (modelType == ModelValue.Action)
                            key = string.Format("{0},{1}", d.Action, Math.Round(value));
                        else
                            key = string.Format("{0},{1}", Math.Round(d.Angle), Math.Round(value));
                    }
                    if (!map.ContainsKey(key)) map.Add(key, 0);
                    map[key]++;
                }
                timer.Stop();

                Console.WriteLine("{0} has an average delta of {1:f2}.  This ran in {2}ms or {3:f2}ms per prediction", modelType, delta / (float)count, timer.ElapsedMilliseconds,  (float)timer.ElapsedMilliseconds/(float)data.Count);

                foreach(var kvp in map.OrderByDescending(k => k.Value))
                {
                    Console.WriteLine("\t{0}\t{1}", kvp.Key, kvp.Value);
                }
            }

            return 0;
        }

        public static int TrainAndEvaulate(string type, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return -1;

            var rand = new Random();
            var data = new List<ModelDataSet>();
            var testData = new List<ModelDataSet>();
            foreach(var kvp in AITraining.GetTrainingFiles(path))
            {
                var file = kvp.Key;
                var count = kvp.Value;

                if (count > 0)
                {
                    foreach (var d in AITraining.GetTraingingData(file))
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
            shootMup.Bots.Model actions = null;
            if (type.Equals("ml", StringComparison.OrdinalIgnoreCase)) actions = new ModelMLNet(data, ModelValue.Action);
            else actions = new ModelOpenCV(data, ModelValue.Action);
            actions.Save(Path.Combine(path, string.Format("action.{0}.model", type)));
            // evaluate
            var eval = actions.Evaluate(testData, ModelValue.Action);
            Console.WriteLine("Actions RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            // train
            shootMup.Bots.Model xy = null;
            if (type.Equals("ml", StringComparison.OrdinalIgnoreCase)) xy = new ModelMLNet(data, ModelValue.XY);
            else xy = new ModelOpenCV(data, ModelValue.XY);
            xy.Save(Path.Combine(path, string.Format("xy.{0}.model", type)));
            // evaluate
            eval = xy.Evaluate(testData, ModelValue.XY);
            Console.WriteLine("XY RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            // train
            shootMup.Bots.Model angle = null;
            if (type.Equals("ml", StringComparison.OrdinalIgnoreCase)) angle = new ModelMLNet(data, ModelValue.Angle);
            else angle = new ModelOpenCV(data, ModelValue.Angle);
            angle.Save(Path.Combine(path, string.Format("angle.{0}.model", type)));
            // evaluate
            eval = angle.Evaluate(testData, ModelValue.Angle);
            Console.WriteLine("Angle RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            return 0;
        }

    }
}
