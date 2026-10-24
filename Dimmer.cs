using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenDimmer
{
    // Fallback only: click-through, never-activating black overlay.
    class OverlayForm : Form
    {
        const int WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_LAYERED = 0x80000,
                  WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOPMOST = 0x8;

        public OverlayForm(Rectangle bounds)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            TopMost = true;
            Bounds = bounds;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEACTIVATE = 0x21, MA_NOACTIVATE = 3;
            if (m.Msg == WM_MOUSEACTIVATE) { m.Result = (IntPtr)MA_NOACTIVATE; return; }
            base.WndProc(ref m);
        }
    }

    // Dimming engine. Uses the Windows Magnification API (compositor-level color effect):
    // no overlay window, no gamma ramp. Falls back to an overlay if the API is unavailable.
    class Dimmer : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MAGCOLOREFFECT { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)] public float[] transform; }
        [DllImport("Magnification.dll")] static extern bool MagInitialize();
        [DllImport("Magnification.dll")] static extern bool MagUninitialize();
        [DllImport("Magnification.dll")] static extern bool MagSetFullscreenColorEffect(ref MAGCOLOREFFECT effect);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 1, SWP_NOMOVE = 2, SWP_NOACTIVATE = 0x10;

        readonly bool mag;
        readonly List<OverlayForm> overlays = new List<OverlayForm>();
        readonly Control sync;
        int level, warm;

        public bool UsingFallback { get { return !mag; } }

        public Dimmer()
        {
            mag = MagInitialize();
            if (!mag)
            {
                sync = new Control();
                var h = sync.Handle;
                BuildOverlays();
                SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
            }
        }

        static void SetEffect(float r, float g, float b)
        {
            var e = new MAGCOLOREFFECT { transform = new float[] {
                r, 0, 0, 0, 0,
                0, g, 0, 0, 0,
                0, 0, b, 0, 0,
                0, 0, 0, 1, 0,
                0, 0, 0, 0, 1 } };
            MagSetFullscreenColorEffect(ref e);
        }

        public void Apply(int dimPercent, int warmPercent)
        {
            level = dimPercent; warm = warmPercent;
            if (mag)
            {
                float f = 1f - level / 100f, k = warm / 100f;
                SetEffect(f, f * (1f - 0.25f * k), f * (1f - 0.6f * k));
            }
            else
            {
                foreach (var o in overlays)
                {
                    o.Opacity = level / 100.0;
                    if (level > 0) { if (!o.Visible) o.Show(); Top(o); }
                    else if (o.Visible) o.Hide();
                }
            }
        }

        // Called periodically: re-assert the effect in case something reset it.
        public void Reapply()
        {
            if (level <= 0 && warm <= 0) return;
            if (mag) Apply(level, warm);
            else foreach (var o in overlays) if (o.Visible) Top(o);
        }

        static void Top(OverlayForm o)
        {
            SetWindowPos(o.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        void BuildOverlays()
        {
            foreach (var o in overlays) { o.Close(); o.Dispose(); }
            overlays.Clear();
            foreach (var scr in Screen.AllScreens) overlays.Add(new OverlayForm(scr.Bounds));
        }

        void OnDisplayChanged(object sender, EventArgs e)
        {
            sync.BeginInvoke(new Action(() => { BuildOverlays(); Apply(level, warm); }));
        }

        public void Dispose()
        {
            if (mag) { SetEffect(1f, 1f, 1f); MagUninitialize(); }
            else
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
                foreach (var o in overlays) o.Close();
            }
        }
    }
}
