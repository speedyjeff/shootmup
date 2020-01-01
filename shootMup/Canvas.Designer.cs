using engine.Common;
using engine.Common.Entities;
using engine.Winforms;
using shootMup.Bots;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace shootMup
{
    partial class Canvas
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private UIHookup UI;
        private World World;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
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
                for(int i=0; i<players.Length; i++) players[i] = new SimpleAI() { Name = string.Format("ai{0}", i) };

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
                            winners.Add(string.Format("{0} [{1}]", player.Name, player.Kills) );
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
    }
}

