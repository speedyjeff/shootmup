using Microsoft.ML;

#if ML_LEGACY
using Microsoft.ML.Legacy;
using Microsoft.ML.Legacy.Data;
using Microsoft.ML.Legacy.Trainers;
using Microsoft.ML.Legacy.Transforms;
#else
using Microsoft.ML.Data;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.Core.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
#endif

using shootMup.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace shootMup.Bots
{
    public class ModelMLNet : Model
    {
        // train
        public ModelMLNet(IEnumerable<ModelDataSet> data, ModelValue prediction)
        {
#if ML_LEGACY
            var pipeline = new LearningPipeline();

            // add data
            pipeline.Add( CollectionDataSource.Create(data) );

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
                "Shield",
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
                NumTrees = 100,
                // NumLeaves
                //   50 - default
                //  100 - xy 63, angle 71
                // 1000 - xy 59, angle 49 (slow)
                NumLeaves = 50,
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
            TrainedModel = pipeline.Train<ModelDataSet, ModelDataSetPrediction>();
#else
            Context = new MLContext();

            // add data
            var textLoader = GetTextLoader(Context, prediction);

            // spill to disk !?!?! since there is no way to load from a collection
            var pathToData = "";
            try
            {
                // write data to disk
                pathToData = WriteToDisk(data, prediction);

                // read in data
                IDataView dataView = textLoader.Read(pathToData);

                // configurations
                var label = "";
                switch (prediction)
                {
                    case ModelValue.Action: label = "Action"; break;
                    case ModelValue.Angle: label = "FaceAngle"; break;
                    case ModelValue.XY: label = "MoveAngle"; break;
                    default: throw new Exception("Unknown value for prediction : " + prediction);
                }
                var dataPipeline = Context.Transforms.CopyColumns(label, "Label")
                    .Append(Context.Transforms.Concatenate("Features", ColumnNames(false /* do not include the label */, prediction)));

                // set the training algorithm
                var trainer = Context.Regression.Trainers.FastTree(label: "Label", features: "Features");
                var trainingPipeline = dataPipeline.Append(trainer);

                TrainedModel = trainingPipeline.Fit(dataView);
            }
            finally
            {
                // cleanup
                if (!string.IsNullOrWhiteSpace(pathToData) && File.Exists(pathToData)) File.Delete(pathToData);
            }
#endif
        }

        // load
        public ModelMLNet(string path)
        {
#if ML_LEGACY
            TrainedModel = PredictionModel.ReadAsync<ModelDataSet, ModelDataSetPrediction>(path).Result;
#else
            Context = new MLContext();

            // load
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                TrainedModel = Context.Model.Load(stream);
            }

            // create the prediction function
            PredictFunc = TrainedModel.MakePredictionFunction<ModelDataSet, ModelDataSetPrediction>(Context);
#endif
        }

        public override void Save(string path)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");

            lock (TrainedModel)
            {
#if ML_LEGACY
                TrainedModel.WriteAsync(path).Wait();
#else
                // save
                using (var stream = File.Create(path))
                {
                    TrainedModel.SaveTo(Context, stream);
                }
#endif
            }
        }

        public override ModelFitness Evaluate(List<ModelDataSet> data, ModelValue prediction)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");

            lock (TrainedModel)
            {
#if ML_LEGACY
                var testData = CollectionDataSource.Create(data);
                var evaluator = new RegressionEvaluator();
                var metrics = evaluator.Evaluate(TrainedModel, testData);

                return new ModelFitness()
                {
                    RMS = metrics.Rms,
                    RSquared = metrics.RSquared
                };
#else
                var textLoader = GetTextLoader(Context, prediction);

                var pathToData = "";
                try
                {
                    // ugh have to spill data to disk for it to work!
                    pathToData = WriteToDisk(data, prediction);

                    IDataView dataView = textLoader.Read(pathToData);
                    var predictions = TrainedModel.Transform(dataView);
                    var metrics = Context.Regression.Evaluate(predictions, label: "Label", score: "Score");

                    return new ModelFitness()
                    {
                        RMS = metrics.Rms,
                        RSquared = metrics.RSquared
                    };
                }
                finally
                {
                    // cleanup
                    if (!string.IsNullOrWhiteSpace(pathToData) && File.Exists(pathToData)) File.Delete(pathToData);
                }
#endif
            }
        }

        public override float Predict(ModelDataSet data)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");

            lock (TrainedModel)
            {
#if ML_LEGACY
                var result = TrainedModel.Predict(data);
                return result.Score;
#else
                var result = PredictFunc.Predict(data);
                return result.Score;
#endif
            }
        }

#region private

#if ML_LEGACY
        private PredictionModel<ModelDataSet, ModelDataSetPrediction> TrainedModel;
#else
        private MLContext Context;
        private ITransformer TrainedModel;
        private PredictionFunction<ModelDataSet, ModelDataSetPrediction> PredictFunc;

        private static string WriteToDisk(IEnumerable<ModelDataSet> data, ModelValue prediction)
        {
            // geneate a random path
            var path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            using (var writer = File.CreateText(path))
            {
                foreach (var d in data)
                {
                    for (int i = 0; i < d.Features(); i++)
                    {
                        writer.Write(d.Feature(i));
                        writer.Write(',');
                    }
                    switch(prediction)
                    {
                        case ModelValue.Action: writer.WriteLine(d.Action); break;
                        case ModelValue.Angle: writer.WriteLine(d.FaceAngle); break;
                        case ModelValue.XY: writer.WriteLine(d.MoveAngle); break;
                        default: throw new Exception("Unknown value for prediction : " + prediction);
                    }
                }
            }

            return path;
        }

        private static string[] ColumnNames(bool withLabel, ModelValue prediction)
        {
            var columns = new List<string>();
            var data = new ModelDataSet();
            for (int i = 0; i < data.Features(); i++) columns.Add(data.Name(i));

            if (withLabel)
            {
                switch (prediction)
                {
                    case ModelValue.Action: columns.Add("Action"); break;
                    case ModelValue.Angle: columns.Add("FaceAngle"); break;
                    case ModelValue.XY: columns.Add("MoveAngle"); break;
                    default: throw new Exception("Unknown value for prediction : " + prediction);
                }
            }

            return columns.ToArray();
        }

        private static TextLoader GetTextLoader(MLContext context, ModelValue prediction)
        {
            var index = 0;
            return context.Data.TextReader(
                new TextLoader.Arguments()
                {
                    Separator = ",",
                    HasHeader = false,
                    Column = ColumnNames(true /*with label*/, prediction).Select(c => new TextLoader.Column(c, DataKind.R4, index++)).ToArray()
                });
        }
#endif

#endregion
    }
}
