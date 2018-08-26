using shootMup.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

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
            var type = GetFileType(ref path);

            if (type == FileType.ML) return new ModelMLNet(path);
            else if (type == FileType.CV) return new ModelOpenCV(path);
            else throw new Exception("Unknown file format header : " + type);
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

        #region private
        enum FileType { Zip, CV, ML };
        private static FileType GetFileType(ref string path)
        {
            var loops = 0;
            do
            {
                var header = new byte[2];
                using (var reader = File.OpenRead(path))
                {
                    reader.Read(header, 0, header.Length);
                }

                if (header[0] == 'P' && header[1] == 'K')
                {
                    // this is a zip file, open it to check
                    using (var zip = ZipFile.OpenRead(path))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            if (entry.FullName.EndsWith(".model"))
                            {
                                // this is likely a CV file, but we need to extract and check
                                var extractPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path), "fromzip"));
                                var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                                // Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
                                // are case-insensitive.
                                if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                                {
                                    Directory.CreateDirectory(extractPath);
                                    path = destinationPath;
                                    entry.ExtractToFile(destinationPath);
                                }
                                break;
                            }
                            else
                                return FileType.ML;

                        }
                    }
                }
                else if (header[0] == '%')
                {
                    return FileType.CV;
                }
            }
            while (loops++ < 1);

            throw new Exception("Unknown file type");
        }
        #endregion
    }
}
