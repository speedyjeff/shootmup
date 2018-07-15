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

        public const char Up = 'w';
        public const char Down = 's';
        public const char Left = 'a';
        public const char Right = 'd';

        public const char Pickup = 'f';

        public const char Reload = 'r';

        public const char Switch = '1';

        // mouse
        public const char LeftMouse = (char)250;
        public const char RightMouse = (char)249;

        // player options
        public static int Speed = 10;
        public const int MaxSheld = 100;
        public const int MaxHealth = 100;
        public const int BulletDuration = 10;
    }
}
