using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace shootMup
{
    partial class Canvas
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private WritableGraphics Surface;

        private World World;

        private Timer OnPaintTimer;
        private Timer OnMoveTimer;

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

                // double buffer
                Surface = new WritableGraphics( BufferedGraphicsManager.Current, CreateGraphics(), Height, Width);

                // timers
                OnPaintTimer = new Timer();
                OnPaintTimer.Interval = Common.Constants.GlobalClock / 2;
                OnPaintTimer.Tick += OnPaintTimer_Tick;
                OnMoveTimer = new Timer();
                OnMoveTimer.Interval = Common.Constants.GlobalClock / 2;
                OnMoveTimer.Tick += OnMoveTimer_Tick;

                // setup game
                World = new World(Surface, new Sounds());

                // setup callbacks
                KeyPress += OnKeyPressed;
                MouseUp += OnMouseUp;
                MouseDown += OnMouseDown;
                MouseMove += OnMouseMove;
                MouseWheel += OnMouseWheel;
                Resize += OnResize;

                OnPaintTimer.Start();
         }
            finally
            {
                ResumeLayout(false);
            }
        }

        private void OnPaintTimer_Tick(object sender, EventArgs e)
        {
            Stopwatch duration = new Stopwatch();
            duration.Start();
            World.Paint();
            Refresh();
            duration.Stop();
            if (duration.ElapsedMilliseconds > (Common.Constants.GlobalClock / 2)-5) System.Diagnostics.Debug.WriteLine("**Paint Duration {0} ms", duration.ElapsedMilliseconds);
            //System.IO.File.AppendAllText("timing.tsv", duration.ElapsedMilliseconds.ToString() + System.Environment.NewLine);
        }

        private void OnMoveTimer_Tick(object sender, EventArgs e)
        {
            World.KeyPress(Common.Constants.RightMouse);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
                Surface.RawRender(e.Graphics);
        }

        private void OnResize(object sender, EventArgs e)
        {
            Surface.RawResize(CreateGraphics(), Height, Width);
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            World.Mousewheel(e.Delta);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // translate the location into an angle relative to the mid point
            //        360/0
            //   270         90
            //         180

            // Width/2 and Height/2 act as the center point
            float angle = Common.Collision.CalculateAngleFromPoint(Width / 2.0f, Height / 2.0f, e.X, e.Y);

            World.Mousemove(e.X, e.Y, angle);
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) World.KeyPress(Common.Constants.LeftMouse);
            else if (e.Button == MouseButtons.Right) OnMoveTimer.Start();
            else if (e.Button == MouseButtons.Middle) World.KeyPress(Common.Constants.MiddleMouse);
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) OnMoveTimer.Stop();
        }

        private void OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            World.KeyPress(e.KeyChar);
            e.Handled = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // user input
            if (keyData == Keys.Left) World.KeyPress(Common.Constants.LeftArrow);
            else if (keyData == Keys.Right) World.KeyPress(Common.Constants.RightArrow);
            else if (keyData == Keys.Up) World.KeyPress(Common.Constants.UpArrow);
            else if (keyData == Keys.Down) World.KeyPress(Common.Constants.DownArrow);
            else if (keyData == Keys.Space) World.KeyPress(Common.Constants.Space);
            else if (keyData == Keys.Escape) World.KeyPress(Common.Constants.Esc);

            // command control
            else if (keyData == Keys.Tab) throw new Exception("NYI - show menu"); // show a menu

            return base.ProcessCmdKey(ref msg, keyData);
        }


        #endregion
    }
}

