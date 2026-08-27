using Learning;
using System;
using System.Collections.Generic;
using System.IO;

namespace shootMup.Bots.RL
{
    // DQN-style agent built on the raw NeuralNetwork primitives.
    // We do not use the library's DeepQ class because it assumes a categorical
    // (one-hot) state; we have a continuous 26-float state.
    public class RLAgent
    {
        public struct Options
        {
            public int StateSize;
            public int ActionCount;
            public int[] Hidden;
            public float LearningRate;
            public float Discount;
            public float EpsilonStart;
            public float EpsilonMin;
            public float EpsilonDecay;
            public int ReplayCapacity;
            public int BatchSize;
            public int MinReplayBeforeLearn;
            public int TargetSyncSteps;
            public int? Seed;

            public static Options Default()
            {
                return new Options()
                {
                    StateSize = RLState.FeatureCount,
                    ActionCount = RLActionSpace.ActionCount,
                    Hidden = new int[] { 64, 64 },
                    LearningRate = 0.001f,
                    Discount = 0.99f,
                    EpsilonStart = 1.0f,
                    EpsilonMin = 0.05f,
                    EpsilonDecay = 0.9999f,
                    ReplayCapacity = 10_000,
                    BatchSize = 32,
                    MinReplayBeforeLearn = 200,
                    TargetSyncSteps = 500,
                    Seed = null,
                };
            }
        }

        private struct Transition
        {
            public float[] State;
            public int Action;
            public float Reward;
            public float[] NextState;
            public bool Done;
        }

        private readonly Options Opts;
        private readonly Random Rand;
        private readonly object Sync = new object();
        private NeuralNetwork Main;
        private NeuralNetwork Target;
        private readonly List<Transition> Memory;
        private int StepsSinceSync;
        private int TotalSteps;

        public float Epsilon { get; set; }
        public int TrainingUpdates { get; private set; }

        public RLAgent(Options opts)
        {
            Opts = opts;
            Rand = opts.Seed.HasValue ? new Random(opts.Seed.Value) : new Random();
            Epsilon = opts.EpsilonStart;
            Memory = new List<Transition>(opts.ReplayCapacity);
            StepsSinceSync = 0;
            TotalSteps = 0;

            Main = new NeuralNetwork(new NeuralOptions()
            {
                InputNumber = opts.StateSize,
                OutputNumber = opts.ActionCount,
                HiddenLayerNumber = opts.Hidden,
                LearningRate = opts.LearningRate,
                MinibatchCount = 1,
                ParallizeExecution = false,
                WeightInitialization = NeuralWeightInitialization.He,
                BiasInitialization = NeuralBiasInitialization.Zero,
            });
            Target = NeuralNetwork.Load(Main);
        }

        private RLAgent(Options opts, NeuralNetwork loaded)
        {
            Opts = opts;
            Rand = opts.Seed.HasValue ? new Random(opts.Seed.Value) : new Random();
            Epsilon = opts.EpsilonMin;
            Memory = new List<Transition>(opts.ReplayCapacity);
            Main = loaded;
            Target = NeuralNetwork.Load(loaded);
        }

        public int ChooseAction(float[] state, bool explore)
        {
            if (explore && Rand.NextDouble() < Epsilon)
            {
                return Rand.Next(Opts.ActionCount);
            }
            lock (Sync)
            {
                var output = Main.Evaluate(state);
                return output.Result;
            }
        }

        public float[] EvaluateProbabilities(float[] state)
        {
            lock (Sync)
            {
                return Main.Evaluate(state).Probabilities;
            }
        }

        // Record a transition and perform one minibatch Bellman update when ready.
        public void Observe(float[] state, int action, float reward, float[] nextState, bool done)
        {
            lock (Sync)
            {
                ObserveLocked(state, action, reward, nextState, done);
            }
        }

        private void ObserveLocked(float[] state, int action, float reward, float[] nextState, bool done)
        {
            if (Memory.Count >= Opts.ReplayCapacity) Memory.RemoveAt(0);
            Memory.Add(new Transition()
            {
                State = state,
                Action = action,
                Reward = reward,
                NextState = nextState,
                Done = done,
            });
            TotalSteps++;

            if (Memory.Count < Opts.MinReplayBeforeLearn) return;

            for (int i = 0; i < Opts.BatchSize; i++)
            {
                var t = Memory[Rand.Next(Memory.Count)];

                var mainOut = Main.Evaluate(t.State);

                float tdTarget = t.Reward;
                if (!t.Done)
                {
                    var tgtOut = Target.Evaluate(t.NextState);
                    float maxQ = float.MinValue;
                    for (int a = 0; a < tgtOut.Probabilities.Length; a++)
                    {
                        if (tgtOut.Probabilities[a] > maxQ) maxQ = tgtOut.Probabilities[a];
                    }
                    tdTarget += Opts.Discount * maxQ;
                }

                // Preferred = current probabilities with the taken action overwritten
                // by the TD target. NeuralNetwork.Learn pushes the output distribution
                // toward that signal for this sample.
                var preferred = new float[mainOut.Probabilities.Length];
                Array.Copy(mainOut.Probabilities, preferred, preferred.Length);
                preferred[t.Action] = tdTarget;
                Main.Learn(mainOut, preferred);

                TrainingUpdates++;
            }

            if (Epsilon > Opts.EpsilonMin)
            {
                Epsilon = Math.Max(Opts.EpsilonMin, Epsilon * Opts.EpsilonDecay);
            }

            StepsSinceSync++;
            if (StepsSinceSync >= Opts.TargetSyncSteps)
            {
                Target = NeuralNetwork.Load(Main);
                StepsSinceSync = 0;
            }
        }

        public void Save(string filename)
        {
            var dir = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            lock (Sync)
            {
                Main.Save(filename);
            }
        }

        public static RLAgent Load(string filename, Options? overrideOpts = null)
        {
            var nn = NeuralNetwork.Load(filename);
            var opts = overrideOpts ?? Options.Default();
            opts.StateSize = RLState.FeatureCount;
            opts.ActionCount = RLActionSpace.ActionCount;
            return new RLAgent(opts, nn);
        }

        public static bool ModelFileExists(string filename) => File.Exists(filename);
    }
}
