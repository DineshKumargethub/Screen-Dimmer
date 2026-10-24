using System;
using System.Collections.Generic;
using System.IO;

namespace ScreenDimmer
{
    class Settings
    {
        public const int MaxLevel = 95;

        public int Level = 50;
        public int Warmth = 0;
        public bool Enabled = true;
        public bool StartHidden = false;
        public bool Schedule = false;
        public TimeSpan From = new TimeSpan(22, 0, 0);
        public TimeSpan To = new TimeSpan(6, 0, 0);
        public Hotkey[] Hot = Hotkey.Defaults(); // [0] dim more, [1] dim less, [2] on/off; null = unassigned

        static readonly string[] HotNames = { "hk_more", "hk_less", "hk_toggle" };

        static readonly string FilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenDimmer", "settings.txt");

        public bool InWindow(TimeSpan now)
        {
            if (From == To) return false;
            if (From < To) return now >= From && now < To;
            return now >= From || now < To; // wraps past midnight
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i < 0) continue;
                    string k = line.Substring(0, i).Trim(), v = line.Substring(i + 1).Trim();
                    int n; bool b; TimeSpan t;
                    switch (k)
                    {
                        case "level": if (int.TryParse(v, out n)) Level = Math.Max(0, Math.Min(MaxLevel, n)); break;
                        case "warmth": if (int.TryParse(v, out n)) Warmth = Math.Max(0, Math.Min(100, n)); break;
                        case "enabled": if (bool.TryParse(v, out b)) Enabled = b; break;
                        case "hidden": if (bool.TryParse(v, out b)) StartHidden = b; break;
                        case "schedule": if (bool.TryParse(v, out b)) Schedule = b; break;
                        case "from": if (TimeSpan.TryParse(v, out t) && t >= TimeSpan.Zero && t < TimeSpan.FromDays(1)) From = t; break;
                        case "to": if (TimeSpan.TryParse(v, out t) && t >= TimeSpan.Zero && t < TimeSpan.FromDays(1)) To = t; break;
                        default:
                            int hi = Array.IndexOf(HotNames, k);
                            if (hi >= 0)
                            {
                                if (v.Equals("none", StringComparison.OrdinalIgnoreCase)) Hot[hi] = null;
                                else { var hk = Hotkey.Parse(v); if (hk != null) Hot[hi] = hk; }
                            }
                            break;
                    }
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
                var lines = new List<string> {
                    "level=" + Level, "warmth=" + Warmth, "enabled=" + Enabled, "hidden=" + StartHidden,
                    "schedule=" + Schedule, "from=" + From.ToString(@"hh\:mm"), "to=" + To.ToString(@"hh\:mm") };
                for (int i = 0; i < HotNames.Length; i++)
                    lines.Add(HotNames[i] + "=" + (Hot[i] == null ? "none" : Hot[i].Serialize()));
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch { }
        }
    }
}
