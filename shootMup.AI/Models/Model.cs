using shootMup.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace shootMup.Bots
{
    public enum ModelValue { Action, XY, Angle };

    public struct ModelFitness
    {
        public double RMS;
        public double RSquared;
    }

    public abstract class Model
    {
        public Model() { }

        // train
        public Model(IEnumerable<ModelDataSet> data, ModelValue prediction) { }

        // load 
        public Model(string path) { }

        public static Model Load(string path)
        {
            // read the first two bytes of the model and determine what type
            var header = new byte[2];
            using (var reader = File.OpenRead(path))
            {
                reader.Read(header, 0, header.Length);
            }

            if (header[0] == 'P' && header[1] == 'K')
            {
                // ml.net
                return new ModelMLNet(path);
            }
            else if (header[0] == '%')
            {
                return new ModelOpenCV(path);
            }
            else throw new Exception("Unknown file format header : " + header[0] + " " + header[1]);
        }

        public virtual void Save(string path) { }

        public virtual ModelFitness Evaluate(List<ModelDataSet> data, ModelValue prediction)
        {
            return default(ModelFitness);
        }

        public bool Predict(ModelDataSet data, out float xdelta, out float ydelta)
        {
            var angle = Predict(data);

            // set course
            float x1, y1;
            Collision.CalculateLineByAngle(0, 0, angle, 1, out x1, out y1, out xdelta, out ydelta);

            // normalize
            var sum = (float)(Math.Abs(xdelta) + Math.Abs(ydelta));
            xdelta = xdelta / sum;
            ydelta = ydelta / sum;

            if (Math.Abs(xdelta) + Math.Abs(ydelta) > 1.0001) throw new Exception("Invalid xdelta,ydelta : " + xdelta + "," + ydelta);

            return true;
        }

        public virtual float Predict(ModelDataSet data)
        {
            return 0;
        }
    }
}
