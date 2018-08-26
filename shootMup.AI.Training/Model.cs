using shootMup.Bots;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace shootMup.Bots.Training
{
    class DataSet
    {
        public List<ModelDataSet> Test;
        public List<ModelDataSet> Training;
        public int TrainingCountMax;

        public DataSet()
        {
            Test = new List<ModelDataSet>();
            Training = new List<ModelDataSet>();
            TrainingCountMax = 100000;
        }
    }

    public static class ModelBuilding
    {
        public static int Check(string type, string path)
        {
            // load each model and then give a few predictions and check the results
            var rand = new Random();
            var data = new List<TrainingData>();
            var done = false;
            foreach (var kvp in AITraining.GetTrainingFiles(path))
            {
                if (done) break;

                var file = kvp.Key;
                var count = kvp.Value;

                if (count > 0)
                {
                    foreach (var d in AITraining.GetTraingingData(file))
                    {
                        if (done) break;

                        if (d.Result)
                        {
                            // sample ~15%
                            if (rand.Next() % 6 == 0)
                            {
                                // test data set
                                data.Add(d);
                            }

                            if (data.Count > 1000) done = true;
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

            // 1 in x of these will be picked up (the smaller the number the more of them
            var takeOnly = 0;
            var rand = new Random();
            var actionData = new DataSet();
            var xyData = new DataSet();
            var angleData = new DataSet();
            int trainingCount = 0, testCount = 0;
            foreach(var kvp in AITraining.GetTrainingFiles(path))
            {
                var file = kvp.Key;
                var count = kvp.Value;

                if (count > 0)
                {
                    // for debugging
                    if (takeOnly > 0 && --takeOnly == 0) break;

                    // once enough data has been gathered, break
                    if (actionData.TrainingCountMax + xyData.TrainingCountMax + angleData.TrainingCountMax == 0) break;

                    foreach (var d in AITraining.GetTraingingData(file))
                    {
                        if (d.Result)
                        {
                            var dm = d.AsModelDataSet();

                            // sample ~15% for test
                            if (rand.Next() % 6 == 0)
                            {
                                // test data set
                                testCount++;
                                if (actionData.TrainingCountMax > 0)
                                {
                                    actionData.Test.Add(dm);
                                }
                                switch ((ActionEnum)d.Action)
                                {
                                    case ActionEnum.Attack:
                                        if (angleData.TrainingCountMax > 0)
                                        {
                                            angleData.Test.Add(dm);
                                        }
                                        break;
                                    case ActionEnum.Move:
                                        if (xyData.TrainingCountMax > 0)
                                        {
                                            xyData.Test.Add(dm);
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                // training data set
                                trainingCount++;
                                if (actionData.TrainingCountMax > 0)
                                {
                                    actionData.Training.Add(dm);
                                    actionData.TrainingCountMax--;
                                }
                                switch ((ActionEnum)d.Action)
                                {
                                    case ActionEnum.Attack:
                                        if (angleData.TrainingCountMax > 0)
                                        {
                                            angleData.Training.Add(dm);
                                            angleData.TrainingCountMax--;
                                        }
                                        break;
                                    case ActionEnum.Move:
                                        if (xyData.TrainingCountMax > 0)
                                        {
                                            xyData.Training.Add(dm);
                                            xyData.TrainingCountMax--;
                                        }
                                        break;
                                }
                            }
                        } // is result
                    } // foreach TrainingData
                } // if count > 0
            } // foreach file

            Console.WriteLine("Training data set ({0} items) and test data set ({1} items)", trainingCount, testCount);
            Console.WriteLine("  Training: Action({0}) XY({1}) Angle({2})", actionData.Training.Count, xyData.Training.Count, angleData.Training.Count);
            Console.WriteLine("      Test: Action({0}) XY({1}) Angle({2})", actionData.Test.Count, xyData.Test.Count, angleData.Test.Count);

            // train
            shootMup.Bots.Model actions = null;
            if (type.Equals("ml", StringComparison.OrdinalIgnoreCase)) actions = new ModelMLNet(actionData.Training, ModelValue.Action);
            else actions = new ModelOpenCV(actionData.Training, ModelValue.Action);
            actions.Save(Path.Combine(path, string.Format("action.{0}.model", type)));
            // evaluate
            var eval = actions.Evaluate(actionData.Test, ModelValue.Action);
            Console.WriteLine("Actions RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            // train
            shootMup.Bots.Model xy = null;
            if (type.Equals("ml", StringComparison.OrdinalIgnoreCase)) xy = new ModelMLNet(xyData.Training, ModelValue.XY);
            else xy = new ModelOpenCV(xyData.Training, ModelValue.XY);
            xy.Save(Path.Combine(path, string.Format("xy.{0}.model", type)));
            // evaluate
            eval = xy.Evaluate(xyData.Test, ModelValue.XY);
            Console.WriteLine("XY RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            // train
            shootMup.Bots.Model angle = null;
            if (type.Equals("ml", StringComparison.OrdinalIgnoreCase)) angle = new ModelMLNet(angleData.Training, ModelValue.Angle);
            else angle = new ModelOpenCV(angleData.Training, ModelValue.Angle);
            angle.Save(Path.Combine(path, string.Format("angle.{0}.model", type)));
            // evaluate
            eval = angle.Evaluate(angleData.Test, ModelValue.Angle);
            Console.WriteLine("Angle RMS={0} R^2={1}", eval.RMS, eval.RSquared);

            return 0;
        }

    }
}
