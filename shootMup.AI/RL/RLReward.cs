using engine.Common;
using engine.Common.Entities;

namespace shootMup.Bots.RL
{
    public struct RLSnapshot
    {
        public float Health;
        public float Shield;
        public int Kills;
        public bool InZone;
        public bool Alive;

        public static RLSnapshot From(Player p, bool inZone)
        {
            return new RLSnapshot()
            {
                Health = p.Health,
                Shield = p.Shield,
                Kills = p.Kills,
                InZone = inZone,
                Alive = p.Health > 0,
            };
        }
    }

    public static class RLReward
    {
        // Per-step reward shaping. Positive = desirable.
        //   +KillBonus          per kill scored this step
        //   +HealthGainScale    per point of health gained
        //   +ShieldGainScale    per point of shield gained
        //   +PickupBonus        on successful pickup
        //   -HealthLossScale    per point of health lost (extra x InZoneMultiplier when inside the shrinking zone)
        //   -ShieldLossScale    per point of shield lost
        //   +StepCost           per step (small negative, encourages action)
        //   -DeathPenalty       on death (terminal)
        //   +WinBonus           on being the last survivor (terminal, applied externally)
        public const float StepCost = -0.002f;
        public const float HealthGainScale = 0.02f;
        public const float ShieldGainScale = 0.02f;
        public const float HealthLossScale = -0.05f;
        public const float ShieldLossScale = -0.02f;
        public const float KillBonus = 1.0f;
        public const float PickupBonus = 0.05f;
        public const float DeathPenalty = -1.0f;
        public const float WinBonus = 2.0f;
        public const float InZoneMultiplier = 1.5f;

        public static float StepReward(RLSnapshot prev, RLSnapshot curr, ActionEnum action, bool actionResult)
        {
            float r = StepCost;

            var killDelta = curr.Kills - prev.Kills;
            if (killDelta > 0) r += KillBonus * killDelta;

            float hd = curr.Health - prev.Health;
            if (hd > 0) r += HealthGainScale * hd;
            else if (hd < 0)
            {
                var penalty = HealthLossScale * (-hd);
                if (prev.InZone || curr.InZone) penalty *= InZoneMultiplier;
                r += penalty;
            }

            float sd = curr.Shield - prev.Shield;
            if (sd > 0) r += ShieldGainScale * sd;
            else if (sd < 0) r += ShieldLossScale * (-sd);

            if (actionResult && action == ActionEnum.Pickup) r += PickupBonus;

            return r;
        }
    }
}
