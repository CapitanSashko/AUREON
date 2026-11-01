using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MP3
{
    // =====================================================================
    //  Тема
    // =====================================================================
    public static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(22, 22, 26);
        public static readonly Color Card = Color.FromArgb(30, 30, 38);
        public static readonly Color Surface = Color.FromArgb(44, 44, 58);
        public static readonly Color SurfaceHover = Color.FromArgb(58, 58, 76);
        public static readonly Color SurfacePressed = Color.FromArgb(34, 34, 46);
        public static readonly Color Border = Color.FromArgb(50, 50, 66);
        public static readonly Color Track = Color.FromArgb(54, 54, 70);

        public static readonly Color Accent1 = Color.FromArgb(124, 92, 255);  // фиолетовый
        public static readonly Color Accent2 = Color.FromArgb(63, 169, 255);  // голубой

        public static readonly Color Text = Color.FromArgb(237, 237, 245);
        public static readonly Color TextDim = Color.FromArgb(142, 142, 163);

        public static float Scale(Control c) => c.DeviceDpi / 96f;

        public static Color Alpha(Color c, int a) => Color.FromArgb(a, c.R, c.G, c.B);

        public static Color Mix(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d <= 0.5f)
            {
                p.AddRectangle(r);
                return p;
            }

            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // =====================================================================
    //  Кнопка
    // =====================================================================
    public enum ButtonKind
    {
        Ghost,   // прозрачная, подсвечивается при наведении
        Soft,    // спокойная заливка с рамкой
        Accent   // градиент
    }

    public class RoundButton : Control
    {
        private ButtonKind kind = ButtonKind.Ghost;
        private int radius = 10;
        private bool hover;
        private bool down;
        private bool showDropArrow;

        public RoundButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.StandardClick |
                     ControlStyles.StandardDoubleClick, true);

            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(43, 31);
        }

        [DefaultValue(ButtonKind.Ghost)]
        public ButtonKind Kind
        {
            get => kind;
            set { kind = value; Invalidate(); }
        }

        [DefaultValue(10)]
        public int Radius
        {
            get => radius;
            set { radius = Math.Max(0, value); Invalidate(); }
        }

        [DefaultValue(false)]
        public bool ShowDropArrow
        {
            get => showDropArrow;
            set { showDropArrow = value; Invalidate(); }
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); Invalidate(); }
        protected override void OnForeColorChanged(EventArgs e) { base.OnForeColorChanged(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { down = true; Invalidate(); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            down = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 2 || Height < 2) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            float k = Theme.Scale(this);
            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);

            using (var path = Theme.Rounded(rect, radius * k))
            {
                switch (kind)
                {
                    case ButtonKind.Accent:
                        {
                            Color c1 = Theme.Accent1, c2 = Theme.Accent2;

                            if (!Enabled)
                            {
                                c1 = Theme.Mix(c1, Theme.Surface, 0.65f);
                                c2 = Theme.Mix(c2, Theme.Surface, 0.65f);
                            }
                            else if (down)
                            {
                                c1 = Theme.Mix(c1, Color.Black, 0.22f);
                                c2 = Theme.Mix(c2, Color.Black, 0.22f);
                            }
                            else if (hover)
                            {
                                c1 = Theme.Mix(c1, Color.White, 0.16f);
                                c2 = Theme.Mix(c2, Color.White, 0.16f);
                            }

                            using var br = new LinearGradientBrush(
                                new Rectangle(0, 0, Width, Height), c1, c2, 20f);
                            g.FillPath(br, path);
                            break;
                        }

                    case ButtonKind.Soft:
                        {
                            Color fill = down ? Theme.SurfacePressed
                                       : hover ? Theme.SurfaceHover
                                       : Theme.Surface;

                            using var br = new SolidBrush(fill);
                            g.FillPath(br, path);

                            using var pen = new Pen(hover && Enabled
                                ? Theme.Alpha(Theme.Accent2, 150)
                                : Theme.Border);
                            g.DrawPath(pen, path);
                            break;
                        }

                    default: // Ghost
                        {
                            if (Enabled && (hover || down))
                            {
                                using var br = new SolidBrush(Color.FromArgb(down ? 30 : 17, 255, 255, 255));
                                g.FillPath(br, path);
                            }
                            break;
                        }
                }
            }

            // Стрелка выпадающего списка справа
            if (showDropArrow)
            {
                float cx = Width - 14f * k, cy = Height / 2f, s = 3.5f * k;
                using var arrowPen = new Pen(Enabled ? Theme.TextDim : Theme.Border, 1.6f * k)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                g.DrawLines(arrowPen, new[]
                {
                new PointF(cx - s, cy - s / 2f),
                new PointF(cx, cy + s / 2f),
                new PointF(cx + s, cy - s / 2f)
            });
            }

            if (string.IsNullOrEmpty(Text)) return;

            using var sf = new StringFormat(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            sf.FormatFlags |= StringFormatFlags.NoWrap;

            Color fore = Enabled ? ForeColor : Theme.TextDim;
            using var tb = new SolidBrush(fore);
            g.DrawString(Text, Font, tb, new RectangleF(0, down ? 1 : 0, Width, Height), sf);
        }
    }
    // =====================================================================
    //  Ползунок
    // =====================================================================
    public class ModernSlider : Control
    {
        private int min = 0, max = 100, val = 0;
        private bool hover, dragging;

        // Как у TrackBar: Scroll вызывается только когда значение меняет пользователь
        public event EventHandler Scroll;
        public event EventHandler ValueChanged;

        public ModernSlider()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(120, 21);
        }

        [DefaultValue(0)]
        public int Minimum
        {
            get => min;
            set
            {
                min = value;
                if (max < min) max = min;
                val = Math.Clamp(val, min, max);
                Invalidate();
            }
        }

        [DefaultValue(100)]
        public int Maximum
        {
            get => max;
            set
            {
                max = value;
                if (min > max) min = max;
                val = Math.Clamp(val, min, max);
                Invalidate();
            }
        }

        [DefaultValue(0)]
        public int Value
        {
            get => val;
            set
            {
                int v = Math.Clamp(value, min, max);
                if (v == val) return;
                val = v;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private float Pad => 9f * Theme.Scale(this);

        private void SetFromX(float x)
        {
            float x0 = Pad, x1 = Width - Pad;
            float frac = x1 > x0 ? Math.Clamp((x - x0) / (x1 - x0), 0f, 1f) : 0f;
            int v = (int)Math.Round(min + frac * (max - min));

            if (v == val) return;

            val = v;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
            Scroll?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                SetFromX(e.X);   // Value уже правильный, когда форма получит MouseDown
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging) SetFromX(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (dragging)
            {
                SetFromX(e.X);
                dragging = false;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 4 || Height < 4) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float k = Theme.Scale(this);
            float pad = Pad;
            float x0 = pad, x1 = Width - pad;
            float cy = Height / 2f;
            float trackH = 4f * k;

            float frac = max > min ? (float)(val - min) / (max - min) : 0f;
            float tx = x0 + (x1 - x0) * frac;

            // Дорожка
            var trackRect = new RectangleF(x0, cy - trackH / 2f, x1 - x0, trackH);
            using (var path = Theme.Rounded(trackRect, trackH / 2f))
            using (var br = new SolidBrush(Theme.Track))
                g.FillPath(br, path);

            // Заполненная часть с градиентом
            if (tx - x0 >= 1f)
            {
                var fillRect = new RectangleF(x0, cy - trackH / 2f, tx - x0, trackH);
                using var path = Theme.Rounded(fillRect, trackH / 2f);
                using var br = new LinearGradientBrush(fillRect, Theme.Accent1, Theme.Accent2, 0f);
                g.FillPath(br, path);
            }

            // Ползунок
            bool active = hover || dragging;
            float r = (active ? 8f : 7f) * k;

            if (active)
            {
                float gr = r + 4f * k;
                using var glow = new SolidBrush(Theme.Alpha(Theme.Accent2, dragging ? 70 : 45));
                g.FillEllipse(glow, tx - gr, cy - gr, gr * 2, gr * 2);
            }

            using (var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.FillEllipse(shadow, tx - r, cy - r + 1f * k, r * 2, r * 2);

            using (var thumb = new SolidBrush(Color.White))
                g.FillEllipse(thumb, tx - r, cy - r, r * 2, r * 2);
        }
    }

    // =====================================================================
    //  Карточка (панель эквалайзера)
    // =====================================================================
    public class CardPanel : Panel
    {
        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        // Рисуем в OnPaintBackground, чтобы прозрачные дочерние контролы
        // получали скруглённый фон карточки
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float k = Theme.Scale(this);
            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);

            using var path = Theme.Rounded(rect, 12f * k);
            using var fill = new SolidBrush(Theme.Card);
            using var pen = new Pen(Theme.Border);
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }
    }

    // =====================================================================
    //  Эквалайзер: отрисовка и управление полосами
    // =====================================================================
    public class EqualizerView : Control
    {
        private static readonly string[] FreqLabels =
            { "31", "62", "125", "250", "500", "1k", "2k", "4k", "8k", "16k" };

        private readonly float[] gains = new float[EqualizerSampleProvider.BandCount];
        private readonly Font labelFont = new Font("Segoe UI", 7.5f);

        private int dragBand = -1;
        private int hoverBand = -1;
        private bool active = true;

        public event EventHandler GainsChanged;

        public EqualizerView()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float[] Gains => gains;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Active
        {
            get => active;
            set { active = value; Invalidate(); }
        }

        public void SetGains(float[] source)
        {
            if (source == null) return;

            int n = Math.Min(source.Length, gains.Length);
            for (int i = 0; i < n; i++)
                gains[i] = Math.Clamp(source[i],
                    -EqualizerSampleProvider.MaxGainDb,
                    EqualizerSampleProvider.MaxGainDb);

            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) labelFont.Dispose();
            base.Dispose(disposing);
        }

        private void GetPlot(out float left, out float right, out float top, out float bottom)
        {
            float k = Theme.Scale(this);
            left = 30f * k;
            right = Width - 6f * k;
            top = 10f * k;
            bottom = Height - 18f * k;
        }

        private float GainToY(float gain, float top, float bottom)
        {
            float max = EqualizerSampleProvider.MaxGainDb;
            return top + (max - gain) / (2f * max) * (bottom - top);
        }

        private int BandAt(float x)
        {
            GetPlot(out float left, out float right, out _, out _);
            if (x < left || x > right) return -1;

            float colW = (right - left) / gains.Length;
            return Math.Clamp((int)((x - left) / colW), 0, gains.Length - 1);
        }

        private void SetFromY(int band, float y)
        {
            GetPlot(out _, out _, out float top, out float bottom);

            float max = EqualizerSampleProvider.MaxGainDb;
            float t = Math.Clamp((y - top) / Math.Max(1f, bottom - top), 0f, 1f);
            float gain = max - t * 2f * max;
            gain = MathF.Round(gain * 2f) / 2f;   // шаг 0.5 дБ

            if (Math.Abs(gains[band] - gain) < 0.001f) return;

            gains[band] = gain;
            Invalidate();
            GainsChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int band = BandAt(e.X);
            if (band < 0) return;

            dragBand = band;
            SetFromY(band, e.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (dragBand >= 0)
            {
                SetFromY(dragBand, e.Y);
                return;
            }

            int band = BandAt(e.X);
            if (band != hoverBand)
            {
                hoverBand = band;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragBand = -1;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverBand = -1;
            Invalidate();
        }

        // Двойной клик по полосе сбрасывает её в 0
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            int band = BandAt(e.X);
            if (band < 0 || Math.Abs(gains[band]) < 0.001f) return;

            gains[band] = 0f;
            Invalidate();
            GainsChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 40 || Height < 30) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            float k = Theme.Scale(this);
            GetPlot(out float left, out float right, out float top, out float bottom);

            float max = EqualizerSampleProvider.MaxGainDb;
            float zeroY = GainToY(0f, top, bottom);
            int n = gains.Length;
            float colW = (right - left) / n;

            using var dimBrush = new SolidBrush(Theme.TextDim);
            using var lightBrush = new SolidBrush(Theme.Text);

            // Шкала дБ слева
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            })
            {
                float lw = left - 8f * k;
                g.DrawString($"+{max:0}", labelFont, dimBrush, new RectangleF(0, top - 7f * k, lw, 14f * k), sf);
                g.DrawString("0", labelFont, dimBrush, new RectangleF(0, zeroY - 7f * k, lw, 14f * k), sf);
                g.DrawString($"-{max:0}", labelFont, dimBrush, new RectangleF(0, bottom - 7f * k, lw, 14f * k), sf);
            }

            // Нулевая линия
            using (var pen = new Pen(Theme.Alpha(Theme.TextDim, 90)) { DashStyle = DashStyle.Dash })
                g.DrawLine(pen, left, zeroY, right, zeroY);

            float grooveW = 3f * k;
            float thumbR = 6f * k;

            using var sfCenter = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near
            };

            for (int i = 0; i < n; i++)
            {
                float cx = left + (i + 0.5f) * colW;
                bool hot = i == hoverBand || i == dragBand;
                float thumbY = GainToY(gains[i], top, bottom);

                // Паз
                var groove = new RectangleF(cx - grooveW / 2f, top, grooveW, bottom - top);
                using (var path = Theme.Rounded(groove, grooveW / 2f))
                using (var br = new SolidBrush(Theme.Track))
                    g.FillPath(br, path);

                // Заливка от нуля до ползунка
                float fy = Math.Min(zeroY, thumbY);
                float fh = Math.Abs(zeroY - thumbY);
                if (fh >= 1f)
                {
                    var fill = new RectangleF(cx - grooveW / 2f, fy, grooveW, fh);
                    using var path = Theme.Rounded(fill, grooveW / 2f);

                    if (active)
                    {
                        using var br = new LinearGradientBrush(groove, Theme.Accent1, Theme.Accent2, 90f);
                        g.FillPath(br, path);
                    }
                    else
                    {
                        using var br = new SolidBrush(Color.FromArgb(95, 95, 110));
                        g.FillPath(br, path);
                    }
                }

                // Свечение при наведении или перетаскивании
                if (hot && active)
                {
                    float gr = thumbR + 4f * k;
                    using var glow = new SolidBrush(Theme.Alpha(Theme.Accent2, 55));
                    g.FillEllipse(glow, cx - gr, thumbY - gr, gr * 2, gr * 2);
                }

                // Ползунок
                using (var tb = new SolidBrush(Color.White))
                    g.FillEllipse(tb, cx - thumbR, thumbY - thumbR, thumbR * 2, thumbR * 2);

                using (var ring = new Pen(active ? Theme.Accent2 : Color.FromArgb(110, 110, 125), 2f * k))
                    g.DrawEllipse(ring, cx - thumbR + k, thumbY - thumbR + k, (thumbR - k) * 2, (thumbR - k) * 2);

                // Подпись частоты
                string label = i < FreqLabels.Length ? FreqLabels[i] : "";
                g.DrawString(label, labelFont, hot ? lightBrush : dimBrush,
                    new RectangleF(cx - colW / 2f, bottom + 6f * k, colW, 14f * k), sfCenter);
            }
        }
    }

    // =====================================================================
    //  Эквалайзер: звуковой фильтр (10 полос, биквады RBJ)
    // =====================================================================
    public sealed class EqualizerSampleProvider : ISampleProvider
    {
        public const int BandCount = 10;
        public const float MaxGainDb = 12f;

        private static readonly float[] Frequencies =
            { 31.25f, 62.5f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f };

        private const float Q = 1.41f;
        private const float RampDbPerRead = 1.5f;

        private readonly ISampleProvider source;
        private readonly int channels;
        private readonly int sampleRate;

        private readonly object sync = new();
        private readonly float[] target = new float[BandCount];
        private readonly float[] current = new float[BandCount];

        private readonly float[] b0 = new float[BandCount];
        private readonly float[] b1 = new float[BandCount];
        private readonly float[] b2 = new float[BandCount];
        private readonly float[] a1 = new float[BandCount];
        private readonly float[] a2 = new float[BandCount];
        private readonly bool[] bandActive = new bool[BandCount];

        private readonly float[,] z1;
        private readonly float[,] z2;

        private volatile bool enabled = true;
        private bool wasEnabled = true;

        public EqualizerSampleProvider(ISampleProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            channels = Math.Max(1, source.WaveFormat.Channels);
            sampleRate = source.WaveFormat.SampleRate;

            z1 = new float[channels, BandCount];
            z2 = new float[channels, BandCount];

            for (int i = 0; i < BandCount; i++)
                UpdateBand(i, 0f);
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        public void SetAllGains(float[] gains, bool immediate)
        {
            if (gains == null) return;

            lock (sync)
            {
                int n = Math.Min(gains.Length, BandCount);
                for (int i = 0; i < n; i++)
                {
                    target[i] = Math.Clamp(gains[i], -MaxGainDb, MaxGainDb);
                    if (immediate)
                    {
                        current[i] = target[i];
                        UpdateBand(i, current[i]);
                    }
                }
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            if (read <= 0) return read;

            if (!enabled)
            {
                wasEnabled = false;
                return read;
            }

            if (!wasEnabled)
            {
                Array.Clear(z1);
                Array.Clear(z2);
                wasEnabled = true;
            }

            StepGains();

            int ch = channels;
            for (int i = 0; i + ch <= read; i += ch)
            {
                for (int c = 0; c < ch; c++)
                {
                    float x = buffer[offset + i + c];

                    for (int b = 0; b < BandCount; b++)
                    {
                        if (!bandActive[b]) continue;

                        float y = b0[b] * x + z1[c, b];
                        z1[c, b] = b1[b] * x - a1[b] * y + z2[c, b];
                        z2[c, b] = b2[b] * x - a2[b] * y;
                        x = y;
                    }

                    buffer[offset + i + c] = x;
                }
            }

            return read;
        }

        private void StepGains()
        {
            lock (sync)
            {
                for (int i = 0; i < BandCount; i++)
                {
                    float diff = target[i] - current[i];
                    if (Math.Abs(diff) < 0.001f) continue;

                    current[i] = Math.Abs(diff) <= RampDbPerRead
                        ? target[i]
                        : current[i] + Math.Sign(diff) * RampDbPerRead;

                    UpdateBand(i, current[i]);
                }
            }
        }

        private void UpdateBand(int band, float gainDb)
        {
            float f = Frequencies[band];

            if (Math.Abs(gainDb) < 0.01f || f >= sampleRate * 0.45f)
            {
                bandActive[band] = false;
                return;
            }

            double A = Math.Pow(10.0, gainDb / 40.0);
            double w0 = 2.0 * Math.PI * f / sampleRate;
            double cos = Math.Cos(w0);
            double alpha = Math.Sin(w0) / (2.0 * Q);

            double nb0 = 1.0 + alpha * A;
            double nb1 = -2.0 * cos;
            double nb2 = 1.0 - alpha * A;
            double na0 = 1.0 + alpha / A;
            double na1 = -2.0 * cos;
            double na2 = 1.0 - alpha / A;

            b0[band] = (float)(nb0 / na0);
            b1[band] = (float)(nb1 / na0);
            b2[band] = (float)(nb2 / na0);
            a1[band] = (float)(na1 / na0);
            a2[band] = (float)(na2 / na0);
            bandActive[band] = true;
        }
    }

    // класс SearchBox
    public class SearchBox : Panel
    {
        private static readonly Font GlyphFont = new Font("Segoe MDL2 Assets", 10f);
        private readonly TextBox box;
        private bool focused;

        public event EventHandler SearchChanged;
        public event KeyEventHandler SearchKeyDown;

        public SearchBox()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            Cursor = Cursors.IBeam;

            box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Пошук треку…"
            };

            box.TextChanged += (s, e) => SearchChanged?.Invoke(this, EventArgs.Empty);
            box.KeyDown += (s, e) => SearchKeyDown?.Invoke(this, e);
            box.Enter += (s, e) => { focused = true; Invalidate(); };
            box.Leave += (s, e) => { focused = false; Invalidate(); };

            Controls.Add(box);
        }

        [Browsable(false)]
        public string Query => box.Text;

        public void Clear() => box.Clear();

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            float k = Theme.Scale(this);
            int left = (int)(30 * k);
            box.Left = left;
            box.Width = Math.Max(10, Width - left - (int)(10 * k));
            box.Top = Math.Max(0, (Height - box.Height) / 2);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            box.Focus();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float k = Theme.Scale(this);
            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);

            using (var path = Theme.Rounded(rect, 10f * k))
            using (var fill = new SolidBrush(Theme.Surface))
            using (var pen = new Pen(focused ? Theme.Alpha(Theme.Accent2, 200) : Theme.Border))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            var iconRect = new Rectangle(0, 0, (int)(28 * k), Height);
            TextRenderer.DrawText(g, "\uE721", GlyphFont, iconRect,
                focused ? Theme.Accent2 : Theme.TextDim,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }


    // =====================================================================
    //  Вкладки плейлистов
    // =====================================================================
    public class PlaylistTabs : Control
    {
        private static readonly Font TabFont = new Font("Segoe UI Semibold", 9f);
        private const TextFormatFlags MeasureFlags =
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

        private readonly List<string> names = new();
        private float[] widths = Array.Empty<float>();
        private float total;
        private float offset;
        private int active;
        private int hover = -1;      // -1: ничего, -2: кнопка «+»

        public event Action<int> TabSelected;
        public event Action AddRequested;
        public event Action<int, Point> TabContextRequested;
        public event Action<string[]> FilesDropped;

        public PlaylistTabs()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            AllowDrop = true;
            Cursor = Cursors.Hand;
            Size = new Size(334, 28);
        }

        private float K => Theme.Scale(this);
        private float PlusW => 30f * K;
        private float Gap => 4f * K;
        private float ViewW => Math.Max(10f, Width - PlusW);

        public void SetTabs(IList<string> tabNames, int activeIndex)
        {
            names.Clear();
            names.AddRange(tabNames);
            active = names.Count == 0 ? -1 : Math.Clamp(activeIndex, 0, names.Count - 1);
            Relayout();
            EnsureVisible(active);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
        }

        private void Relayout()
        {
            float k = K;
            widths = new float[names.Count];
            total = 0;

            for (int i = 0; i < names.Count; i++)
            {
                int tw = TextRenderer.MeasureText(names[i], TabFont,
                    new Size(int.MaxValue, int.MaxValue), MeasureFlags).Width;

                widths[i] = Math.Min(140f * k, tw + 26f * k);
                total += widths[i] + (i < names.Count - 1 ? Gap : 0);
            }

            ClampOffset();
        }

        private void ClampOffset()
        {
            float max = Math.Max(0f, total - ViewW);
            offset = Math.Clamp(offset, 0f, max);
        }

        private float TabLeft(int index)
        {
            float x = -offset;
            for (int i = 0; i < index && i < widths.Length; i++)
                x += widths[i] + Gap;
            return x;
        }

        private void EnsureVisible(int i)
        {
            if (i < 0 || i >= widths.Length) return;

            float left = TabLeft(i);
            float right = left + widths[i];

            if (left < 0) offset += left;
            else if (right > ViewW) offset += right - ViewW;

            ClampOffset();
        }

        // Индекс вкладки, -2 для кнопки «+», -1 для пустого места
        private int HitTest(Point p)
        {
            if (p.X >= Width - PlusW) return -2;
            if (p.X >= ViewW) return -1;

            for (int i = 0; i < widths.Length; i++)
            {
                float left = TabLeft(i);
                if (p.X >= left && p.X < left + widths[i]) return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            if (h != hover) { hover = h; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hover != -1) { hover = -1; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int h = HitTest(e.Location);
            if (h >= 0 && h != active)
            {
                active = h;
                EnsureVisible(h);
                Invalidate();
                TabSelected?.Invoke(h);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int h = HitTest(e.Location);

            if (e.Button == MouseButtons.Right)
                TabContextRequested?.Invoke(h >= 0 ? h : -1, PointToScreen(e.Location));
            else if (e.Button == MouseButtons.Left && h == -2)
                AddRequested?.Invoke();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            offset -= e.Delta / 120f * 40f * K;
            ClampOffset();
            Invalidate();
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            base.OnDragEnter(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
                FilesDropped?.Invoke(paths);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (Width < 40 || Height < 12) return;

            float k = K;
            float viewW = ViewW;
            float x = -offset;

            g.SetClip(new RectangleF(0, 0, viewW, Height));

            for (int i = 0; i < names.Count; i++)
            {
                float w = widths[i];
                var rect = new RectangleF(x, 1f, w, Height - 3f);
                x += w + Gap;

                if (rect.Right < 0 || rect.Left > viewW) continue;

                bool act = i == active;
                bool hot = i == hover;

                using (var path = Theme.Rounded(rect, 8f * k))
                {
                    if (act)
                    {
                        using var br = new LinearGradientBrush(
                            Rectangle.Round(rect), Theme.Accent1, Theme.Accent2, 0f);
                        g.FillPath(br, path);
                    }
                    else
                    {
                        using var br = new SolidBrush(hot ? Theme.SurfaceHover : Theme.Card);
                        using var pen = new Pen(Theme.Border);
                        g.FillPath(br, path);
                        g.DrawPath(pen, path);
                    }
                }

                Color textColor = act ? Color.White : hot ? Theme.Text : Theme.TextDim;
                TextRenderer.DrawText(g, names[i], TabFont, Rectangle.Round(rect), textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }

            g.ResetClip();

            // Кнопка «+»
            var plus = new RectangleF(Width - PlusW + 2f * k, 1f, PlusW - 2f * k - 1f, Height - 3f);
            using (var path = Theme.Rounded(plus, 8f * k))
            using (var br = new SolidBrush(hover == -2 ? Theme.SurfaceHover : Theme.Card))
            using (var pen = new Pen(hover == -2 ? Theme.Alpha(Theme.Accent2, 150) : Theme.Border))
            {
                g.FillPath(br, path);
                g.DrawPath(pen, path);
            }

            float cx = plus.X + plus.Width / 2f, cy = plus.Y + plus.Height / 2f, s = 5f * k;
            using (var pen = new Pen(hover == -2 ? Theme.Text : Theme.TextDim, 1.6f * k)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            })
            {
                g.DrawLine(pen, cx - s, cy, cx + s, cy);
                g.DrawLine(pen, cx, cy - s, cx, cy + s);
            }
        }
    }

    // =====================================================================
    //  Скорость воспроизведения (линейная интерполяция, меняет и тон)
    // =====================================================================
    public sealed class SpeedSampleProvider : ISampleProvider
    {
        private const int Cap = 4096;                 // размер буфера в кадрах

        private readonly ISampleProvider source;
        private readonly int ch;
        private readonly float[] inBuf;
        private readonly object sync = new();
        private int inFrames;
        private double phase;
        private volatile float speed = 1f;

        public SpeedSampleProvider(ISampleProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            ch = Math.Max(1, source.WaveFormat.Channels);
            inBuf = new float[Cap * ch];
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public float Speed
        {
            get => speed;
            set => speed = Math.Clamp(value, 0.25f, 4f);
        }

        // Вызывать после перемотки, чтобы не доигрывался старый кусок
        public void Reset()
        {
            lock (sync)
            {
                inFrames = 0;
                phase = 0;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            lock (sync)
            {
                int frames = count / ch;
                int written = 0;
                double sp = speed;

                while (written < frames)
                {
                    int i0 = (int)phase;

                    if (i0 + 1 >= inFrames)
                    {
                        int drop = Math.Min(i0, inFrames);
                        if (drop > 0)
                        {
                            Array.Copy(inBuf, drop * ch, inBuf, 0, (inFrames - drop) * ch);
                            inFrames -= drop;
                            phase -= drop;
                            i0 = (int)phase;
                        }

                        int room = Cap - inFrames;
                        int got = source.Read(inBuf, inFrames * ch, room * ch) / ch;
                        if (got <= 0) break;

                        inFrames += got;
                        continue;
                    }

                    float t = (float)(phase - i0);
                    int a = i0 * ch, b = a + ch;
                    int o = offset + written * ch;

                    for (int c = 0; c < ch; c++)
                        buffer[o + c] = inBuf[a + c] + (inBuf[b + c] - inBuf[a + c]) * t;

                    phase += sp;
                    written++;
                }

                return written * ch;
            }
        }
    }

    // =====================================================================
    //  Баланс колонок: звуковой фильтр (только стерео)
    // =====================================================================
    public sealed class BalanceSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private volatile float targetL = 1f, targetR = 1f;
        private float curL = 1f, curR = 1f;
        private float pan;

        public BalanceSampleProvider(ISampleProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        // -1 = только левая колонка, 0 = центр, +1 = только правая
        public float Balance
        {
            get => pan;
            set
            {
                pan = Math.Clamp(value, -1f, 1f);
                targetL = pan > 0f ? 1f - pan : 1f;
                targetR = pan < 0f ? 1f + pan : 1f;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            if (read <= 0 || source.WaveFormat.Channels != 2) return read;

            float tl = targetL, tr = targetR;

            // Быстрый путь: центр и ничего не меняется
            if (tl == 1f && tr == 1f && curL == 1f && curR == 1f) return read;

            for (int i = 0; i + 1 < read; i += 2)
            {
                // Плавный переход, чтобы не щёлкало при движении ползунка
                curL += (tl - curL) * 0.002f;
                curR += (tr - curR) * 0.002f;

                buffer[offset + i] *= curL;
                buffer[offset + i + 1] *= curR;
            }

            if (Math.Abs(curL - tl) < 0.0005f) curL = tl;
            if (Math.Abs(curR - tr) < 0.0005f) curR = tr;

            return read;
        }
    }

    // =====================================================================
    //  Баланс колонок: ползунок от -100 до +100
    // =====================================================================
    public class BalanceSlider : Control
    {
        private int val;
        private bool hover, dragging;

        public event EventHandler ValueChanged;

        public BalanceSlider()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(130, 20);
        }

        [DefaultValue(0)]
        public int Value
        {
            get => val;
            set
            {
                int v = Math.Clamp(value, -100, 100);
                if (v == val) return;
                val = v;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private float EdgePad => 16f * Theme.Scale(this);

        private void SetFromX(float x)
        {
            float x0 = EdgePad, x1 = Width - EdgePad;
            float frac = x1 > x0 ? Math.Clamp((x - x0) / (x1 - x0), 0f, 1f) : 0.5f;
            int v = (int)Math.Round(-100 + frac * 200);
            if (Math.Abs(v) <= 5) v = 0;      // «прилипание» к центру
            Value = v;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                SetFromX(e.X);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging) SetFromX(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (dragging)
            {
                SetFromX(e.X);
                dragging = false;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            Value = 0;
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Value = val + (e.Delta > 0 ? 5 : -5);
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 50 || Height < 8) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float k = Theme.Scale(this);
            float x0 = EdgePad, x1 = Width - EdgePad;
            float cx = (x0 + x1) / 2f;
            float cy = Height / 2f;
            float trackH = 4f * k;
            float tx = x0 + (x1 - x0) * (val + 100) / 200f;

            // Буквы по краям
            const TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                          TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            var font = Font;
            using (var f = new Font("Segoe UI Semibold", 8f))
            {
                TextRenderer.DrawText(g, "Л", f, new Rectangle(0, 0, (int)(14 * k), Height),
                    val < 0 ? Theme.Text : Theme.TextDim, flags);
                TextRenderer.DrawText(g, "П", f, new Rectangle(Width - (int)(14 * k), 0, (int)(14 * k), Height),
                    val > 0 ? Theme.Text : Theme.TextDim, flags);
            }

            // Дорожка
            var trackRect = new RectangleF(x0, cy - trackH / 2f, x1 - x0, trackH);
            using (var path = Theme.Rounded(trackRect, trackH / 2f))
            using (var br = new SolidBrush(Theme.Track))
                g.FillPath(br, path);

            // Заливка от центра к ползунку
            float fw = Math.Abs(tx - cx);
            if (fw >= 1f)
            {
                var fill = new RectangleF(Math.Min(cx, tx), cy - trackH / 2f, fw, trackH);
                using var path = Theme.Rounded(fill, trackH / 2f);
                using var br = new LinearGradientBrush(fill, Theme.Accent1, Theme.Accent2, 0f);
                g.FillPath(br, path);
            }

            // Отметка центра
            using (var pen = new Pen(Theme.Alpha(Theme.TextDim, 140), 1f * k))
                g.DrawLine(pen, cx, cy - 6f * k, cx, cy + 6f * k);

            // Ползунок
            bool active = hover || dragging;
            float r = (active ? 7f : 6f) * k;

            if (active)
            {
                float gr = r + 3f * k;
                using var glow = new SolidBrush(Theme.Alpha(Theme.Accent2, dragging ? 70 : 45));
                g.FillEllipse(glow, tx - gr, cy - gr, gr * 2, gr * 2);
            }

            using (var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.FillEllipse(shadow, tx - r, cy - r + 1f * k, r * 2, r * 2);

            using (var thumb = new SolidBrush(Color.White))
                g.FillEllipse(thumb, tx - r, cy - r, r * 2, r * 2);
        }
    }


    
    // =====================================================================
    //  Плейлист: номер, название, оценка звёздами, длительность
    // =====================================================================
    public class ModernListBox : ListBox
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONDBLCLK = 0x0203;

        private static readonly Font GlyphFont = new Font("Segoe MDL2 Assets", 9f);
        private static readonly Font NumberFont = new Font("Segoe UI", 8.5f);
        private static readonly Font TimeFont = new Font("Segoe UI", 8.5f);
        private static readonly Color StarOn = Color.FromArgb(255, 196, 61);

        private int hover = -1;
        private int hoverStar;
        private string searchText = "";

        // Форма подставляет сюда свои функции: оценка (0-5) и длительность трека
        public Func<object, int> RatingProvider;
        public Func<object, TimeSpan> DurationProvider;

        // (индекс строки, число звёзд 1-5)
        public event Action<int, int> RatingClicked;

        public ModernListBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            BorderStyle = BorderStyle.None;
            IntegralHeight = false;
            ItemHeight = 34;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SearchText
        {
            get => searchText;
            set { searchText = value ?? ""; Invalidate(); }
        }

        private bool Matches(int index) =>
            searchText.Length == 0 ||
            Items[index].ToString().Contains(searchText, StringComparison.CurrentCultureIgnoreCase);

        // Индекс первого совпадения, начиная со start (с переходом через конец списка), или -1
        public int FindMatch(int start)
        {
            int n = Items.Count;
            if (n == 0 || searchText.Length == 0) return -1;

            for (int i = 0; i < n; i++)
            {
                int idx = ((start + i) % n + n) % n;
                if (Matches(idx)) return idx;
            }
            return -1;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            try { SetWindowTheme(Handle, "DarkMode_Explorer", null); }
            catch { }
        }

        // ---------- Геометрия звёзд ----------
        private void StarsLayout(Rectangle bounds, out float left, out float size, out float step)
        {
            float k = Theme.Scale(this);
            size = 11f * k;
            step = 13f * k;

            float rowRight = bounds.Right - 6f * k;
            float right = rowRight - 10f * k - 40f * k - 6f * k;   // слева от длительности
            left = right - (4 * step + size);
        }

        // 0 = курсор не над звёздами, 1-5 = номер звезды
        private int StarAt(int index, Point p)
        {
            if (index < 0 || index >= Items.Count) return 0;

            Rectangle b = GetItemRectangle(index);
            if (p.Y < b.Y || p.Y >= b.Bottom) return 0;

            StarsLayout(b, out float left, out float size, out float step);

            float x = p.X - left + (step - size) / 2f;
            if (x < 0 || x >= 5 * step) return 0;

            return Math.Min(5, (int)(x / step) + 1);
        }

        private static GraphicsPath StarPath(float cx, float cy, float r)
        {
            var pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double a = -Math.PI / 2 + i * Math.PI / 5;
                float rr = i % 2 == 0 ? r : r * 0.45f;
                pts[i] = new PointF(cx + (float)(Math.Cos(a) * rr), cy + (float)(Math.Sin(a) * rr));
            }

            var p = new GraphicsPath();
            p.AddPolygon(pts);
            return p;
        }

        // Клик по звёздам обрабатываем сами: строка не выделяется и трек не запускается
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONDBLCLK)
            {
                long lp = m.LParam.ToInt64();
                var pt = new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));

                int idx = IndexFromPoint(pt);
                int star = StarAt(idx, pt);

                if (star > 0)
                {
                    RatingClicked?.Invoke(idx, star);
                    return;
                }
            }

            base.WndProc(ref m);
        }

        // ---------- Мышь ----------
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int idx = IndexFromPoint(e.Location);
            int star = StarAt(idx, e.Location);

            Cursor = star > 0 ? Cursors.Hand : Cursors.Default;

            if (idx == hover && star == hoverStar) return;

            int old = hover;
            hover = idx;
            hoverStar = star;
            InvalidateRow(old);
            InvalidateRow(hover);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            int old = hover;
            hover = -1;
            hoverStar = 0;
            Cursor = Cursors.Default;
            InvalidateRow(old);
        }

        private void InvalidateRow(int index)
        {
            if (index >= 0 && index < Items.Count)
                Invalidate(GetItemRectangle(index));
        }

        private static string FormatTime(TimeSpan t) =>
            t.TotalHours >= 1
                ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
                : $"{t.Minutes:00}:{t.Seconds:00}";

        // ---------- Отрисовка ----------
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float k = Theme.Scale(this);
            bool selected = (e.State & DrawItemState.Selected) != 0;
            bool hot = e.Index == hover;
            bool dim = searchText.Length > 0 && !Matches(e.Index);
            Rectangle r = e.Bounds;
            object item = Items[e.Index];

            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, r);

            var row = new RectangleF(r.X + 6 * k, r.Y + 2 * k, r.Width - 12 * k, r.Height - 4 * k);

            if (selected)
            {
                using var path = Theme.Rounded(row, 8f * k);
                using var fill = new LinearGradientBrush(
                    Rectangle.Round(row),
                    Theme.Alpha(Theme.Accent1, 60),
                    Theme.Alpha(Theme.Accent2, 40), 0f);
                g.FillPath(fill, path);

                var bar = new RectangleF(row.X, row.Y + 7 * k, 3f * k, row.Height - 14 * k);
                using var barPath = Theme.Rounded(bar, 1.5f * k);
                using var barBrush = new LinearGradientBrush(
                    Rectangle.Round(bar), Theme.Accent1, Theme.Accent2, 90f);
                g.FillPath(barBrush, barPath);
            }
            else if (hot)
            {
                using var path = Theme.Rounded(row, 8f * k);
                using var fill = new SolidBrush(Color.FromArgb(14, 255, 255, 255));
                g.FillPath(fill, path);
            }

            Color numColor = dim ? Theme.Mix(Theme.TextDim, BackColor, 0.5f) : Theme.TextDim;
            Color textColor = selected ? Color.White
                            : dim ? Theme.Mix(Theme.Text, BackColor, 0.7f)
                            : Theme.Text;

            // Номер или значок воспроизведения
            var numRect = new Rectangle((int)(row.X + 10 * k), r.Y, (int)(28 * k), r.Height);
            const TextFormatFlags center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                           TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                                           TextFormatFlags.SingleLine;

            if (selected)
                TextRenderer.DrawText(g, "\uE768", GlyphFont, numRect, Theme.Accent2, center);
            else
                TextRenderer.DrawText(g, (e.Index + 1).ToString("00"), NumberFont, numRect, numColor, center);

            // Длительность справа
            TimeSpan dur = DurationProvider?.Invoke(item) ?? TimeSpan.Zero;
            if (dur > TimeSpan.Zero)
            {
                var timeRect = new Rectangle(
                    (int)(row.Right - 10 * k - 40 * k), r.Y, (int)(40 * k), r.Height);

                TextRenderer.DrawText(g, FormatTime(dur), TimeFont, timeRect,
                    dim ? numColor : selected ? Theme.Text : Theme.TextDim,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }

            // Звёзды
            StarsLayout(r, out float starsLeft, out float starSize, out float starStep);

            int rating = RatingProvider?.Invoke(item) ?? 0;
            int shown = hot && hoverStar > 0 ? hoverStar : rating;

            if (shown > 0 || hot || selected)
            {
                float cy = r.Y + r.Height / 2f;
                int onAlpha = dim ? 90 : 255;

                using var on = new SolidBrush(Color.FromArgb(onAlpha, StarOn));
                using var off = new SolidBrush(Theme.Alpha(Theme.Track, dim ? 90 : 255));

                for (int i = 0; i < 5; i++)
                {
                    float cx = starsLeft + i * starStep + starSize / 2f;
                    using var path = StarPath(cx, cy, starSize / 2f + 0.5f * k);
                    g.FillPath(i < shown ? on : off, path);
                }
            }

            // Название
            int textLeft = numRect.Right + (int)(6 * k);
            int textRight = (int)(starsLeft - 8 * k);

            var textRect = new Rectangle(textLeft, r.Y, Math.Max(10, textRight - textLeft), r.Height);

            TextRenderer.DrawText(g, item.ToString(), Font, textRect, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }

    // =====================================================================
    //  Обложка
    // =====================================================================
    public class CoverBox : PictureBox
    {
        private Bitmap blur;
        private Image blurSource;

        public CoverBox()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) blur?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Color bg = Parent?.BackColor ?? Theme.Bg;

            using (var b = new SolidBrush(BackColor))
                g.FillRectangle(b, ClientRectangle);

            bool drawn = false;
            Image img = Image;

            if (img != null)
            {
                try
                {
                    DrawCover(g, img);
                    drawn = true;
                }
                catch
                {
                    // изображение могло быть уже освобождено
                }
            }

            if (!drawn) DrawPlaceholder(g);

            // Плавный переход в цвет формы снизу
            int fadeH = Math.Max(8, (int)(Height * 0.28));
            var fadeRect = new Rectangle(0, Height - fadeH, Width, fadeH + 1);
            using var fade = new LinearGradientBrush(fadeRect, Theme.Alpha(bg, 0), bg, 90f);
            g.FillRectangle(fade, fadeRect);
        }

        private void DrawCover(Graphics g, Image img)
        {
            EnsureBlur(img);

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (blur != null)
            {
                g.DrawImage(blur, ClientRectangle);
                using var dim = new SolidBrush(Color.FromArgb(150, 12, 12, 16));
                g.FillRectangle(dim, ClientRectangle);
            }

            float s = Math.Min((float)Width / img.Width, (float)Height / img.Height);
            float w = img.Width * s, h = img.Height * s;
            g.DrawImage(img, (Width - w) / 2f, (Height - h) / 2f, w, h);
        }

        private void EnsureBlur(Image img)
        {
            if (ReferenceEquals(blurSource, img)) return;

            blur?.Dispose();
            blur = null;
            blurSource = img;

            try
            {
                var bmp = new Bitmap(24, 24);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g.DrawImage(img, 0, 0, 24, 24);
                }
                blur = bmp;
            }
            catch
            {
                blur = null;
            }
        }

        private void DrawPlaceholder(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            using (var br = new LinearGradientBrush(ClientRectangle,
                       Color.FromArgb(44, 34, 84), Color.FromArgb(20, 44, 80), 35f))
                g.FillRectangle(br, ClientRectangle);

            using var font = new Font("Segoe UI Symbol", Math.Max(20f, Height / 4f));
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var tb = new SolidBrush(Color.FromArgb(120, 255, 255, 255));
            g.DrawString("\u266A", font, tb, ClientRectangle, sf);
        }
    }

    // =====================================================================
    //  Тёмное меню
    // =====================================================================
    public sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColors())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var b = new SolidBrush(Theme.Card);
            e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is not ToolStripDropDown) return;

            using var p = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            if (!item.Selected || !item.Enabled) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new RectangleF(4, 2, item.Width - 8, item.Height - 4);
            using var path = Theme.Rounded(r, 6f);
            using var br = new LinearGradientBrush(
                Rectangle.Round(r),
                Theme.Alpha(Theme.Accent1, 170),
                Theme.Alpha(Theme.Accent2, 150), 0f);
            g.FillPath(br, path);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = !e.Item.Enabled ? Theme.TextDim
                        : e.Item.Selected ? Color.White
                        : Theme.Text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var p = new Pen(Theme.Border);
            e.Graphics.DrawLine(p, 8, y, e.Item.Width - 8, y);
        }

        private sealed class DarkColors : ProfessionalColorTable
        {
            public DarkColors() { UseSystemColors = false; }

            public override Color ToolStripDropDownBackground => Theme.Card;
            public override Color MenuBorder => Theme.Border;
            public override Color MenuItemBorder => Color.Transparent;
            public override Color MenuItemSelected => Theme.SurfaceHover;
            public override Color MenuItemSelectedGradientBegin => Theme.SurfaceHover;
            public override Color MenuItemSelectedGradientEnd => Theme.SurfaceHover;
            public override Color ImageMarginGradientBegin => Theme.Card;
            public override Color ImageMarginGradientMiddle => Theme.Card;
            public override Color ImageMarginGradientEnd => Theme.Card;
            public override Color SeparatorDark => Theme.Border;
            public override Color SeparatorLight => Theme.Border;
        }
    }

    // =====================================================================
    //  Спектр: расчёт (FFT), звук не меняет
    // =====================================================================
    public sealed class SpectrumSampleProvider : ISampleProvider
    {
        private const int FftSize = 2048;
        private const int Hop = 512;
        private const float MinDb = -70f;
        private const float MaxDb = -10f;

        private readonly ISampleProvider _source;
        private readonly Func<float> _gain;
        private readonly int _channels;
        private readonly int _bands;

        private readonly float[] _ring = new float[FftSize];
        private readonly float[] _window = new float[FftSize];
        private readonly float[] _re = new float[FftSize];
        private readonly float[] _im = new float[FftSize];
        private readonly float[] _cos = new float[FftSize / 2];
        private readonly float[] _sin = new float[FftSize / 2];
        private readonly int[] _binStart;
        private readonly int[] _binEnd;

        private int _wp, _hopCount, _filled;

        private readonly object _lock = new();
        private readonly Queue<(long Tick, float[] Bands)> _frames = new();
        private long _lastRead;

        public WaveFormat WaveFormat => _source.WaveFormat;
        public int BandCount => _bands;

        public SpectrumSampleProvider(ISampleProvider source, Func<float> gain = null, int bands = 24)
        {
            _source = source;
            _gain = gain;
            _channels = Math.Max(1, source.WaveFormat.Channels);
            _bands = Math.Max(1, bands);

            for (int i = 0; i < FftSize; i++)
                _window[i] = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / (FftSize - 1)));

            for (int i = 0; i < FftSize / 2; i++)
            {
                double a = 2.0 * Math.PI * i / FftSize;
                _cos[i] = (float)Math.Cos(a);
                _sin[i] = (float)-Math.Sin(a);
            }

            double sr = source.WaveFormat.SampleRate;
            double binHz = sr / FftSize;
            double fMin = 50.0;
            double fMax = Math.Min(16000.0, sr / 2.0 * 0.95);

            _binStart = new int[_bands];
            _binEnd = new int[_bands];

            for (int b = 0; b < _bands; b++)
            {
                double f0 = fMin * Math.Pow(fMax / fMin, (double)b / _bands);
                double f1 = fMin * Math.Pow(fMax / fMin, (double)(b + 1) / _bands);

                int s = Math.Max(1, (int)Math.Floor(f0 / binHz));
                int e = Math.Max(s, (int)Math.Floor(f1 / binHz));

                _binStart[b] = Math.Min(s, FftSize / 2 - 1);
                _binEnd[b] = Math.Min(e, FftSize / 2 - 1);
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);
            Interlocked.Exchange(ref _lastRead, Environment.TickCount64);
            if (read <= 0) return read;

            float gain = _gain?.Invoke() ?? 1f;
            float inv = 1f / Math.Max(gain, 0.03f);
            int ch = _channels;

            for (int i = 0; i + ch <= read; i += ch)
            {
                float sum = 0f;
                for (int c = 0; c < ch; c++)
                    sum += buffer[offset + i + c];

                _ring[_wp] = sum / ch * inv;
                _wp = (_wp + 1) & (FftSize - 1);
                if (_filled < FftSize) _filled++;

                if (++_hopCount >= Hop)
                {
                    _hopCount = 0;
                    if (_filled >= FftSize)
                    {
                        try { Analyze(); }
                        catch { /* визуализация не должна ломать звук */ }
                    }
                }
            }

            return read;
        }

        private void Analyze()
        {
            for (int i = 0; i < FftSize; i++)
            {
                _re[i] = _ring[(_wp + i) & (FftSize - 1)] * _window[i];
                _im[i] = 0f;
            }

            Fft();

            const float scale = 4f / FftSize;
            var bands = new float[_bands];

            for (int b = 0; b < _bands; b++)
            {
                float max = 0f;
                for (int k = _binStart[b]; k <= _binEnd[b]; k++)
                {
                    float mag = MathF.Sqrt(_re[k] * _re[k] + _im[k] * _im[k]) * scale;
                    if (mag > max) max = mag;
                }

                float db = 20f * MathF.Log10(max + 1e-6f);
                db += 12f * b / Math.Max(1, _bands - 1);
                bands[b] = Math.Clamp((db - MinDb) / (MaxDb - MinDb), 0f, 1f);
            }

            lock (_lock)
            {
                _frames.Enqueue((Environment.TickCount64, bands));
                while (_frames.Count > 64) _frames.Dequeue();
            }
        }

        private void Fft()
        {
            int n = FftSize;

            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;

                if (i < j)
                {
                    (_re[i], _re[j]) = (_re[j], _re[i]);
                    (_im[i], _im[j]) = (_im[j], _im[i]);
                }
            }

            for (int len = 2; len <= n; len <<= 1)
            {
                int half = len >> 1;
                int step = n / len;

                for (int i = 0; i < n; i += len)
                {
                    for (int k = 0, t = 0; k < half; k++, t += step)
                    {
                        float wr = _cos[t], wi = _sin[t];
                        int a = i + k, b = a + half;

                        float tr = _re[b] * wr - _im[b] * wi;
                        float ti = _re[b] * wi + _im[b] * wr;

                        _re[b] = _re[a] - tr;
                        _im[b] = _im[a] - ti;
                        _re[a] += tr;
                        _im[a] += ti;
                    }
                }
            }
        }

        public bool Poll(int delayMs, float[] dest)
        {
            long limit = Environment.TickCount64 - delayMs;
            float[] newest = null;

            lock (_lock)
            {
                while (_frames.Count > 0 && _frames.Peek().Tick <= limit)
                    newest = _frames.Dequeue().Bands;
            }

            if (newest == null) return false;
            Array.Copy(newest, dest, Math.Min(newest.Length, dest.Length));
            return true;
        }

        public bool HasPending
        {
            get { lock (_lock) return _frames.Count > 0; }
        }

        public bool IsActive =>
            Environment.TickCount64 - Interlocked.Read(ref _lastRead) < 250;
    }

    // =====================================================================
    //  Спектр: отрисовка
    // =====================================================================
    public class SpectrumView : Control
    {
        private const int DelayMs = 120;

        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 20 };
        private SpectrumSampleProvider _source;

        private float[] _target = new float[24];
        private float[] _level = new float[24];
        private float[] _peak = new float[24];
        private int[] _hold = new int[24];

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SpectrumSampleProvider Source
        {
            get => _source;
            set
            {
                _source = value;
                int n = value?.BandCount ?? _level.Length;
                if (n != _level.Length)
                {
                    _target = new float[n];
                    _level = new float[n];
                    _peak = new float[n];
                    _hold = new int[n];
                }
            }
        }

        public SpectrumView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _timer.Tick += OnTick;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode) _timer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _timer.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!Visible) return;

            var src = _source;
            if (src != null)
            {
                bool got = src.Poll(DelayMs, _target);
                if (!got && !src.IsActive && !src.HasPending)
                    Array.Clear(_target);
            }
            else
            {
                Array.Clear(_target);
            }

            bool changed = false;

            for (int i = 0; i < _level.Length; i++)
            {
                float t = _target[i];
                float l = _level[i];

                if (t > l) l += (t - l) * 0.55f;
                else l = Math.Max(t, l - 0.03f);
                if (l < 0.002f) l = 0f;

                float p = _peak[i];
                if (l >= p) { p = l; _hold[i] = 18; }
                else if (_hold[i] > 0) _hold[i]--;
                else p = Math.Max(l, p - 0.012f);

                if (Math.Abs(l - _level[i]) > 0.001f || Math.Abs(p - _peak[i]) > 0.001f)
                    changed = true;

                _level[i] = l;
                _peak[i] = p;
            }

            if (changed) Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Bg);

            if (Width < 20 || Height < 20) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            var card = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = Theme.Rounded(card, 10f))
            using (var cardBrush = new SolidBrush(Theme.Card))
            using (var border = new Pen(Theme.Border))
            {
                g.FillPath(cardBrush, path);
                g.DrawPath(border, path);
            }

            const int padX = 10, padY = 8;
            var area = new Rectangle(padX, padY, Width - 2 * padX, Height - 2 * padY);
            int n = _level.Length;
            const float gap = 3f;
            float barW = (area.Width - gap * (n - 1)) / n;
            if (barW < 1f || area.Height < 8) return;

            float radius = Math.Min(barW / 2f, 2f);

            // Сверху голубой, снизу фиолетовый
            using var grad = new LinearGradientBrush(area, Theme.Accent2, Theme.Accent1, 90f);
            using var idle = new SolidBrush(Theme.Track);
            using var cap = new SolidBrush(Theme.Alpha(Theme.Accent2, 230));

            for (int i = 0; i < n; i++)
            {
                float x = area.X + i * (barW + gap);
                float h = Math.Max(2f, _level[i] * area.Height);
                var bar = new RectangleF(x, area.Bottom - h, barW, h);

                using (var path = Theme.Rounded(bar, radius))
                    g.FillPath(_level[i] > 0.001f ? (Brush)grad : idle, path);

                if (_peak[i] > 0.02f)
                {
                    float cy = Math.Max(area.Top, area.Bottom - _peak[i] * area.Height - 3f);
                    g.FillRectangle(cap, x, cy, barW, 2f);
                }
            }
        }
    }
}