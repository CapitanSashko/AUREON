using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MP3
{
    // Сповіщення про трек у верхній частині екрана (як в AIMP 3).
    // Не забирає фокус, не показується в панелі завдань і в Alt+Tab.
    [System.ComponentModel.DesignerCategory("Code")]
    public sealed class TrackToast : Form
    {
        // ----- налаштування (значення для 96 DPI) -----
        private const int HoldMs = 4500;       // скільки висить на екрані
        private const int InMs = 260;          // тривалість появи
        private const int OutMs = 320;         // тривалість зникнення
        private const int TopMargin = 12;      // відступ від верху робочої області
        private const int ToastWidth = 400;
        private const int ToastHeight = 98;
        private const int CoverSize = 74;
        private const int Radius = 14;

        // ----- кольори (темна тема плеєра) -----
        private static readonly Color Back = Color.FromArgb(24, 24, 30);
        private static readonly Color BorderCol = Color.FromArgb(62, 62, 76);
        private static readonly Color Accent = Color.FromArgb(0, 120, 212);
        private static readonly Color TextMain = Color.FromArgb(240, 240, 244);
        private static readonly Color TextSub = Color.FromArgb(185, 185, 195);
        private static readonly Color TextMuted = Color.FromArgb(135, 135, 148);

        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        private readonly Font _fTitle = new Font("Segoe UI Semibold", 11.5F);
        private readonly Font _fSub = new Font("Segoe UI", 9.5F);
        private readonly Font _fSmall = new Font("Segoe UI", 8.5F);
        private readonly Font _fNote = new Font("Segoe UI Symbol", 24F);

        private long _lastTick;
        private long _holdStart;
        private double _progress;      // 0 = сховане, 1 = повністю показане
        private int _target;           // 1 = показувати, 0 = ховати
        private bool _hover;
        private int _finalTop;

        private Bitmap _thumb;
        private string _title = "", _line2 = "", _line3 = "";

        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96.0);

        public TrackToast()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Back;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Opacity = 0;

            ClientSize = new Size(S(ToastWidth), S(ToastHeight));
            _timer.Tick += (_, _) => Tick();
        }

        // Показати вікно без активації (не краде фокус у гри, відео, текстових редакторів)
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000;   // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080;   // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        // ==================== Публічний інтерфейс ====================

        // Показує (або оновлює) сповіщення. cover копіюється, тому оригінал можна звільняти.
        public void ShowTrack(Image cover, string title, string line2, string line3, Rectangle workArea)
        {
            SetCover(cover);
            _title = title ?? "";
            _line2 = line2 ?? "";
            _line3 = line3 ?? "";

            Left = workArea.Left + (workArea.Width - Width) / 2;
            _finalTop = workArea.Top + S(TopMargin);

            long now = _clock.ElapsedMilliseconds;
            _lastTick = now;
            _holdStart = now;
            _target = 1;

            if (!Visible)
            {
                _progress = 0;
                ApplyProgress();   // ставимо вікно за верхню межу, прозорість 0
                Show();            // без активації
            }

            _timer.Start();
            Invalidate();
        }

        // Плавно сховати
        public void Dismiss()
        {
            if (!Visible) return;
            _target = 0;
            _lastTick = _clock.ElapsedMilliseconds;
            _timer.Start();
        }

        // Сховати негайно
        public void HideNow()
        {
            _timer.Stop();
            _target = 0;
            _progress = 0;
            if (Visible) Hide();
        }

        // ==================== Анімація ====================
        private void Tick()
        {
            long now = _clock.ElapsedMilliseconds;
            double dt = Math.Min(60, now - _lastTick);
            _lastTick = now;

            if (_target == 1)
            {
                if (_progress < 1.0)
                {
                    _progress = Math.Min(1.0, _progress + dt / InMs);
                    _holdStart = now;               // поки з'являється, відлік утримання на нулі
                }
                else if (_hover)
                {
                    _holdStart = now;               // курсор над вікном: не ховаємо
                }
                else if (now - _holdStart >= HoldMs)
                {
                    _target = 0;
                }
            }
            else
            {
                _progress = Math.Max(0.0, _progress - dt / OutMs);
                if (_progress <= 0.0)
                {
                    _timer.Stop();
                    Hide();
                    return;
                }
            }

            ApplyProgress();
            Invalidate(new Rectangle(0, Height - S(4), Width, S(4))); // смужка таймера
        }

        private void ApplyProgress()
        {
            double e = 1.0 - Math.Pow(1.0 - _progress, 3.0);   // плавне гальмування
            int offset = (int)Math.Round((1.0 - e) * (Height + S(TopMargin)));
            int top = _finalTop - offset;
            if (Top != top) Top = top;

            double op = Math.Min(1.0, _progress * 1.8);
            if (Math.Abs(Opacity - op) > 0.005) Opacity = op;
        }

        // ==================== Миша ====================
        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _holdStart = _clock.ElapsedMilliseconds - (HoldMs - 1500); // після виходу ще ~1.5 с
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            Dismiss();
            base.OnMouseClick(e);
        }

        // ==================== Обкладинка ====================
        private void SetCover(Image cover)
        {
            Bitmap old = _thumb;
            _thumb = null;

            if (cover != null)
            {
                try { _thumb = MakeSquare(cover, S(CoverSize)); }
                catch { _thumb = null; }
            }

            old?.Dispose();
        }

        // Вирізає по центру квадрат і зменшує до потрібного розміру
        private static Bitmap MakeSquare(Image src, int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            float side = Math.Min(src.Width, src.Height);
            float sx = (src.Width - side) / 2f;
            float sy = (src.Height - side) / 2f;

            using var attrs = new ImageAttributes();
            attrs.SetWrapMode(WrapMode.TileFlipXY);

            g.DrawImage(src, new Rectangle(0, 0, size, size),
                sx, sy, side, side, GraphicsUnit.Pixel, attrs);
            return bmp;
        }

        // ==================== Малювання ====================
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Back);

            // Смужка таймера знизу (скільки ще висітиме)
            double fraction = 1.0;
            if (_progress >= 1.0)
                fraction = 1.0 - (double)(_clock.ElapsedMilliseconds - _holdStart) / HoldMs;
            fraction = Math.Clamp(fraction, 0.0, 1.0);

            int barH = S(3);
            int barW = (int)Math.Round(Width * fraction);
            if (barW > 0)
            {
                using var barBrush = new SolidBrush(Accent);
                g.FillRectangle(barBrush, 0, Height - barH, barW, barH);
            }

            // Обкладинка
            int pad = S(12);
            int cs = S(CoverSize);
            var coverRect = new Rectangle(pad, (Height - barH - cs) / 2, cs, cs);

            using (var path = RoundRect(coverRect, S(10)))
            {
                var state = g.Save();
                g.SetClip(path);

                if (_thumb != null)
                {
                    g.DrawImage(_thumb, coverRect);
                }
                else
                {
                    using var br = new LinearGradientBrush(coverRect,
                        Color.FromArgb(56, 56, 70), Color.FromArgb(28, 28, 36), 55f);
                    g.FillRectangle(br, coverRect);

                    TextRenderer.DrawText(g, "\u266B", _fNote, coverRect, Color.FromArgb(100, 100, 118),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding);
                }

                g.Restore(state);
            }

            // Текст
            int tx = coverRect.Right + S(14);
            int tw = Math.Max(10, Width - tx - pad);
            int y0 = coverRect.Top;

            const TextFormatFlags flags =
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

            if (_title.Length > 0)
                TextRenderer.DrawText(g, _title, _fTitle, new Rectangle(tx, y0, tw, S(28)), TextMain, flags);

            if (_line2.Length > 0)
                TextRenderer.DrawText(g, _line2, _fSub, new Rectangle(tx, y0 + S(28), tw, S(20)), TextSub, flags);

            if (_line3.Length > 0)
                TextRenderer.DrawText(g, _line3, _fSmall, new Rectangle(tx, y0 + S(52), tw, S(18)), TextMuted, flags);

            // Рамка
            using var borderPath = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), S(Radius));
            using var pen = new Pen(BorderCol, 1f);
            g.DrawPath(pen, borderPath);
        }

        // Закруглені кути вікна
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (Width <= 0 || Height <= 0) return;
            using var path = RoundRect(new Rectangle(0, 0, Width, Height), S(Radius));
            Region = new Region(path);
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _thumb?.Dispose();
                _fTitle.Dispose();
                _fSub.Dispose();
                _fSmall.Dispose();
                _fNote.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}