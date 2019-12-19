using shootMup.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace shootMup.Bots
{
    public class TrainedAI : AI
    {
        public TrainedAI() : base()
        {
            // never record training data for these AI
            RecordTraining = false;

            // in case it gest stuck
            Rand = new Random();
            LastActionFailed = 0;
        }

        static TrainedAI()
        { 
            // get models
            ActionModel = Model.Load(Path.Combine("Models", "Prebuilt", "action.ml.model"));
            XYModel = Model.Load(Path.Combine("Models", "Prebuilt", "xy.ml.model"));
            AngleModel = Model.Load(Path.Combine("Models", "Prebuilt", "angle.ml.model"));            
        }

        public override ActionEnum Action(List<Element> elements, float angleToCenter, bool inZone, ref float xdelta, ref float ydelta, ref float angle)
        {
            // check if last action failed
            if (LastActionFailed > 0)
            {
                LastActionFailed--;
                xdelta = Xdelta;
                ydelta = Ydelta;
                return ActionEnum.Move;
            }

            // construct a view of the current world
            var data = new TrainingData()
            {
                // core data
                CenterAngle = angleToCenter,
                InZone = inZone,
                Health = Health,
                Shield = Shield,
                Z = Z,
                Primary = Primary != null ? Primary.GetType().Name : "",
                PrimaryAmmo = Primary != null ? Primary.Ammo : 0,
                PrimaryClip = Primary != null ? Primary.Clip : 0,
                Secondary = Secondary != null ? Secondary.GetType().Name : "",
                SecondaryAmmo = Secondary != null ? Secondary.Ammo : 0,
                SecondaryClip = Secondary != null ? Secondary.Clip : 0
            };
            data.Proximity = AITraining.ComputeProximity(this, elements).Values.ToList();

            // use the model to predict its actions
            var modeldataset = data.AsModelDataSet();
            int iAction = 0;
            {
                iAction = (int)Math.Round(ActionModel.Predict(modeldataset));
                angle = AngleModel.Predict(modeldataset);
                XYModel.Predict(modeldataset, out xdelta, out ydelta);
            }

            // do some sanity checking...
            if (iAction < 0 || iAction >= (int)ActionEnum.COUNT) iAction = (int)ActionEnum.Move;
            if (Math.Abs(xdelta) + Math.Abs(ydelta) > 1.00001) throw new Exception("xdelta and ydelta are invalid");
            while (angle < 0) angle += 360;
            while (angle >= 360) angle -= 360;
            if (angle < 0 || angle >= 360) throw new Exception("Invalid angle: " + angle);

            return (ActionEnum)iAction;
        }

        public override void Feedback(ActionEnum action, object item, bool result)
        {
            // if last action failed, choose a random direction for next time
            if (!result)
            {
                var angle = Rand.Next() % 360;
                float x1, x2;
                Collision.CalculateLineByAngle(0, 0, angle, 1, out x1, out x2, out Xdelta, out Ydelta);
                var sum = (float)(Math.Abs(Xdelta) + Math.Abs(Ydelta));
                Xdelta = Xdelta / sum;
                Ydelta = Ydelta / sum;
                //LastActionFailed = 10;
            }
        }

        #region private
        private static Model AngleModel;
        private static Model XYModel;
        private static Model ActionModel;

        private Random Rand;
        private int LastActionFailed;
        private float Xdelta;
        private float Ydelta;
        #endregion
    }
}
