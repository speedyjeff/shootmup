using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace shootMup.Common
{
    public enum ModelValue { Action, XY, Angle };

    public class Model
    {
        public static Model Train(List<TrainingData> data, ModelValue prediction)
        {
            var pipeline = new LearningPipeline();

            // add data
            pipeline.Add(CollectionDataSource.Create(data.Select(d => Transform(d))) );

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
            pipeline.Add(new FastTreeRegressor());

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

        public bool Predict(TrainingData data, out float xdelta, out float ydelta)
        {
            var angle = Predict(data);

            // set course
            float x1, y1, x2, y2;
            Collision.CalculateLineByAngle(0, 0, angle, 1, out x1, out y1, out x2, out y2);

            xdelta = x2 - x1;
            ydelta = y2 - y1;

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

        public float Predict(TrainingData data)
        {
            var modelData = Transform(data);

            lock (TrainedModel)
            {
                var result = TrainedModel.Predict(modelData);
                return result.Value;
            }
        }

        #region private
        private PredictionModel<ModelDataSet, ModelDataSetPrediction> TrainedModel;

        private static ModelDataSet Transform(TrainingData data)
        {
            var result = new ModelDataSet()
            {
                // core data
                CenterAngle = data.CenterAngle,
                InZone = data.InZone ? 1f : 0,
                Health = data.Health,
                Sheld = data.Sheld,
                Z = data.Z,
                Primary = data.Primary,
                PrimaryAmmo = data.PrimaryAmmo,
                PrimaryClip = data.PrimaryClip,
                Secondary = data.Secondary,
                SecondaryAmmo = data.SecondaryAmmo,
                SecondaryClip = data.SecondaryClip,
            };

            // outcome
            result.Action = (int)data.Action;
            result.FaceAngle = data.Angle;
            result.MoveAngle = Collision.CalculateAngleFromPoint(0, 0, data.Xdelta, data.Ydelta);

            // proximity
            foreach (var elem in data.Proximity)
            {
                switch (elem.Name)
                {
                    case "Ammo":
                        result.Name_1 = elem.Name;
                        result.Angle_1 = elem.Angle;
                        result.Distance_1 = elem.Distance;
                        break;
                    case "Bandage":
                        result.Name_2 = elem.Name;
                        result.Angle_2 = elem.Angle;
                        result.Distance_2 = elem.Distance;
                        break;
                    case "Helmet":
                        result.Name_3 = elem.Name;
                        result.Angle_3 = elem.Angle;
                        result.Distance_3 = elem.Distance;
                        break;
                    case "AK47":
                        result.Name_4 = elem.Name;
                        result.Angle_4 = elem.Angle;
                        result.Distance_4 = elem.Distance;
                        break;
                    case "Shotgun":
                        result.Name_5 = elem.Name;
                        result.Angle_5 = elem.Angle;
                        result.Distance_5 = elem.Distance;
                        break;
                    case "Pistol":
                        result.Name_6 = elem.Name;
                        result.Angle_6 = elem.Angle;
                        result.Distance_6 = elem.Distance;
                        break;
                    case "Obstacle":
                        result.Name_7 = elem.Name;
                        result.Angle_7 = elem.Angle;
                        result.Distance_7 = elem.Distance;
                        break;
                    case "Player":
                        result.Name_8 = elem.Name;
                        result.Angle_8 = elem.Angle;
                        result.Distance_8 = elem.Distance;
                        break;
                    default:
                        throw new Exception("Unknown proximity element type : " + elem.Name);
                }
            }

            return result;
        }
        #endregion
    }
}
