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

        public const char Space = (char)248;

        public const char W = 'W';
        public const char w = 'w';
        public const char S = 'S';
        public const char s = 's';
        public const char A = 'A';
        public const char a = 'a';
        public const char D = 'D';
        public const char d = 'd';

        public const char F = 'F';
        public const char f = 'f';

        public const char R = 'R';
        public const char r = 'r';

        public const char x1 = '1';

        public const char x0 = '0';
        public const char x2 = '2';
        public const char Q = 'Q';
        public const char q = 'q';

        // mouse
        public const char LeftMouse = (char)250;
        public const char RightMouse = (char)249;

        // player options
        public static int Speed = 10;
        public const int MaxSheld = 100;
        public const int MaxHealth = 100;
        public const int EphemerialElementDuration = 10;
    }
}
