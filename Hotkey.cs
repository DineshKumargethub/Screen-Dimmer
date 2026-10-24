using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ScreenDimmer
{
    // A global hotkey: Ctrl/Alt/Shift modifiers + one key.
    class Hotkey
    {
        public const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4;

        public readonly uint Mod;
        public readonly Keys Key;

        public Hotkey(uint mod, Keys key) { Mod = mod; Key = key; }

        public bool Same(Hotkey o) { return o != null && o.Mod == Mod && o.Key == Key; }

        // Shown to the user, e.g. "Ctrl+Alt+D".
        public override string ToString() { return Build(KeyName(Key)); }

        // Stored in settings.txt using the Keys enum name, e.g. "Ctrl+Alt+D".
        public string Serialize() { return Build(Key.ToString()); }

        string Build(string key)
        {
            var parts = new List<string>();
            if ((Mod & MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((Mod & MOD_ALT) != 0) parts.Add("Alt");
            if ((Mod & MOD_SHIFT) != 0) parts.Add("Shift");
            parts.Add(key);
            return string.Join("+", parts.ToArray());
        }

        public static Hotkey Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            string[] tok = s.Split('+');
            uint mod = 0;
            for (int i = 0; i < tok.Length - 1; i++)
            {
                switch (tok[i].Trim().ToLowerInvariant())
                {
                    case "ctrl": mod |= MOD_CONTROL; break;
                    case "alt": mod |= MOD_ALT; break;
                    case "shift": mod |= MOD_SHIFT; break;
                    default: return null;
                }
            }
            Keys k;
            if (mod == 0 || !Enum.TryParse(tok[tok.Length - 1].Trim(), true, out k) || !Enum.IsDefined(typeof(Keys), k)) return null;
            if (k == Keys.None || k == Keys.ControlKey || k == Keys.ShiftKey || k == Keys.Menu ||
                k == Keys.LWin || k == Keys.RWin) return null;
            return new Hotkey(mod, k);
        }

        public static string KeyName(Keys k)
        {
            if (k >= Keys.D0 && k <= Keys.D9) return ((char)('0' + (k - Keys.D0))).ToString();
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return "Num" + (k - Keys.NumPad0);
            switch (k)
            {
                case Keys.Oemplus: return "=";
                case Keys.OemMinus: return "-";
                case Keys.Oemcomma: return ",";
                case Keys.OemPeriod: return ".";
                case Keys.OemQuestion: return "/";
                case Keys.Oem1: return ";";
                case Keys.Oem3: return "`";
                case Keys.OemOpenBrackets: return "[";
                case Keys.Oem6: return "]";
                case Keys.Oem5: return "\\";
                case Keys.Oem7: return "'";
                case Keys.Prior: return "PageUp";
                case Keys.Next: return "PageDown";
                case Keys.Return: return "Enter";
                case Keys.Back: return "Backspace";
                case Keys.Capital: return "CapsLock";
                default: return k.ToString();
            }
        }

        // Order matches the hotkey ids: 1 = dim more, 2 = dim less, 3 = on/off.
        public static Hotkey[] Defaults()
        {
            return new[] {
                new Hotkey(MOD_CONTROL | MOD_ALT, Keys.D),
                new Hotkey(MOD_CONTROL | MOD_ALT, Keys.W),
                new Hotkey(MOD_CONTROL | MOD_ALT, Keys.T) };
        }
    }
}
