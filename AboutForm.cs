using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ScreenDimmer
{
    static class Links
    {
        public const string GitHub = "https://github.com/DineshKumargethub";
        public const string Donate = "https://buymeacoffee.com/dineshkumarg";
        public const string AboutText = "Created and maintained by Dineshkumar G. I am a passionate developer who enjoys " +
                                        "building practical software to solve everyday problems and improve usability.";

        public static void Open(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch
            {
                try { Clipboard.SetText(url); } catch { }
                MessageBox.Show("Could not open the browser. The link was copied to the clipboard:\n" + url,
                    "Screen Dimmer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    class AboutForm : Form
    {
        public AboutForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            Text = "About Screen Dimmer";
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Icon = DimmerContext.AppIcon();

            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            string ver = v == null ? "" : "Version " + v.ToString(3);

            UI.Text(this, "Screen Dimmer", 24, 20, 332, 32, 16f, Theme.Text, FontStyle.Bold);
            UI.Text(this, ver, 24, 54, 332, 20, 9f, Theme.Sub);

            UI.Text(this, "About", 24, 92, 332, 22, 10f, Theme.Text, FontStyle.Bold);
            UI.Text(this, Links.AboutText, 24, 116, 332, 64, 9.5f, Theme.Text);

            var gh = UI.FlatButton("GitHub", 24, 196, 100, 32);
            gh.Click += (s, e) => Links.Open(Links.GitHub);
            var don = UI.FlatButton("\u2665  Donate - Buy me a coffee", 132, 196, 224, 32);
            don.ForeColor = Theme.Accent;
            don.Click += (s, e) => Links.Open(Links.Donate);
            Controls.Add(gh);
            Controls.Add(don);

            UI.Text(this, "Donations are voluntary and are not required to use the software.", 24, 240, 332, 34, 8.5f, Theme.Sub);
            UI.Text(this, "MIT License \u00A9 2026 Dineshkumar G.", 24, 282, 232, 20, 8.5f, Theme.Sub);

            var close = UI.FlatButton("Close", 276, 278, 80, 30);
            close.Click += (s, e) => Close();
            Controls.Add(close);
            CancelButton = close;

            ClientSize = new Size(380, 326);
            ResumeLayout(false);
            PerformLayout();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UI.DarkTitleBar(Handle);
        }
    }
}
