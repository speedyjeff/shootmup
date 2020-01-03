using engine.Common;
using engine.Common.Entities;
using engine.Common.Entities.AI;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace shootMup.Bots
{
    public enum TrainedAIModel { ML_Net, OpenCV }

    public class TrainedAI : ShootMAI
    {
        public TrainedAI(TrainedAIModel model) : base()
        {
            // in case it gest stuck
            Rand = new Random();
            LastActionFailed = 0;

            switch(model)
            {
                case TrainedAIModel.ML_Net:
                    ActionModel = ML_ActionModel;
                    XYModel = ML_XYModel;
                    AngleModel = ML_AngleModel;
                    break;
                case TrainedAIModel.OpenCV:
                    ActionModel = CV_ActionModel;
                    XYModel = CV_XYModel;
                    AngleModel = CV_AngleModel;
                    break;
                default:
                    throw new Exception("Unknown AI model type " + model);
            }
        }

        static TrainedAI()
        { 
            // get models
            ML_ActionModel = Model.Load(Path.Combine("Models", "Prebuilt", "action.ml.model"));
            ML_XYModel = Model.Load(Path.Combine("Models", "Prebuilt", "xy.ml.model"));
            ML_AngleModel = Model.Load(Path.Combine("Models", "Prebuilt", "angle.ml.model"));

            CV_ActionModel = Model.Load(Path.Combine("Models", "Prebuilt", "action.cv.model"));
            CV_XYModel = Model.Load(Path.Combine("Models", "Prebuilt", "xy.cv.model"));
            CV_AngleModel = Model.Load(Path.Combine("Models", "Prebuilt", "angle.cv.model"));
        }

        public override ActionEnum Action(List<Element> elements, float angleToCenter, bool inZone, ref float xdelta, ref float ydelta, ref float zdelta, ref float angle)
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
            var pname = "";
            var pammo = 0;
            var pclip = 0;
            var sname = "";
            var sammo = 0;
            var sclip = 0;

            if (Primary != null && Primary is RangeWeapon)
            {
                pname = Primary.GetType().Name;
                pammo = (Primary as RangeWeapon).Ammo;
                pclip = (Primary as RangeWeapon).Clip;
            }
            if (Secondary != null && Secondary.Length == 1 && Secondary[0] != null && Secondary[0] is RangeWeapon)
            {
                sname = Secondary.GetType().Name;
                sammo = (Secondary[0] as RangeWeapon).Ammo;
                sclip = (Secondary[0] as RangeWeapon).Clip;
            }
            var data = new TrainingData()
            {
                // core data
                CenterAngle = angleToCenter,
                InZone = inZone,
                Health = Health,
                Shield = Shield,
                Z = Z,
                Primary = pname,
                PrimaryAmmo = pammo,
                PrimaryClip = pclip,
                Secondary = sname,
                SecondaryAmmo = sammo,
                SecondaryClip = sclip
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

            // pass it along
            base.Feedback(action, item, result);
        }

        #region private
        private Model AngleModel;
        private Model XYModel;
        private Model ActionModel;

        private static Model ML_AngleModel;
        private static Model ML_XYModel;
        private static Model ML_ActionModel;

        private static Model CV_AngleModel;
        private static Model CV_XYModel;
        private static Model CV_ActionModel;

        private Random Rand;
        private int LastActionFailed;
        private float Xdelta;
        private float Ydelta;
        #endregion
    }
}
