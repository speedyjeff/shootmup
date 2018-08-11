using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace shootMup.Common
{
    public class TrainedAI : AI
    {
        public TrainedAI() : base()
        {
            // never record training data for these AI
            RecordTraining = false;

            // get models
            ActionModel = AITraining.GetActionModel();
            XYModel = AITraining.GetXYModel();
            AngleModel = AITraining.GetAngleModel();            
        }

        public override ActionEnum Action(List<Element> elements, float angleToCenter, bool inZone, ref float xdelta, ref float ydelta, ref float angle)
        {
            // construct a view of the current world
            var data = new TrainingData()
            {
                // core data
                CenterAngle = angleToCenter,
                InZone = inZone,
                Health = Health,
                Sheld = Sheld,
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
            var modeldataset = data.ToModelDataSet();
            int iAction = (int)ActionModel.Predict(modeldataset);
            angle = AngleModel.Predict(modeldataset);
            XYModel.Predict(modeldataset, out xdelta, out ydelta);

            // do some sanity checking...
            if (iAction < 0 || iAction >= (int)ActionEnum.ZoneDamage) throw new Exception("Unknown action : " + iAction);
            if (Math.Abs(xdelta) + Math.Abs(ydelta) > 1.00001) throw new Exception("Incorrect delta");
            if (angle < 0 || angle > 360) throw new Exception("Incorrect angle : " + angle);

            return (ActionEnum)iAction;
        }

        #region private
        private Model AngleModel;
        private Model XYModel;
        private Model ActionModel;
        #endregion
    }
}
