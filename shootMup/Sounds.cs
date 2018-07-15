using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

using shootMup.Common;

namespace shootMup
{
    public class Sounds : ISounds
    {
        public void Play(string path)
        {
            SoundPlayer player = null;
            if (!All.TryGetValue(path, out player))
            {
                player = new SoundPlayer();
                player.SoundLocation = path;
                All.Add(path, player);
            }
            player.Play();
        }

        #region private
        private static Dictionary<string, SoundPlayer> All = new Dictionary<string, SoundPlayer>();
        #endregion
    }
}
