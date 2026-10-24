using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenDimmer
{
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(22, 23, 27);
        public static readonly Color Panel = Color.FromArgb(34, 36, 43);
        public static readonly Color Hover = Color.FromArgb(48, 50, 60);
        public static readonly Color Border = Color.FromArgb(62, 64, 74);
        public static readonly Color Text = Color.FromArgb(230, 230, 235);
        public static readonly Color Sub = Color.FromArgb(142, 144, 154);
        public static readonly Color Accent = Color.FromArgb(124, 156, 255);

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            float d = Math.Max(1f, Math.Min(rad * 2f, Math.Min(r.Width, r.Height)));
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    class DarkSlider : Control
    {
        int val;
        bool drag;
        public int Minimum = 0, Maximum = 100;
        public event EventHandler ValueChanged;

        public DarkSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
        }

        public int Value
        {
            get { return val; }
            set
            {
                int v = Math.Max(Minimum, Math.Min(Maximum, value));
                if (v == val) return;
                val = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        float Margin2 { get { return Height / 3.2f + 2f; } }

        void SetFromX(int x)
        {
            float x0 = Margin2, x1 = Width - Margin2;
            double t = (x - x0) / (x1 - x0);
            Value = Minimum + (int)Math.Round(t * (Maximum - Minimum));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Bg);
            float tr = Height / 3.2f, th = Height / 5f, m = Margin2, cy = Height / 2f;
            float x0 = m, x1 = Width - m;
            float frac = Maximum > Minimum ? (float)(val - Minimum) / (Maximum - Minimum) : 0f;
            float x = x0 + frac * (x1 - x0);
            using (var p = Theme.Round(new RectangleF(x0, cy - th / 2, x1 - x0, th), th / 2))
            using (var b = new SolidBrush(Color.FromArgb(52, 54, 64))) g.FillPath(b, p);
            if (x > x0 + 1)
                using (var p = Theme.Round(new RectangleF(x0, cy - th / 2, x - x0, th), th / 2))
                using (var b = new SolidBrush(Theme.Accent)) g.FillPath(b, p);
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, x - tr, cy - tr, tr * 2, tr * 2);
            if (Focused)
                using (var pen = new Pen(Theme.Accent, 2f)) g.DrawEllipse(pen, x - tr, cy - tr, tr * 2, tr * 2);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus(); drag = true; SetFromX(e.X);
        }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (drag) SetFromX(e.X); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); drag = false; }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); Value += Math.Sign(e.Delta); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override bool IsInputKey(Keys k)
        {
            return k == Keys.Left || k == Keys.Right || k == Keys.Up || k == Keys.Down || base.IsInputKey(k);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) Value -= 1;
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) Value += 1;
            else if (e.KeyCode == Keys.PageDown) Value -= 5;
            else if (e.KeyCode == Keys.PageUp) Value += 5;
        }
    }

    class DarkToggle : Control
    {
        bool chk;
        public event EventHandler CheckedChanged;

        public DarkToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            ForeColor = Theme.Text;
        }

        public bool Checked
        {
            get { return chk; }
            set
            {
                if (chk == value) return;
                chk = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Bg);
            float ph = Height * 0.72f, pw = ph * 1.8f, py = (Height - ph) / 2f;
            using (var p = Theme.Round(new RectangleF(0, py, pw, ph), ph / 2))
            using (var b = new SolidBrush(chk ? Theme.Accent : Color.FromArgb(62, 64, 74))) g.FillPath(b, p);
            float kd = ph - 6f;
            float kx = chk ? pw - kd - 3f : 3f;
            using (var b = new SolidBrush(chk ? Color.White : Color.FromArgb(200, 200, 208)))
                g.FillEllipse(b, kx, py + 3f, kd, kd);
            if (Focused)
                using (var p = Theme.Round(new RectangleF(-1, py - 1, pw + 2, ph + 2), ph / 2 + 1))
                using (var pen = new Pen(Color.FromArgb(150, Theme.Accent), 1.5f)) g.DrawPath(pen, p);
            int tx = (int)pw + 12;
            TextRenderer.DrawText(g, Text, Font, new Rectangle(tx, 0, Math.Max(1, Width - tx), Height), ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); Checked = !Checked; }
        protected override void OnKeyUp(KeyEventArgs e) { base.OnKeyUp(e); if (e.KeyCode == Keys.Space) Checked = !Checked; }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
    }

    class DarkColors : ProfessionalColorTable
    {
        public DarkColors() { UseSystemColors = false; }
        public override Color ToolStripDropDownBackground { get { return Theme.Panel; } }
        public override Color ImageMarginGradientBegin { get { return Theme.Panel; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.Panel; } }
        public override Color ImageMarginGradientEnd { get { return Theme.Panel; } }
        public override Color MenuBorder { get { return Theme.Border; } }
        public override Color MenuItemBorder { get { return Theme.Hover; } }
        public override Color MenuItemSelected { get { return Theme.Hover; } }
        public override Color MenuItemSelectedGradientBegin { get { return Theme.Hover; } }
        public override Color MenuItemSelectedGradientEnd { get { return Theme.Hover; } }
        public override Color MenuItemPressedGradientBegin { get { return Theme.Hover; } }
        public override Color MenuItemPressedGradientMiddle { get { return Theme.Hover; } }
        public override Color MenuItemPressedGradientEnd { get { return Theme.Hover; } }
        public override Color SeparatorDark { get { return Theme.Border; } }
        public override Color SeparatorLight { get { return Theme.Border; } }
    }

    class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkColors()) { RoundedEdges = false; }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Sub;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var g = e.Graphics;
            var r = e.ImageRectangle;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Theme.Accent, 2f))
                g.DrawLines(pen, new PointF[] {
                    new PointF(r.Left + 3, r.Top + r.Height / 2f),
                    new PointF(r.Left + r.Width / 2.5f, r.Bottom - 4),
                    new PointF(r.Right - 3, r.Top + 4) });
        }
    }

    // Small shared helpers for the secondary dialogs.
    static class UI
    {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

        public static void DarkTitleBar(IntPtr handle)
        {
            int on = 1;
            DwmSetWindowAttribute(handle, 20, ref on, 4); // Win10 20H1+/11
            DwmSetWindowAttribute(handle, 19, ref on, 4); // older builds
        }

        public static Button FlatButton(string text, int x, int y, int w, int h)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat, BackColor = Theme.Panel, ForeColor = Theme.Text,
                Cursor = Cursors.Hand, UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = Theme.Border;
            b.FlatAppearance.MouseOverBackColor = Theme.Hover;
            b.FlatAppearance.MouseDownBackColor = Theme.Border;
            return b;
        }

        public static Label Text(Control parent, string t, int x, int y, int w, int h, float size, Color c,
                                 FontStyle st = FontStyle.Regular)
        {
            var l = new Label
            {
                Text = t, Location = new Point(x, y), Size = new Size(w, h), AutoSize = false,
                ForeColor = c, BackColor = Color.Transparent, Font = new Font("Segoe UI", size, st)
            };
            parent.Controls.Add(l);
            return l;
        }
    }
}
