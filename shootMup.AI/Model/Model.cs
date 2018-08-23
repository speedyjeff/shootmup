﻿using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Models;
using Microsoft.ML.Runtime;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace shootMup.Common
{
    public enum ModelValue { Action, XY, Angle };

    public struct ModelFitness
    {
        public double RMS;
        public double RSquared;
    }

    public class Model
    {
        public static Model Train(IEnumerable<ModelDataSet> data, ModelValue prediction)
        {
            var pipeline = new LearningPipeline();

            // add data
            pipeline.Add( CollectionDataSource.Create(data) );

            // normalize text fields
            pipeline.Add(new CategoricalOneHotVectorizer("Primary", "Secondary"));

            // choose what to predict
            switch (prediction)
            {
                case ModelValue.Action:
                    pipeline.Add(new ColumnCopier(("Action", "Label")));
                    break;
                case ModelValue.Angle:
                    pipeline.Add(new ColumnCopier(("FaceAngle", "Label")));
                    break;
                case ModelValue.XY:
                    pipeline.Add(new ColumnCopier(("MoveAngle", "Label")));
                    break;
                default: throw new Exception("Unknown prediction : " + prediction);
            }

            // add columns as features
            // do not include the features which should be predicted
            pipeline.Add(new ColumnConcatenator("Features",
                "CenterAngle",
                "InZone",
                "Health",
                "Sheld",
                "Z",
                "Primary",
                "PrimaryAmmo",
                "PrimaryClip",
                "Secondary",
                "SecondaryAmmo",
                "SecondaryClip",
                // "Ammo",
                "Angle_1",
                "Distance_1",
                // "Bandage",
                "Angle_2",
                "Distance_2",
                // "Helmet",
                "Angle_3",
                "Distance_3",
                // "Ak47",
                "Angle_4",
                "Distance_4",
                // "Shotgun",
                "Angle_5",
                "Distance_5",
                // "Pistol",
                "Angle_6",
                "Distance_6",
                // "Obstacle",
                "Angle_7",
                "Distance_7",
                // "Player",
                "Angle_8",
                "Distance_8"
                ));

            // add a classifier
            // action 0.25, xy 84.9777, angle 71.591
            //pipeline.Add(new FastTreeRegressor());

            pipeline.Add(new FastTreeRegressor()
            {
                // NumTrees 
                //  100 - default
                //  100 - xy 79
                // 1000 - xy 63, angle 52 (slow)
                NumTrees = 1000,
                // NumLeaves
                //   50 - default
                //  100 - xy 63, angle 71
                // 1000 - xy 59, angle 49 (slow)
                NumLeaves = 1000,
                // NumThreads
                // 5 - default
                NumThreads = 50,
                // EntropyCoe
                // 0.30 - default
                // 0.70 - xy 79, angle 66
                // 0.99 - xy 79, angle 66
                // 0.05 - xy 79, angle 66
                EntropyCoefficient = 0.3

            });

            // action 0.26 xy 85.1606, angle 72.5194
            //pipeline.Add(new FastTreeTweedieRegressor());
            // took too long
            //pipeline.Add(new GeneralizedAdditiveModelRegressor());
            // runtime exception
            //pipeline.Add(new LightGbmRegressor());
            // action 0.4736, xy 105.8815, angle 91.2677
            //pipeline.Add(new OnlineGradientDescentRegressor());
            // runtime exception
            //pipeline.Add(new OrdinaryLeastSquaresRegressor());
            // action 0.4628, xy 106.0547, angle 91.0179
            //pipeline.Add(new PoissonRegressor());
            // action 0.4599, xy 105.8474, angle 90.4134
            //pipeline.Add(new StochasticDualCoordinateAscentRegressor());

            // train the model
            var model = new Model();
            model.TrainedModel = pipeline.Train<ModelDataSet, ModelDataSetPrediction>();
            return model;
        }

        public void Save(string path)
        {
            lock (TrainedModel)
            {
                TrainedModel.WriteAsync(path).Wait();
            }
        }

        public static Model Load(string path)
        {
            var model = new Model();
            model.TrainedModel = PredictionModel.ReadAsync<ModelDataSet, ModelDataSetPrediction>(path).Result;

            return model;
        }

        public ModelFitness Evaluate(List<ModelDataSet> data)
        {
            lock (TrainedModel)
            {
                var testData = CollectionDataSource.Create(data);
                var evaluator = new RegressionEvaluator();
                var metrics = evaluator.Evaluate(TrainedModel, testData);

                return new ModelFitness()
                {
                    RMS = metrics.Rms,
                    RSquared = metrics.RSquared
                };
            }
        }

        public bool Predict(ModelDataSet data, out float xdelta, out float ydelta)
        {
            var angle = Predict(data);

            // set course
            float x1, y1;
            Collision.CalculateLineByAngle(0, 0, angle, 1, out x1, out y1, out xdelta, out ydelta);

            // normalize
            xdelta = xdelta / (Math.Abs(xdelta) + Math.Abs(ydelta));
            ydelta = ydelta / (Math.Abs(xdelta) + Math.Abs(ydelta));
            if (Math.Abs(xdelta) + Math.Abs(ydelta) > 1)
            {
                var delta = (Math.Abs(xdelta) + Math.Abs(ydelta)) - 1;
                if (xdelta > ydelta) xdelta -= delta;
                else ydelta -= delta;
            }
            xdelta = (float)Math.Round(xdelta, 4);
            ydelta = (float)Math.Round(ydelta, 4);

            return true;
        }

        public float Predict(ModelDataSet data)
        {
            lock (TrainedModel)
            {
                var result = TrainedModel.Predict(data);
                return result.Value;
            }
        }

        #region private
        private PredictionModel<ModelDataSet, ModelDataSetPrediction> TrainedModel;
        #endregion
    }
}
