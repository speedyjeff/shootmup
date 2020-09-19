using engine.Common;
using engine.Common.Entities;
using engine.Winforms;
using shootMup.Bots;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace shootMupCore
{
    public partial class shootMup : Form
    {
        public shootMup()
        {
            InitializeComponent();

            try
            {
                SuspendLayout();

                // initial setup
                AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
                AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                ClientSize = new System.Drawing.Size(1484, 1075);
                Name = "shootMup";
                Text = "shootMup";
                DoubleBuffered = true;

                // generate players
                var human = new ShootMPlayer() { Name = "You" };
                var players = new Player[100];
                for (int i = 0; i < players.Length; i++) players[i] = new SimpleAI() { Name = string.Format("ai{0}", i) };

                // generate the world
                World = WorldGenerator.Generate(WorldType.Random, PlayerPlacement.Borders, human, ref players);

                // if we are training for AI, then capture telemetry
                World.OnBeforeAction += AITraining.CaptureBefore;
                World.OnAfterAction += AITraining.CaptureAfter;
                World.OnDeath += (elem) =>
                {
                    if (elem is Player)
                    {
                        var winners = new List<string>();

                        // capture the winners
                        foreach (var player in players)
                        {
                            winners.Add(string.Format("{0} [{1}]", player.Name, player.Kills));
                        }

                        AITraining.CaptureWinners(winners);
                    }
                };

                UI = new UIHookup(this, World);
            }
            finally
            {
                ResumeLayout(false);
            }
        }

        #region protected
        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            if (UI != null)
            {
                UI.ProcessCmdKey(keyData);
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (UI != null)
            {
                UI.ProcessWndProc(ref m);
            }

            base.WndProc(ref m);
        } // WndProc
        #endregion

        #region private
        private UIHookup UI;
        private World World;
        #endregion

    }
}
