using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Bots
{
    public class AI : Player
    {
        public AI() : base()
        {
            ShowDamage = true;
            DisplayHud = false;
            Color = new RGBA() { R = 0, G = 0, B = 255, A = 255 };
            ShowDiagnostics = Constants.Debug_AIMoveDiag;
        }

        public volatile int RunningState;
        public bool ShowDiagnostics { get; protected set; }
        
        public virtual ActionEnum Action(List<Element> elements, float angleToCenter, bool inZone, ref float xdelta, ref float ydelta, ref float angle)
        {
            return ActionEnum.None;
        }

        public virtual void Feedback(ActionEnum action, object item, bool result)
        {
        }
    }
}
