using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;

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
                IDataView dataView = textLoader.Load(pathToData);
                InputSchema = dataView.Schema;

                // configurations
                var label = "";
                switch (prediction)
                {
                    case ModelValue.Action: label = "Action"; break;
                    case ModelValue.Angle: label = "FaceAngle"; break;
                    case ModelValue.XY: label = "MoveAngle"; break;
                    default: throw new Exception("Unknown value for prediction : " + prediction);
                }
                var dataPipeline = Context.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: label)
                    .Append(Context.Transforms.Concatenate("Features", ColumnNames(false /* do not include the label */, prediction)));

                // set the training algorithm
                var trainer = Context.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features");
                var trainingPipeline = dataPipeline.Append(trainer);

                TrainedModel = trainingPipeline.Fit(dataView);
            }
            finally
            {
                // cleanup
                if (!string.IsNullOrWhiteSpace(pathToData) && File.Exists(pathToData)) File.Delete(pathToData);
            }
        }

        // load
        public ModelMLNet(string path)
        {
            Context = new MLContext();

            // load
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                TrainedModel = Context.Model.Load(stream, out InputSchema);
            }

            // create the prediction function
            PredictFunc = Context.Model.CreatePredictionEngine<ModelDataSet, ModelDataSetPrediction>(TrainedModel, InputSchema);
        }

        public override void Save(string path)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");

            lock (TrainedModel)
            {
                // save
                using (var stream = File.Create(path))
                {
                    Context.Model.Save(TrainedModel, InputSchema, stream);
                }
            }
        }

        public override ModelFitness Evaluate(List<ModelDataSet> data, ModelValue prediction)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");

            lock (TrainedModel)
            {
                var textLoader = GetTextLoader(Context, prediction);

                var pathToData = "";
                try
                {
                    // ugh have to spill data to disk for it to work!
                    pathToData = WriteToDisk(data, prediction);

                    IDataView dataView = textLoader.Load(pathToData);
                    var predictions = TrainedModel.Transform(dataView);
                    var metrics = Context.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

                    return new ModelFitness()
                    {
                        RMS = metrics.RootMeanSquaredError,
                        RSquared = metrics.RSquared
                    };
                }
                finally
                {
                    // cleanup
                    if (!string.IsNullOrWhiteSpace(pathToData) && File.Exists(pathToData)) File.Delete(pathToData);
                }
            }
        }

        public override float Predict(ModelDataSet data)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");

            lock (TrainedModel)
            {
                var result = PredictFunc.Predict(data);
                return result.Score;
            }
        }

#region private

        private MLContext Context;
        private ITransformer TrainedModel;
        private DataViewSchema InputSchema;
        private PredictionEngine<ModelDataSet, ModelDataSetPrediction> PredictFunc;

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
            return context.Data.CreateTextLoader(
                columns: ColumnNames(true /*with label*/, prediction).Select(c => new TextLoader.Column(c, DataKind.Single, index++)).ToArray(),
                separatorChar: ',', 
                hasHeader: false);
        }

#endregion
    }
}
