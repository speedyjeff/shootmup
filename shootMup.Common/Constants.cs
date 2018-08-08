using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public static class Constants
    {
        // arrow keys
        public const char UpArrow = (char)254;
        public const char DownArrow = (char)253;
        public const char LeftArrow = (char)252;
        public const char RightArrow = (char)251;

        // keyboard
        public const char Space = (char)248;
        public const char Esc = (char)247;

        public const char Up = 'W';
        public const char Up2 = 'w';
        public const char Down = 'S';
        public const char Down2 = 's';
        public const char Left = 'A';
        public const char Left2 = 'a';
        public const char Right = 'D';
        public const char Right2 = 'd';

        public const char Pickup = 'F';
        public const char Pickup2 = 'f';

        public const char Reload = 'R';
        public const char MiddleMouse = 'r';

        public const char Switch = '1';

        public const char Drop = '0';
        public const char Drop2 = '2';
        public const char Drop3 = 'Q';
        public const char Drop4 = 'q';

        // mouse
        public const char LeftMouse = (char)250;
        public const char RightMouse = (char)249;

        // player options
        public const int Speed = 10;
        public const float MaxSpeedMultiplier = 4;
        public const float MinSpeedMultiplier = 0.1f;
        public const int MaxSheld = 100;
        public const int MaxHealth = 100;
        public const int GlobalClock = 100; // ms
        public const bool CaptureAITrainingData = true;

        // world options
        public const float MaxZoomIn = 10f;
        public const float ZoomStep = 0.1f;
        public const float Ground = 0f;
        public const float Sky = 1f;

        // diagnstics
        public const bool Debug_ShowHitBoxes = false;
        public const bool Debug_AIMoveDiag = false;
    }
}
