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

        public bool Predict(ModelDataSet data, out float xdelta, out float ydelta)
        {
            var angle = Predict(data);

            // set course
            if (angle < 0) angle *= -1;
            angle = angle % 360;
            ydelta = ((float)Math.Cos(angle * Math.PI / 180) * 1) * -1;
            xdelta = (float)Math.Sin(angle * Math.PI / 180) * 1;

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
