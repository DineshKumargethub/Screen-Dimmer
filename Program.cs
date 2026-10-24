using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using WinTimer = System.Windows.Forms.Timer;

namespace ScreenDimmer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool created;
            using (var mutex = new Mutex(true, "ScreenDimmer_SingleInstance", out created))
            using (var showEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "ScreenDimmer_ShowWindow"))
            {
                if (!created) { showEvt.Set(); return; } // already running: just bring its window up
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                bool startup = Array.IndexOf(args, "--startup") >= 0;
                Application.Run(new DimmerContext(startup, showEvt));
            }
        }
    }

    class HotkeyWindow : NativeWindow
    {
        public event Action<int> HotkeyPressed;
        public HotkeyWindow() { CreateHandle(new CreateParams()); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312 && HotkeyPressed != null) HotkeyPressed(m.WParam.ToInt32());
            base.WndProc(ref m);
        }
    }

    class DimmerContext : ApplicationContext
    {
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);

        const int Step = 5;
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string AppName = "ScreenDimmer";

        public readonly Settings S = new Settings();
        public bool Exiting { get; private set; }
        public bool[] HotkeyOk = { true, true, true };
        public bool HotkeysOk { get { return HotkeyOk[0] && HotkeyOk[1] && HotkeyOk[2]; } }

        readonly Dimmer dimmer;
        readonly NotifyIcon tray;
        readonly ToolStripMenuItem miEnabled;
        readonly MainForm form;
        readonly HotkeyWindow hotkeys;
        readonly WinTimer timer;
        readonly RegisteredWaitHandle wait;
        bool? lastInside;
        bool dirty, balloonShown, aboutOpen;

        public DimmerContext(bool startup, EventWaitHandle showEvt)
        {
            S.Load();
            dimmer = new Dimmer();

            var menu = new ContextMenuStrip
            {
                Renderer = new DarkRenderer(), BackColor = Theme.Panel, ForeColor = Theme.Text,
                ShowImageMargin = false, ShowCheckMargin = true, Font = new Font("Segoe UI", 9F)
            };
            var miOpen = new ToolStripMenuItem("Open Screen Dimmer") { Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            miOpen.Click += (s, e) => ShowWindow();
            miEnabled = new ToolStripMenuItem("Dimming on");
            miEnabled.Click += (s, e) => { S.Enabled = !S.Enabled; Commit(); };
            menu.Items.Add(miOpen);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miEnabled);
            foreach (int p in new[] { 25, 50, 75 })
            {
                int v = p;
                var mi = new ToolStripMenuItem("Dim " + v + "%");
                mi.Click += (s, e) => { S.Level = v; S.Enabled = true; Commit(); };
                menu.Items.Add(mi);
            }
            menu.Items.Add(new ToolStripSeparator());
            var miAbout = new ToolStripMenuItem("About / Donate");
            miAbout.Click += (s, e) => ShowAbout();
            menu.Items.Add(miAbout);
            var miExit = new ToolStripMenuItem("Exit");
            miExit.Click += (s, e) => ExitApp();
            menu.Items.Add(miExit);

            tray = new NotifyIcon { Icon = AppIcon(), Text = "Screen Dimmer", ContextMenuStrip = menu, Visible = true };
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowWindow(); };

            form = new MainForm(this);
            var handle = form.Handle; // create the handle so other threads can BeginInvoke onto it
            wait = ThreadPool.RegisterWaitForSingleObject(showEvt,
                (state, timedOut) => { try { form.BeginInvoke(new Action(ShowWindow)); } catch { } },
                null, -1, false);

            // Defaults: Ctrl+Alt+D dimmer, Ctrl+Alt+W brighter, Ctrl+Alt+T on/off (customizable)
            hotkeys = new HotkeyWindow();
            hotkeys.HotkeyPressed += OnHotkey;
            HotkeyOk = TryRegister(S.Hot);

            timer = new WinTimer { Interval = 1500 };
            timer.Tick += (s, e) =>
            {
                CheckSchedule();
                dimmer.Reapply();
                if (dirty) { dirty = false; S.Save(); }
            };
            timer.Start();

            CheckSchedule();
            Commit();
            if (!(startup && S.StartHidden)) ShowWindow();
        }

        public void Commit()
        {
            bool active = S.Enabled && (S.Level > 0 || S.Warmth > 0);
            dimmer.Apply(active ? S.Level : 0, active ? S.Warmth : 0);
            miEnabled.Checked = S.Enabled;
            tray.Text = S.Enabled ? "Screen Dimmer - " + S.Level + "%" : "Screen Dimmer - off";
            form.SyncFromState();
            dirty = true;
        }

        public void ScheduleChanged()
        {
            lastInside = null;
            CheckSchedule();
            Commit();
        }

        // Edge-triggered: only flips Enabled when the schedule window is entered or left,
        // so manual toggles stay in effect until the next transition.
        void CheckSchedule()
        {
            if (!S.Schedule) return;
            bool inside = S.InWindow(DateTime.Now.TimeOfDay);
            if (lastInside != inside)
            {
                lastInside = inside;
                if (S.Enabled != inside) { S.Enabled = inside; Commit(); }
            }
        }

        bool[] TryRegister(Hotkey[] set)
        {
            var ok = new bool[3];
            for (int i = 0; i < 3; i++)
            {
                UnregisterHotKey(hotkeys.Handle, i + 1);
                ok[i] = set[i] == null || RegisterHotKey(hotkeys.Handle, i + 1, set[i].Mod, (uint)set[i].Key);
            }
            return ok;
        }

        // Returns -1 on success, otherwise the index of the first hotkey another app already owns
        // (in that case the previous hotkeys are restored).
        public int ApplyHotkeys(Hotkey[] set)
        {
            var ok = TryRegister(set);
            int bad = Array.IndexOf(ok, false);
            if (bad >= 0) { HotkeyOk = TryRegister(S.Hot); return bad; }
            S.Hot = (Hotkey[])set.Clone();
            HotkeyOk = ok;
            dirty = true;
            form.SyncFromState();
            return -1;
        }

        // While the hotkey dialog is open the global hotkeys are released so the dialog can capture them.
        public void PauseHotkeys(bool pause)
        {
            if (pause) { for (int i = 1; i <= 3; i++) UnregisterHotKey(hotkeys.Handle, i); }
            else { HotkeyOk = TryRegister(S.Hot); form.SyncFromState(); }
        }

        public void ShowAbout()
        {
            if (aboutOpen) return;
            aboutOpen = true;
            try { using (var f = new AboutForm()) f.ShowDialog(form.Visible ? form : null); }
            finally { aboutOpen = false; }
        }

        void OnHotkey(int id)
        {
            if (id == 1) { S.Level = Math.Min(Settings.MaxLevel, S.Level + Step); S.Enabled = true; }
            else if (id == 2) { S.Level = Math.Max(0, S.Level - Step); S.Enabled = true; }
            else S.Enabled = !S.Enabled;
            Commit();
        }

        public void ShowWindow()
        {
            if (!form.Visible) form.Show();
            if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
            form.TopMost = true; form.TopMost = false; // reliably bring to front
            form.Activate();
        }

        public void NotifyHidden()
        {
            if (balloonShown) return;
            balloonShown = true;
            tray.ShowBalloonTip(1500, "Screen Dimmer", "Still running in the tray. Right-click the icon to exit.", ToolTipIcon.Info);
        }

        void ExitApp()
        {
            Exiting = true;
            S.Save();
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            timer.Stop();
            wait.Unregister(null);
            for (int i = 1; i <= 3; i++) UnregisterHotKey(hotkeys.Handle, i);
            dimmer.Dispose();
            tray.Visible = false;
            tray.Dispose();
            form.Close();
            base.ExitThreadCore();
        }

        public static bool IsStartupOn()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue(AppName) != null;
        }

        public static void SetStartup(bool on)
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (k == null) return;
                if (on) k.SetValue(AppName, "\"" + Environment.ProcessPath + "\" --startup");
                else k.DeleteValue(AppName, false);
            }
        }

        public static Icon AppIcon()
        {
            try { var ic = Icon.ExtractAssociatedIcon(Environment.ProcessPath); if (ic != null) return ic; } catch { }
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.FillEllipse(Brushes.White, 3, 3, 26, 26);
                g.FillPie(Brushes.Black, 3, 3, 26, 26, 90, 180);
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
    }
}
