using OpenCvSharp;
using OpenCvSharp.ML;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Bots
{
    public class ModelOpenCV : Model
    {
        // train
        public ModelOpenCV(List<ModelDataSet> input, ModelValue prediction)
        {
            if (input == null || input.Count == 0) throw new Exception("Must have valid input");

            // convert features into proper form
            var features = new float[input.Count, input[0].Features()];
            var labels = new float[input.Count];

            for(int i=0; i<input.Count; i++)
            {
                for (int j = 0; j < input[i].Features(); j++)
                    features[i,j] = input[i].Feature(j);

                switch(prediction)
                {
                    case ModelValue.Action: labels[i] = input[i].Action; break;
                    case ModelValue.Angle: labels[i] = input[i].FaceAngle; break;
                    case ModelValue.XY: labels[i] = input[i].MoveAngle; break;
                    default: throw new Exception("Unknown prediction type : " + prediction);
                }
            }

            // train
            var labelInput = InputArray.Create<float>(labels);
            var dataInput = InputArray.Create<float>(features);

            TrainedModel = RTrees.Create();
            TrainedModel.RegressionAccuracy = 0.00001f;
            // RTrees.MaxDepath (r^2)
            //  default - action 0.3424, xy 0.1735, angle 0.2208
            //    5     - 
            //   20     - action 0.6482, xy 0.5912, angle 0.6414 (new default)
            //  100     - action 0.6408, xy 0.5914, angle 0.6419
            TrainedModel.MaxDepth = 20;
            // RTress.MinSampleCount
            //  default(10) - see 20 above
            //     1        - actions 0.6625, xy 0.5077, angle 0.6376
            //    50        - actions 0.6464, xy 0.5627, angle 0.6217
            //TrainedModel.MinSampleCount = 1;

            // fails
            //TrainedModel = LogisticRegression.Create();
            
            //  fails
            // TrainedModel = DTrees.Create();

            // failed
            //TrainedModel = SVM.Create();
            //TrainedModel.KernelType = SVM.KernelTypes.Linear;
            //TrainedModel.Type = SVM.Types.NuSvr;
            //TrainedModel.C = 1;
            //TrainedModel.P = 0.01;
            //TrainedModel.Gamma = 10f;
            //TrainedModel.Degree = 0.1;
            //TrainedModel.Coef0 = 0;
            //TrainedModel.Nu = 0.1;

            TrainedModel.Train(dataInput, SampleTypes.RowSample, labelInput);
        }

        // load
        public ModelOpenCV(string path)
        {
            TrainedModel = RTrees.Load(path);
        }

        public override void Save(string path)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");

            TrainedModel.Save(path);
        }

        public override ModelFitness Evaluate(List<ModelDataSet> data, ModelValue prediction)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");
            if (data.Count == 0) return default(ModelFitness);

            // values
            var predictions = new float[data.Count];
            var labels = new float[data.Count];
            
            for(int i=0; i<data.Count; i++)
            {
               predictions[i] = Predict(data[i]);
                switch (prediction)
                {
                    case ModelValue.Action: labels[i] = (float)data[i].Action; break;
                    case ModelValue.Angle: labels[i] = data[i].FaceAngle; break;
                    case ModelValue.XY: labels[i] = data[i].MoveAngle; break;
                    default: throw new Exception("Unknown prediction type : " + prediction);
                }
            }

            // calculate rms
            // https://sciencing.com/calculate-rms-5104500.html
            // rms(a,b,c) = sqrt((a^2 + b^2 + c^2)/3); 
            var rms = 0d;
            for(int i=0; i<predictions.Length; i++)
            {
                rms = Math.Pow(predictions[i] - labels[i], 2);
            }
            rms /= predictions.Length;
            rms = Math.Sqrt(rms);

            // calculate r^2
            // https://www.sapling.com/5117357/calculate-rsquared
            // R = (Count (sum of ab) - (sum of a)(sum of b)) / [sqrt((Count(sum a^2) - (sum of a)^2)(Count *(sum of b^2) - (sum of b)^2)]
            //  a == labels
            //  b == prediction
            var ab = 0d;
            var a = 0d;
            var b = 0d;
            var a2 = 0d;
            var b2 = 0d;
            for (int i = 0; i < predictions.Length; i++)
            {
                a += labels[i];
                b += predictions[i];
                ab += (labels[i] * predictions[i]);
                a2 += Math.Pow(labels[i], 2);
                b2 += Math.Pow(predictions[i], 2);
            }
            var r2 = ((predictions.Length * ab) - (a * b)) / Math.Sqrt(((predictions.Length * a2) - Math.Pow(a, 2)) * (predictions.Length * b2 - Math.Pow(b, 2)));
            r2 = Math.Pow(r2, 2);

            return new ModelFitness()
            {
                RMS = rms,
                RSquared = r2
            };
        }

        public override float Predict(ModelDataSet input)
        {
            if (TrainedModel == null) throw new Exception("Must initialize the model before calling");

            lock (TrainedModel)
            {
                // cache for reuse
                if (PredictInput == null) PredictInput = new float[input.Features()];
                for (int i = 0; i < PredictInput.Length; i++)
                    PredictInput[i] = input.Feature(i);

                using (var arr = InputArray.Create<float>(PredictInput))
                {
                    return TrainedModel.Predict(arr);
                }
            }
        }

        #region private
        private RTrees TrainedModel;
        private float[] PredictInput;
        #endregion
    }
}
