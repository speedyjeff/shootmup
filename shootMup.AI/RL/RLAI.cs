using engine.Common;
using engine.Common.Entities;
using System.Collections.Generic;

namespace shootMup.Bots.RL
{
    public enum RLMode
    {
        // Greedy exploitation only; used for playing games with a trained model.
        Inference,
        // Epsilon-greedy exploration and writes transitions back to the agent.
        Training,
    }

    // RL-based bot. The agent (RLAgent) is shared across multiple RLAI instances
    // during training so that all bots contribute transitions to the same replay
    // buffer. In inference mode a single loaded agent can be reused freely.
    public class RLAI : ShootMAI
    {
        public RLAgent Agent { get; }
        public RLMode Mode { get; }

        // Most-recent (state, action) pair for this player. A trainer uses these
        // to build (s, a, r, s') transitions once it sees the next before-action.
        public float[] LastState { get; private set; }
        public int LastAction { get; private set; } = -1;
        public bool LastActionResult { get; private set; }
        public ActionEnum LastDecodedAction { get; private set; } = ActionEnum.None;
        public RLSnapshot LastSnapshot { get; private set; }

        public RLAI(RLAgent agent, RLMode mode) : base()
        {
            Agent = agent;
            Mode = mode;
        }

        public override ActionEnum Action(
            List<Element> elements,
            float angleToCenter,
            bool inZone,
            ref float xdelta,
            ref float ydelta,
            ref float zdelta,
            ref float angle)
        {
            var state = RLState.Build(this, elements, angleToCenter, inZone);
            var explore = Mode == RLMode.Training;
            var actionIndex = Agent.ChooseAction(state, explore);

            var decoded = RLActionSpace.Decode(actionIndex, this, elements, Angle, out var ax, out var ay, out var aang);
            xdelta = ax;
            ydelta = ay;
            angle = aang;
            zdelta = 0f;

            LastState = state;
            LastAction = actionIndex;
            LastDecodedAction = decoded;
            LastSnapshot = RLSnapshot.From(this, inZone);

            return decoded;
        }

        public override void Feedback(ActionEnum action, object item, bool result)
        {
            // The engine emits a Move-feedback after every step (engine/World.cs) — ignore
            // that one when our decoded action was not Move, so LastActionResult tracks
            // the result of the action we actually chose.
            if (action == LastDecodedAction) LastActionResult = result;
            base.Feedback(action, item, result);
        }
    }
}
