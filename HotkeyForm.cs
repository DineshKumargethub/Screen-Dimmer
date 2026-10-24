using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenDimmer
{
    // Read-only box that records the key combination pressed while it has focus.
    class HotkeyBox : TextBox
    {
        Hotkey val;
        public event Action<string> Notice;

        public HotkeyBox()
        {
            ReadOnly = true;
            ShortcutsEnabled = false;
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = Theme.Panel;
            ForeColor = Theme.Text;
            Cursor = Cursors.Hand;
            TextAlign = HorizontalAlignment.Center;
            Display(null);
        }

        public Hotkey Value
        {
            get { return val; }
            set { val = value; Display(null); }
        }

        void Display(string text)
        {
            Text = text ?? (val == null ? "None" : val.ToString());
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Display("Press keys..."); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Display(null); }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode, mods = keyData & Keys.Modifiers;
            if (key == Keys.Tab || (mods == Keys.None && (key == Keys.Escape || key == Keys.Return)))
                return base.ProcessCmdKey(ref msg, keyData); // keep keyboard navigation working
            if (mods == Keys.None && (key == Keys.Back || key == Keys.Delete))
            {
                val = null; Display("Press keys...");
                if (Notice != null) Notice("");
                return true;
            }
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu || key == Keys.LWin || key == Keys.RWin)
                return true; // modifier pressed on its own: wait for the real key

            uint m = 0;
            if ((mods & Keys.Control) != 0) m |= Hotkey.MOD_CONTROL;
            if ((mods & Keys.Alt) != 0) m |= Hotkey.MOD_ALT;
            if ((mods & Keys.Shift) != 0) m |= Hotkey.MOD_SHIFT;
            if (m == 0)
            {
                if (Notice != null) Notice("Include at least one of Ctrl, Alt or Shift.");
                return true;
            }
            val = new Hotkey(m, key);
            Display("Press keys...");
            if (Notice != null) Notice("");
            return true;
        }
    }

    class HotkeyForm : Form
    {
        readonly DimmerContext ctx;
        readonly HotkeyBox[] boxes = new HotkeyBox[3];
        readonly Label msg;

        public HotkeyForm(DimmerContext c)
        {
            ctx = c;
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            Text = "Customize hotkeys";
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Icon = DimmerContext.AppIcon();

            UI.Text(this, "Customize hotkeys", 24, 18, 352, 28, 13f, Theme.Text, FontStyle.Bold);
            UI.Text(this, "Click a box, then press the new combination. Backspace clears it. " +
                          "Use at least one of Ctrl, Alt or Shift.", 24, 50, 352, 36, 8.5f, Theme.Sub);

            string[] names = { "Dim more", "Dim less (brighter)", "Dimming on / off" };
            for (int i = 0; i < 3; i++)
            {
                int y = 104 + i * 40;
                UI.Text(this, names[i], 24, y + 4, 160, 22, 9.5f, Theme.Text);
                var b = new HotkeyBox { Location = new Point(190, y), Size = new Size(186, 26), Value = ctx.S.Hot[i] };
                b.Notice += t => msg.Text = t;
                boxes[i] = b;
                Controls.Add(b);
            }

            msg = UI.Text(this, "", 24, 228, 352, 36, 8.5f, Color.FromArgb(255, 130, 130));

            var reset = UI.FlatButton("Reset defaults", 24, 276, 120, 30);
            reset.Click += (s, e) =>
            {
                var d = Hotkey.Defaults();
                for (int i = 0; i < 3; i++) boxes[i].Value = d[i];
                msg.Text = "";
            };
            var cancel = UI.FlatButton("Cancel", 208, 276, 80, 30);
            cancel.Click += (s, e) => Close();
            var save = UI.FlatButton("Save", 296, 276, 80, 30);
            save.ForeColor = Theme.Accent;
            save.Click += (s, e) => SaveClicked();
            Controls.Add(reset); Controls.Add(cancel); Controls.Add(save);
            CancelButton = cancel;

            ClientSize = new Size(400, 326);
            ResumeLayout(false);
            PerformLayout();
            ctx.PauseHotkeys(true);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            ctx.PauseHotkeys(false);
        }

        void SaveClicked()
        {
            var set = new Hotkey[3];
            for (int i = 0; i < 3; i++) set[i] = boxes[i].Value;
            for (int i = 0; i < 3; i++)
                for (int j = i + 1; j < 3; j++)
                    if (set[i] != null && set[i].Same(set[j]))
                    {
                        msg.Text = "Two actions cannot share the same hotkey.";
                        return;
                    }
            int bad = ctx.ApplyHotkeys(set);
            if (bad >= 0)
            {
                msg.Text = set[bad] + " is already used by another app. Try a different combination.";
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UI.DarkTitleBar(Handle);
        }
    }
}
