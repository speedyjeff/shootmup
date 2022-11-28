using engine.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public static class Initialize
    {
        public static void LoadResources(Action<string, byte[]> preloadSound)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            foreach (var kvp in engine.Common.Embedded.LoadResource(assembly))
            {
                var parts = kvp.Key.Split('.');
                var name = parts.Length < 2 ? kvp.Key : parts[parts.Length - 2];

                if (kvp.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    // preload image (now loadable by name)
                    var bytes = new byte[kvp.Value.Length];
                    kvp.Value.Read(bytes, 0, bytes.Length);
                    var img = new ImageSource(name, bytes);
                }
                else if (kvp.Key.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    // preload sounds (now loadable by name)
                    var bytes = new byte[kvp.Value.Length];
                    kvp.Value.Read(bytes, 0, bytes.Length);
                    preloadSound(name, bytes);
                }
            }
        }
    }
}
