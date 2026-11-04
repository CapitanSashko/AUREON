using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MP3
{
    /// <summary>Компактная круглая крутилка для панели AUREON EQ.</summary>
    public sealed class RotaryKnobControl : Control
    {
        private int minimum;
        private int maximum = 100;
        private int value;
        private int startValue;
        private int startY;
        private bool dragging;

        public event EventHandler ValueChanged;
        public event EventHandler ValueChangeCompleted;

        public RotaryKnobControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        [DefaultValue(0)]
        public int Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                if (maximum < minimum) maximum = minimum;
                SetValue(this.value, false);
                Invalidate();
            }
        }

        [DefaultValue(100)]
        public int Maximum
        {
            get => maximum;
            set
            {
                maximum = Math.Max(minimum, value);
                SetValue(this.value, false);
                Invalidate();
            }
        }

        [DefaultValue(0)]
        public int Value
        {
            get => value;
            set => SetValue(value, true);
        }

        [DefaultValue("")]
        public string Caption { get; set; } = "";

        [DefaultValue("")]
        public string Unit { get; set; } = "";

        public void SetValue(int newValue, bool notify)
        {
            int clamped = Math.Clamp(newValue, minimum, maximum);
            if (clamped == value) return;

            value = clamped;
            Invalidate();

            if (notify)
                ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled || e.Button != MouseButtons.Left) return;

            dragging = true;
            startY = e.Y;
            startValue = value;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;

            int range = Math.Max(1, maximum - minimum);
            int sensitivity = Math.Max(48, Height - 18);
            int delta = (int)Math.Round((startY - e.Y) * range / (double)sensitivity);
            Value = startValue + delta;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!dragging) return;

            dragging = false;
            Capture = false;
            ValueChangeCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (dragging && !Capture)
            {
                dragging = false;
                ValueChangeCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!Enabled || e.Delta == 0) return;

            int step = e.Delta > 0 ? 1 : -1;
            Value += step;
            ValueChangeCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent?.BackColor ?? Theme.Card);

            float scale = DeviceDpi / 96f;
            float width = Width;
            float centerX = width / 2f;
            float centerY = Math.Min(39f * scale, Height * 0.58f);
            float radius = Math.Min(16f * scale, Math.Max(9f, (Height - 25f * scale) / 2f));

            Color textColor = Enabled ? Theme.Text : Theme.TextDim;
            Color dim = Theme.TextDim;
            using (var captionFont = new Font("Segoe UI Semibold", 7f * scale, FontStyle.Bold))
            using (var valueFont = new Font("Segoe UI", 7.2f * scale, FontStyle.Regular))
            using (var captionBrush = new SolidBrush(textColor))
            using (var valueBrush = new SolidBrush(Enabled ? Theme.Accent2 : dim))
            using (var centerBrush = new SolidBrush(Theme.Surface))
            using (var ringPen = new Pen(Theme.Border, 1.2f * scale))
            using (var trackPen = new Pen(Theme.Track, 4f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var activePen = new Pen(Enabled ? Theme.Accent1 : dim, 4f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var pointerPen = new Pen(Enabled ? Color.White : dim, 2f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var centerText = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(Caption ?? "", captionFont, captionBrush,

                var arc = new RectangleF(centerX - radius, centerY - radius,
                    radius * 2f, radius * 2f);

                g.DrawArc(trackPen, arc, 135f, 270f);

                float fraction = maximum > minimum
                    ? (value - minimum) / (float)(maximum - minimum)
                    : 0f;

                if (fraction > 0.001f)
                    g.DrawArc(activePen, arc, 135f, 270f * fraction);

                g.FillEllipse(centerBrush, centerX - radius * 0.63f,
                    centerY - radius * 0.63f, radius * 1.26f, radius * 1.26f);
                g.DrawEllipse(ringPen, centerX - radius * 0.63f,
                    centerY - radius * 0.63f, radius * 1.26f, radius * 1.26f);

                double angle = (135.0 + 270.0 * fraction) * Math.PI / 180.0;
                float inner = radius * 0.38f;
                float outer = radius * 0.92f;
                g.DrawLine(pointerPen,
                    centerX + (float)Math.Cos(angle) * inner,
                    centerY + (float)Math.Sin(angle) * inner,
                    centerX + (float)Math.Cos(angle) * outer,
                    centerY + (float)Math.Sin(angle) * outer);

                string display = value.ToString() + (Unit ?? "");
                g.DrawString(display, valueFont, valueBrush,
                    new RectangleF(0, Height - 15f * scale, width, 14f * scale), centerText);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }
    }
}
