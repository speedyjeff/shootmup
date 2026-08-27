using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;

namespace shootMup.Bots.RL
{
    // Wires World events to the shared RLAgent so that (s, a, r, s') transitions
    // can be captured across multiple RLAI bots during headless training.
    //
    // Usage:
    //   var trainer = new RLTrainer(agent, rlPlayers);
    //   trainer.Attach(world);
    //   ... run game ...
    //   trainer.FinalizeEpisode(winners);  // writes terminal rewards
    //   trainer.Detach(world);
    public class RLTrainer
    {
        private readonly RLAgent Agent;
        private readonly Dictionary<int, RLAI> Players;
        private readonly Dictionary<int, Pending> Pendings;
        private readonly Dictionary<int, bool> LastInZone;
        private readonly object Sync = new object();

        // Per-episode stats (backed by thread-safe counters).
        public int TotalTransitions => _transitionCount;
        public double TotalReward => Rewards;

        private Action<Player, ActionDetails> _onBefore;
        private Action<Player, ActionEnum, bool> _onAfter;
        private Action<Element> _onDeath;

        private struct Pending
        {
            public float[] State;
            public int Action;
            public ActionEnum Decoded;
            public RLSnapshot Snapshot;
            public bool HasValue;
        }

        public RLTrainer(RLAgent agent, IEnumerable<RLAI> rlPlayers)
        {
            Agent = agent;
            Players = new Dictionary<int, RLAI>();
            Pendings = new Dictionary<int, Pending>();
            LastInZone = new Dictionary<int, bool>();
            foreach (var p in rlPlayers)
            {
                Players[p.Id] = p;
                Pendings[p.Id] = default;
                LastInZone[p.Id] = false;
            }
        }

        public void Attach(World world)
        {
            _onBefore = OnBefore;
            _onAfter = OnAfter;
            _onDeath = OnDeath;
            world.OnBeforeAction += _onBefore;
            world.OnAfterAction += _onAfter;
            world.OnDeath += _onDeath;
        }

        public void Detach(World world)
        {
            if (_onBefore != null) world.OnBeforeAction -= _onBefore;
            if (_onAfter != null) world.OnAfterAction -= _onAfter;
            if (_onDeath != null) world.OnDeath -= _onDeath;
        }

        // Called before every action the player takes. By this point RLAI.Action()
        // has already run (during the engine's step) and stashed LastState/LastAction,
        // so we can complete any pending transition from the previous step.
        private void OnBefore(Player player, ActionDetails details)
        {
            if (!Players.TryGetValue(player.Id, out var ai)) return;

            Pending priorPending;
            float[] nextState;
            RLSnapshot currSnap;
            bool priorResult;
            bool hasPrior;
            lock (Sync)
            {
                LastInZone[player.Id] = details.InZone;
                hasPrior = Pendings.TryGetValue(player.Id, out priorPending) && priorPending.HasValue && ai.LastState != null;
                nextState = ai.LastState;
                currSnap = RLSnapshot.From(player, details.InZone);
                priorResult = ai.LastActionResult;

                Pendings[player.Id] = new Pending()
                {
                    State = ai.LastState,
                    Action = ai.LastAction,
                    Decoded = ai.LastDecodedAction,
                    Snapshot = ai.LastSnapshot,
                    HasValue = ai.LastState != null && ai.LastAction >= 0,
                };
            }

            if (hasPrior)
            {
                var reward = RLReward.StepReward(priorPending.Snapshot, currSnap, priorPending.Decoded, priorResult);
                Agent.Observe(priorPending.State, priorPending.Action, reward, nextState, done: false);
                System.Threading.Interlocked.Increment(ref _transitionCount);
                AddReward(reward);
            }
        }

        private int _transitionCount;
        private double _rewardSum;
        private void AddReward(double r)
        {
            lock (Sync) { _rewardSum += r; }
        }

        private void OnAfter(Player player, ActionEnum action, bool result)
        {
            // Action result is captured by RLAI.Feedback (set on the bot itself).
        }

        private void OnDeath(Element element)
        {
            if (!(element is Player player)) return;
            if (!Players.TryGetValue(player.Id, out var ai)) return;

            Pending pending;
            bool inZone;
            lock (Sync)
            {
                if (!Pendings.TryGetValue(player.Id, out pending) || !pending.HasValue) return;
                inZone = LastInZone.TryGetValue(player.Id, out var iz) && iz;
                Pendings[player.Id] = default;
            }

            var currSnap = RLSnapshot.From(player, inZone);
            currSnap.Alive = false;
            var reward = RLReward.StepReward(pending.Snapshot, currSnap, pending.Decoded, ai.LastActionResult)
                       + RLReward.DeathPenalty;

            Agent.Observe(pending.State, pending.Action, reward, ai.LastState ?? pending.State, done: true);
            System.Threading.Interlocked.Increment(ref _transitionCount);
            AddReward(reward);
        }

        public void FinalizeEpisode()
        {
            List<KeyValuePair<int, Pending>> snapshot;
            lock (Sync)
            {
                snapshot = new List<KeyValuePair<int, Pending>>(Pendings);
                Pendings.Clear();
            }
            foreach (var kv in snapshot)
            {
                var pending = kv.Value;
                if (!pending.HasValue) continue;
                if (!Players.TryGetValue(kv.Key, out var ai)) continue;

                var bonus = RLReward.WinBonus;
                Agent.Observe(pending.State, pending.Action, bonus, ai.LastState ?? pending.State, done: true);
                System.Threading.Interlocked.Increment(ref _transitionCount);
                AddReward(bonus);
            }
        }

        // Expose internal counters.
        public int Transitions => _transitionCount;
        public double Rewards { get { lock (Sync) return _rewardSum; } }
    }
}
