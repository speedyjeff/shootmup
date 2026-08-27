using engine.Common;
using engine.Common.Entities;
using shootMup.Bots.RL;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace shootMup.Bots.Training
{
    public static class RLRunner
    {
        // Usage: rl [episodes] [modelPath] [opponents=simple|random] [secondsPerEpisode]
        public static int Run(int episodes, string modelPath, string opponents, int secondsPerEpisode)
        {
            if (episodes <= 0) episodes = 100;
            if (secondsPerEpisode <= 0) secondsPerEpisode = 60;
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                modelPath = Path.Combine("Models", "Prebuilt", "rl.nn.model");
            }
            if (string.IsNullOrWhiteSpace(opponents)) opponents = "simple";

            // Preload the embedded image/sound resources so that World.Paint
            // doesn't throw when we initialize the (void) graphics surface.
            Initialize.LoadResources((name, bytes) => { /* headless: no sound loader */ });

            // Reuse or create the agent
            RLAgent agent;
            if (RLAgent.ModelFileExists(modelPath))
            {
                Console.WriteLine("Resuming from existing model at {0}", modelPath);
                agent = RLAgent.Load(modelPath);
                agent.Epsilon = 0.5f; // mid-range when resuming
            }
            else
            {
                Console.WriteLine("Creating new RL agent at {0}", modelPath);
                agent = new RLAgent(RLAgent.Options.Default());
            }

            // Per-episode CSV log
            var logDir = Path.GetDirectoryName(modelPath);
            if (string.IsNullOrEmpty(logDir)) logDir = ".";
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "rl.training.csv");
            var writeHeader = !File.Exists(logPath);
            using var log = File.AppendText(logPath);
            if (writeHeader) log.WriteLine("episode,ticks,alive,rl_alive,transitions,total_reward,epsilon,updates");

            Console.WriteLine("Starting RL training: {0} episodes, opponents={1}", episodes, opponents);

            for (int ep = 0; ep < episodes; ep++)
            {
                // Build a match: 4 RL bots + 96 opponents
                var human = new ShootMPlayer() { Name = "human" };
                var players = new Player[100];
                var rlList = new List<RLAI>();
                for (int i = 0; i < players.Length; i++)
                {
                    if (i < 4)
                    {
                        var rl = new RLAI(agent, RLMode.Training) { Name = $"rl{i}" };
                        players[i] = rl;
                        rlList.Add(rl);
                    }
                    else
                    {
                        players[i] = MakeOpponent(opponents, i);
                    }
                }

                var world = WorldGenerator.Generate(WorldType.Random, PlayerPlacement.Borders, human, ref players);
                world.InitializeGraphics(new VoidSurface(), new VoidSound());

                var trainer = new RLTrainer(agent, rlList);
                trainer.Attach(world);

                world.OnPaused += () => null;
                world.KeyPress(Constants.Esc);

                var timer = Stopwatch.StartNew();
                var maxMs = secondsPerEpisode * 1000;
                while (world.Alive > 1 && timer.ElapsedMilliseconds < maxMs)
                {
                    System.Threading.Thread.Sleep(500);
                }
                timer.Stop();
                world.KeyPress(Constants.Esc);

                // count how many RL bots survived
                int rlAlive = 0;
                foreach (var rl in rlList)
                {
                    if (rl.Health > 0) rlAlive++;
                }
                if (rlAlive > 0) trainer.FinalizeEpisode();

                trainer.Detach(world);

                log.WriteLine($"{ep},{timer.ElapsedMilliseconds},{world.Alive},{rlAlive},{trainer.TotalTransitions},{trainer.TotalReward:F2},{agent.Epsilon:F3},{agent.TrainingUpdates}");
                log.Flush();

                Console.WriteLine("episode {0}: ticks={1}ms alive={2} rlAlive={3} trans={4} reward={5:F2} eps={6:F3}",
                    ep, timer.ElapsedMilliseconds, world.Alive, rlAlive, trainer.TotalTransitions, trainer.TotalReward, agent.Epsilon);

                // Save every 10 episodes (and at the end)
                if ((ep + 1) % 10 == 0 || ep == episodes - 1)
                {
                    agent.Save(modelPath);
                    Console.WriteLine("  saved -> {0}", modelPath);
                }
            }

            Console.WriteLine("RL training complete: {0} episodes, {1} total updates", episodes, agent.TrainingUpdates);
            return 0;
        }

        private static Player MakeOpponent(string kind, int index)
        {
            switch (kind.ToLower())
            {
                case "random": return new RandomAI() { Name = $"ai{index}" };
                case "simple":
                default: return new SimpleAI() { Name = $"ai{index}" };
            }
        }
    }
}
