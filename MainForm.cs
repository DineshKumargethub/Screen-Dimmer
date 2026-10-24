using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenDimmer
{
    class MainForm : Form
    {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

        readonly DimmerContext ctx;
        readonly DarkSlider dimSlider, warmSlider;
        readonly Label lblPct, lblWarm, lblKeys;
        readonly DarkToggle tgOn, tgSched, tgStartup, tgHidden;
        readonly TextBox txtFrom, txtTo;
        bool syncing;

        public MainForm(DimmerContext c)
        {
            ctx = c;
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            Text = "Screen Dimmer";
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = DimmerContext.AppIcon();

            Lbl("Screen Dimmer", 24, 18, 312, 30, 16f, Theme.Text, FontStyle.Bold);
            Lbl("Dims your whole screen. It can never be brighter than your current Windows brightness.", 24, 50, 312, 38, 8.5f, Theme.Sub);

            Lbl("Dimming", 24, 104, 150, 24, 10f, Theme.Text);
            lblPct = Lbl("0%", 200, 98, 136, 32, 18f, Theme.Text, FontStyle.Bold, ContentAlignment.TopRight);
            dimSlider = new DarkSlider { Location = new Point(20, 138), Size = new Size(320, 30), Minimum = 0, Maximum = Settings.MaxLevel };
            Controls.Add(dimSlider);

            int[] presets = { 0, 25, 50, 75 };
            for (int i = 0; i < presets.Length; i++)
            {
                int v = presets[i];
                var b = new Button
                {
                    Text = v + "%", Location = new Point(24 + i * 80, 178), Size = new Size(70, 30),
                    FlatStyle = FlatStyle.Flat, BackColor = Theme.Panel, ForeColor = Theme.Text,
                    Cursor = Cursors.Hand, UseVisualStyleBackColor = false
                };
                b.FlatAppearance.BorderColor = Theme.Border;
                b.FlatAppearance.MouseOverBackColor = Theme.Hover;
                b.FlatAppearance.MouseDownBackColor = Theme.Border;
                b.Click += (s, e) => { ctx.S.Level = v; ctx.S.Enabled = true; ctx.Commit(); };
                Controls.Add(b);
            }

            Lbl("Warmth (less blue light)", 24, 234, 180, 24, 10f, Theme.Text);
            lblWarm = Lbl("0%", 200, 232, 136, 24, 11f, Theme.Sub, FontStyle.Regular, ContentAlignment.TopRight);
            warmSlider = new DarkSlider { Location = new Point(20, 262), Size = new Size(320, 30), Minimum = 0, Maximum = 100 };
            Controls.Add(warmSlider);

            var line = new Panel { Location = new Point(24, 312), Size = new Size(312, 1), BackColor = Theme.Border };
            Controls.Add(line);

            tgOn = Tog("Dimming on", 24, 328, 312);
            tgSched = Tog("Auto-dim schedule", 24, 366, 190);
            txtFrom = TimeBox(218, 367);
            Lbl("to", 266, 369, 18, 20, 9f, Theme.Sub, FontStyle.Regular, ContentAlignment.TopCenter);
            txtTo = TimeBox(286, 367);
            tgStartup = Tog("Start with Windows", 24, 404, 312);
            tgHidden = Tog("Start hidden in tray", 24, 442, 312);
            lblKeys = Lbl("", 24, 484, 312, 44, 8f, Theme.Sub);

            var btnKeys = UI.FlatButton("Customize hotkeys", 24, 538, 140, 30);
            btnKeys.Click += (s, e) => { using (var f = new HotkeyForm(ctx)) f.ShowDialog(this); };
            var btnAbout = UI.FlatButton("About", 172, 538, 76, 30);
            btnAbout.Click += (s, e) => ctx.ShowAbout();
            var btnDonate = UI.FlatButton("\u2665 Donate", 256, 538, 80, 30);
            btnDonate.ForeColor = Theme.Accent;
            btnDonate.Click += (s, e) => Links.Open(Links.Donate);
            Controls.Add(btnKeys);
            Controls.Add(btnAbout);
            Controls.Add(btnDonate);

            tgStartup.Checked = DimmerContext.IsStartupOn();

            dimSlider.ValueChanged += (s, e) => { if (syncing) return; ctx.S.Level = dimSlider.Value; ctx.S.Enabled = true; ctx.Commit(); };
            warmSlider.ValueChanged += (s, e) => { if (syncing) return; ctx.S.Warmth = warmSlider.Value; ctx.S.Enabled = true; ctx.Commit(); };
            tgOn.CheckedChanged += (s, e) => { if (syncing) return; ctx.S.Enabled = tgOn.Checked; ctx.Commit(); };
            tgSched.CheckedChanged += (s, e) => { if (syncing) return; ctx.S.Schedule = tgSched.Checked; ctx.ScheduleChanged(); };
            tgStartup.CheckedChanged += (s, e) => { if (syncing) return; DimmerContext.SetStartup(tgStartup.Checked); };
            tgHidden.CheckedChanged += (s, e) => { if (syncing) return; ctx.S.StartHidden = tgHidden.Checked; ctx.Commit(); };
            txtFrom.Leave += (s, e) => ReadTime(txtFrom, true);
            txtTo.Leave += (s, e) => ReadTime(txtTo, false);
            txtFrom.KeyDown += TimeKey;
            txtTo.KeyDown += TimeKey;

            ClientSize = new Size(360, 586);
            ResumeLayout(false);
            PerformLayout();
            SyncFromState();
        }

        Label Lbl(string t, int x, int y, int w, int h, float size, Color c,
                  FontStyle st = FontStyle.Regular, ContentAlignment al = ContentAlignment.TopLeft)
        {
            var l = new Label
            {
                Text = t, Location = new Point(x, y), Size = new Size(w, h), AutoSize = false,
                ForeColor = c, BackColor = Color.Transparent, TextAlign = al, Font = new Font("Segoe UI", size, st)
            };
            Controls.Add(l);
            return l;
        }

        DarkToggle Tog(string t, int x, int y, int w)
        {
            var g = new DarkToggle { Text = t, Location = new Point(x, y), Size = new Size(w, 28) };
            Controls.Add(g);
            return g;
        }

        TextBox TimeBox(int x, int y)
        {
            var t = new TextBox
            {
                Location = new Point(x, y), Width = 46, MaxLength = 5, TextAlign = HorizontalAlignment.Center,
                BackColor = Theme.Panel, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(t);
            return t;
        }

        void TimeKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            var t = (TextBox)sender;
            ReadTime(t, t == txtFrom);
        }

        void ReadTime(TextBox t, bool isFrom)
        {
            TimeSpan ts;
            if (TimeSpan.TryParse(t.Text.Trim(), out ts) && ts >= TimeSpan.Zero && ts < TimeSpan.FromDays(1))
            {
                if (isFrom) ctx.S.From = ts; else ctx.S.To = ts;
            }
            t.Text = (isFrom ? ctx.S.From : ctx.S.To).ToString(@"hh\:mm");
            ctx.ScheduleChanged();
        }

        public void SyncFromState()
        {
            syncing = true;
            var s = ctx.S;
            dimSlider.Value = s.Level;
            lblPct.Text = s.Level + "%";
            warmSlider.Value = s.Warmth;
            lblWarm.Text = s.Warmth + "%";
            tgOn.Checked = s.Enabled;
            tgSched.Checked = s.Schedule;
            tgHidden.Checked = s.StartHidden;
            txtFrom.Enabled = txtTo.Enabled = s.Schedule;
            if (!txtFrom.Focused) txtFrom.Text = s.From.ToString(@"hh\:mm");
            if (!txtTo.Focused) txtTo.Text = s.To.ToString(@"hh\:mm");
            string[] what = { "dimmer", "brighter", "on/off" };
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 3; i++)
                if (s.Hot[i] != null) parts.Add(s.Hot[i] + " " + what[i]);
            string keys = parts.Count == 0 ? "No hotkeys assigned." : string.Join("   \u2022   ", parts.ToArray());
            if (!ctx.HotkeysOk) keys += "\nSome hotkeys are already used by another app.";
            lblKeys.Text = keys;
            syncing = false;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int on = 1;
            DwmSetWindowAttribute(Handle, 20, ref on, 4); // dark title bar (Win10 20H1+/11)
            DwmSetWindowAttribute(Handle, 19, ref on, 4); // older builds
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!ctx.Exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                ctx.NotifyHidden();
            }
            base.OnFormClosing(e);
        }
    }
}
