using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using SoundTouch.Net.NAudioSupport;

namespace MP3
{
    public partial class Form1 : Form
    {
        // ==================== Константи ====================

        // false: папка = одна вкладка з усіма треками з підпапок
        // true: кожна підпапка-альбом = окрема вкладка
        private const bool SubfoldersAsTabs = false;

        // ==================== Власна панель AUREON ====================

        private Panel titleBar;

        private int _heightBeforeEq; // Запоминаем высоту окна до открытия EQ

        private int _lastClientHeight;
        private int _listPlayerBaseTop; // Top плейлиста (не змінюємо при ресайзі знизу)
        private Label titleLabel;
        private Button minimizeButton;
        private Button closeButton;

        private SearchBox searchBox;
        private PlaylistTabs playlistTabs;
        private BalanceSlider balanceSlider;

        // YouTube Music (WebView2)
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Button onlineButton;
        private bool onlineOpen;
        private HScrollBar onlineScroll;
        private const int OnlineWideWidth = 900;   // ширина сторінки для Spotify та Apple Music
        private bool reopenOnlineAfterRestore;
        private const bool UseMobileUserAgent = true;

        private enum OnlineService { YouTubeMusic, Spotify, AppleMusic }
        private OnlineService onlineService = OnlineService.YouTubeMusic;
        private ContextMenuStrip onlineMenu;

        private const string DesktopUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0";

        private const string OnlineStartUrl = "https://music.youtube.com";
        private const string MobileUserAgent =
            "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36";
        private static readonly string WebViewProfileDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "AUREON", "webview2");

        // Власні пресети та швидкість
        private readonly List<(string Name, float[] Gains)> customPresets = new();
        private static readonly string PresetsFilePath =
            Path.Combine(AppContext.BaseDirectory, "equalizer", "presets.cfg");

        private float speed = 1f;
        private SoundTouchWaveStream? soundTouchStream;
        private RoundButton speedButton;
        private ContextMenuStrip speedMenu;
        private static readonly float[] SpeedSteps = { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f };
        private float balance;                       // -1 ... +1
        private BalanceSampleProvider balanceProvider;
        private AudioEffectsSampleProvider audioEffects;
        private MasterVolumeSampleProvider masterVolume;
        private readonly Dictionary<string, int> ratings = new(StringComparer.OrdinalIgnoreCase);

        private bool _resizingBottom;
        private int _resizeStartY;
        private int _resizeStartHeight;
        private int _listStartHeight;
        private const int BottomResizeBorder = 6; // зона захвату знизу, px

        private const int TitleBarHeight = 30;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const string PlayGlyph = "\uE768";
        private const string PauseGlyph = "\uE769";
        private const double FadeDurationSeconds = 1.2; // було 2.0 — м’якше на старті

        // true: файл cover.jpg у папці важливіший за обкладинку, вшиту в трек
        private static readonly bool PreferFolderCover = false;

        // Найбільша сторона обкладинки в пам'яті (більші зображення зменшуються)
        private const int MaxCoverSize = 800;

        private static readonly string PlaylistDir =
            Path.Combine(AppContext.BaseDirectory, "playlist");

        // Файл вкладки «Default» (залишився від старої версії, тож нічого не губиться)
        private static readonly string PlaylistFilePath =
            Path.Combine(PlaylistDir, "playlist.m3u8");

        // Список вкладок і активна вкладка
        private static readonly string TabsFilePath =
            Path.Combine(PlaylistDir, "tabs.cfg");

        private static readonly string RatingsFilePath =
            Path.Combine(PlaylistDir, "ratings.cfg");

        private static readonly string EqSettingsPath =
            Path.Combine(AppContext.BaseDirectory, "equalizer", "equalizer.cfg");

        private Size originalSize;


        // Заокруглені кути форми
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        private static bool IsWin11 => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundedCorners();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (IsHandleCreated && !IsWin11)
                ApplyRoundedCorners();

            if (WindowState == FormWindowState.Normal && !isCustomSize && !onlineOpen)
                SyncPlaylistToFormHeight();
        }

        private void ApplyRoundedCorners()
        {
            if (IsWin11)
            {
                int round = 2;   // DWMWCP_ROUND
                DwmSetWindowAttribute(Handle, 33, ref round, sizeof(int));   // DWMWA_WINDOW_CORNER_PREFERENCE
                return;
            }

            IntPtr rgn = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 16, 16);
            Region = Region.FromHrgn(rgn);
            DeleteObject(rgn);
        }


        // ==================== Стан ====================
        private bool isCustomSize;
        private bool isRepeating;
        private bool isShuffle;
        private readonly Random random = new();
        private bool isDragging;
        private bool isChangingTrack;

        private WasapiOut waveOutDevice;
        private AudioFileReader audioFileReader;
        private EqualizerSampleProvider equalizer;
        private SpectrumSampleProvider spectrum;
        private readonly System.Windows.Forms.Timer fadeTimer = new() { Interval = 50 };

        private readonly ConcurrentDictionary<string, TimeSpan> _durations = new(StringComparer.OrdinalIgnoreCase);
        private int _durationVersion;
        private int _coverVersion;

        // ==================== Сповіщення про трек ====================
        private TrackToast toast;
        private bool toastEnabled = true;
        private string _lastToastFile;
        private static readonly string ToastSettingsPath =
            Path.Combine(AppContext.BaseDirectory, "toast.cfg");

        private readonly Dictionary<Control, Point> _savedLocations = new();
        private Size _savedMinSize;
        private bool _layoutSaved;

        // ==================== Плейлисти (вкладки): стан ====================
        private readonly List<PlaylistData> playlists = new();
        private PlaylistData viewList;      // відкритий у вкладці
        private PlaylistData playList;      // з нього зараз грає музика
        private Track playingTrack;

        private ContextMenuStrip tabMenu;
        private ToolStripItem miRename, miClear, miClose;
        private int tabMenuIndex = -1;

        // ==================== Еквалайзер: стан ====================
        private const string EqCustomName = "Власні";
        private const int EqGap = 4;

        private static readonly (string Name, float[] Gains)[] EqPresets =
        {
            ("Плоский", new float[] {  0,  0,  0,  0,  0,  0,  0,  0,  0,  0 }),
            ("Бас",     new float[] {  6,  5,  4,  2,  0,  0,  0,  0,  0,  0 }),
            ("Високі",  new float[] {  0,  0,  0,  0,  0,  1,  2,  4,  5,  6 }),
            ("Вокал",   new float[] { -2, -2, -1,  1,  3,  4,  3,  1, -1, -2 }),
            ("Рок",     new float[] {  5,  4,  3,  1, -1, -1,  1,  3,  4,  5 }),
            ("Поп",     new float[] { -1,  2,  4,  5,  3,  0, -1, -1, -1, -1 }),
            ("Джаз",    new float[] {  4,  3,  1,  2, -2, -2,  0,  2,  3,  4 }),
        };

        private readonly float[] eqGains = new float[EqualizerSampleProvider.BandCount];
        private bool eqEnabled = true;
        private string eqPreset = "Плоский";
        private bool eqPanelOpen;
        private ContextMenuStrip eqPresetMenu;

        private sealed class Track
        {
            public string FilePath { get; }
            public string Name { get; }

            public Track(string filePath)
            {
                FilePath = filePath;
                Name = Path.GetFileNameWithoutExtension(filePath);
            }

            public override string ToString() => Name;
        }

        private sealed class PlaylistData
        {
            public string Name;
            public string FilePath;
            public readonly List<Track> Tracks = new();
            public readonly HashSet<string> Files = new(StringComparer.OrdinalIgnoreCase);

            public bool Add(string path)
            {
                if (!Files.Add(path)) return false;
                Tracks.Add(new Track(path));
                return true;
            }
        }

        public Form1()
        {
            InitializeComponent();

            BuildTitleBar();
            BuildPlaylistTabs();     // до originalSize: вікно стає вищим на висоту вкладок
            BuildOnlineButton();

            originalSize = Size;

            fadeTimer.Tick += FadeTimer_Tick;

            // DragEnter / DragDrop списку та DoubleClick форми й обкладинки
            // уже підписані в Form1.Designer.cs, тут їх повторно не підписуємо.
            NameTrack.DoubleClick += CollapsedArea_DoubleClick;
            TimerLabel.DoubleClick += CollapsedArea_DoubleClick;

            // Темне контекстне меню (Visual Studio не зберігає цей рядок у Designer.cs)
            contextMenuStripListPlayer.Renderer = new DarkMenuRenderer();

            SetShuffleMode(false);

            BuildSpeedButton();
            BuildSearchBox();

            BuildListExtras();


            progressBarMusic.Minimum = 0;

            LoadCustomPresets();
            BuildEqualizerUi();


            LayoutEqPanel();
            BuildAudioEnhancementsUi();
            InitializeVolumeControl();
            eqPresetButton.ShowDropArrow = true;
            LoadEqualizerSettings();
            LoadAudioEnhancementSettings();

            toast = new TrackToast();
            LoadToastSetting();
        }


        // Завантажує зображення з файлу, не залишаючи його відкритим (файл не блокується).
        private static Image LoadImageWithoutLocking(string path)
        {
            if (!File.Exists(path))
                return null;

            using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read);

            using var image = Image.FromStream(stream);

            return new Bitmap(image);
        }


        // Метод створення власної панелі заголовка
        private void BuildTitleBar()
        {


            // Прибираємо стандартну рамку Windows
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;

            // Збільшуємо робочу область на висоту заголовка
            foreach (Control control in Controls)
            {
                control.Top += TitleBarHeight;
            }

            ClientSize = new Size(
                ClientSize.Width,
                ClientSize.Height + TitleBarHeight);

            // Коригуємо обмеження розміру вікна
            MinimumSize = new Size(
                MinimumSize.Width,
                MinimumSize.Height + TitleBarHeight);

            // Після рядків з MinimumSize / MaximumSize + TitleBarHeight:
            MaximumSize = new Size(
                MaximumSize.Width > 0 ? MaximumSize.Width : 0,
                Math.Max(MaximumSize.Height, 2000)); // можна тягнути вниз

            titleBar = new Panel
            {
                Name = "titleBar",
                Location = Point.Empty,
                Size = new Size(ClientSize.Width, TitleBarHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(20, 21, 26)
            };

            PictureBox titleIcon = new PictureBox
            {
                Size = new Size(20, 20),
                Location = new Point(9, 5),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = LoadImageWithoutLocking(
                    Path.Combine(AppContext.BaseDirectory, "Image", "logo.png"))
            };



            titleLabel = new Label
            {
                Text = "AUREON",
                ForeColor = Color.FromArgb(235, 238, 245),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(34, 0),
                Size = new Size(150, TitleBarHeight)
            };

            minimizeButton = CreateTitleButton("—");
            minimizeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            minimizeButton.Location = new Point(ClientSize.Width - 88, 0);

            closeButton = CreateTitleButton("×");
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.Location = new Point(ClientSize.Width - 44, 0);

            minimizeButton.Click += (s, e) =>
            {
                if (isCustomSize || IsOnlineActive)
                    MinimizeKeepingAudio();
                else
                    CollapseToCorner();
            };

            closeButton.Click += (s, e) => Close();

            titleBar.MouseDown += TitleBar_MouseDown;
            titleLabel.MouseDown += TitleBar_MouseDown;

            titleBar.DoubleClick += TitleBar_DoubleClick;
            titleLabel.DoubleClick += TitleBar_DoubleClick;

            titleBar.Controls.Add(titleIcon);
            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(minimizeButton);
            titleBar.Controls.Add(closeButton);

            Controls.Add(titleBar);
            titleBar.BringToFront();
        }

        private Button CreateTitleButton(string text)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(210, 214, 222),
                BackColor = Color.FromArgb(20, 21, 26),
                Font = new Font("Segoe UI", 10F),
                Size = new Size(44, TitleBarHeight),
                TabStop = false,
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(48, 51, 60);
            button.FlatAppearance.MouseDownBackColor =
                Color.FromArgb(65, 69, 80);

            return button;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            ReleaseCapture();

            SendMessage(
                Handle,
                WM_NCLBUTTONDOWN,
                (IntPtr)HTCAPTION,
                IntPtr.Zero);
        }

        private void TitleBar_DoubleClick(object sender, EventArgs e)
        {
            if (isCustomSize)
                RestoreWindow();
        }


        // ==================== Завантаження ====================
        private void Form1_Load(object sender, EventArgs e)
        {
            SetupToolTips();
            LoadRatings();
            LoadPlaylists();
            ShowPlaylist(viewList);

            SetCover(LoadDefaultCover());

            if (viewList.Tracks.Count > 0)
                SelectAndPlay(0);

            _lastClientHeight = ClientSize.Height;
            _listPlayerBaseTop = ListPlayer.Top;

            // Принудительно выравниваем плейлист и кнопки при старте
            SyncPlaylistToFormHeight();

            ClampFormToScreen();
        }


        private void SetupToolTips()
        {
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;

            toolTip.SetToolTip(BackButton, "Попередній трек");
            toolTip.SetToolTip(PauseButton, "Пауза / Відтворення");
            toolTip.SetToolTip(ForwardButton, "Наступний трек");
            toolTip.SetToolTip(StopButton, "Стоп");
            toolTip.SetToolTip(VolumeLabel, "Повзунок гучності");
            toolTip.SetToolTip(progressBarMusic, "Прогрес-бар треку");
            toolTip.SetToolTip(ListPlayer, "Плейлист");
            toolTip.SetToolTip(TimerLabel, "Час відтворення (подвійний клік — розгорнути)");
            toolTip.SetToolTip(noRepeatTrack, "Не повторювати трек");
            toolTip.SetToolTip(AddPlayList, "Додати треки до плейлиста");
            toolTip.SetToolTip(DeletePlayList, "Видалити плейлист");
            toolTip.SetToolTip(pictureCover, "Обкладинка (подвійний клік — згорнути)");
            toolTip.SetToolTip(repeatTrack, "Повторювати трек");
            toolTip.SetToolTip(NameTrack, "Назва треку (подвійний клік — розгорнути)");
            toolTip.SetToolTip(AllTracksTime, "Загальний час плейлиста");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SaveEqualizerSettings();
            SaveAudioEnhancementSettings();
            SaveTabs();
            StopMusic();
            toast?.Dispose();
            webView?.Dispose();
            base.OnFormClosed(e);
        }

        // ==================== Вкладки плейлистів: побудова ====================
        private void BuildPlaylistTabs()
        {
            int h = (int)Math.Round(28 * DeviceDpi / 96.0);
            int gap = (int)Math.Round(4 * DeviceDpi / 96.0);
            int delta = h + gap;

            // Вкладки стають там, де був список; список і нижні кнопки зсуваються вниз
            int top = ListPlayer.Top;

            Control[] below =
            {
                ListPlayer,
                repeatTrack, noRepeatTrack, yesshuffleTrack, nosshuffleTrack,
                AddPlayList, DeletePlayList
            };

            foreach (Control c in below)
                c.Top += delta;

            // Спочатку піднімаємо верхню межу, щоб вікно не обрізалось
            MaximumSize = new Size(MaximumSize.Width, MaximumSize.Height + delta);
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + delta);
            MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height + delta);

            playlistTabs = new PlaylistTabs
            {
                Name = "playlistTabs",
                Location = new Point(8, top),
                Size = new Size(ClientSize.Width - 16, h)
            };

            playlistTabs.TabSelected += i =>
            {
                if (i >= 0 && i < playlists.Count)
                    ShowPlaylist(playlists[i]);
            };
            playlistTabs.AddRequested += AddFolderPlaylistFromDialog;
            playlistTabs.TabContextRequested += PlaylistTabs_ContextRequested;
            playlistTabs.FilesDropped += PlaylistTabs_FilesDropped;

            Controls.Add(playlistTabs);
            toolTip.SetToolTip(playlistTabs,
                "Плейлисти: «+» або перетягніть папку, правий клік — меню, Ctrl+Tab — перемкнути");

            BuildTabMenu();
        }

        private void BuildTabMenu()
        {
            tabMenu = new ContextMenuStrip
            {
                Renderer = new DarkMenuRenderer(),
                Font = new Font("Segoe UI", 9.5F),
                ShowImageMargin = false
            };

            tabMenu.Items.Add("Додати папку як плейлист…", null, (s, e) => AddFolderPlaylistFromDialog());
            tabMenu.Items.Add("Новий порожній плейлист", null, (s, e) => CreateEmptyPlaylist());
            tabMenu.Items.Add(new ToolStripSeparator());
            miRename = tabMenu.Items.Add("Перейменувати…", null, (s, e) => RenameTab(tabMenuIndex));
            miClear = tabMenu.Items.Add("Очистити", null, (s, e) =>
            {
                if (tabMenuIndex >= 0 && tabMenuIndex < playlists.Count)
                    ClearList(playlists[tabMenuIndex]);
            });
            miClose = tabMenu.Items.Add("Закрити вкладку", null, (s, e) => CloseTab(tabMenuIndex));
        }

        private void PlaylistTabs_ContextRequested(int index, Point screen)
        {
            tabMenuIndex = index;
            bool has = index >= 0 && index < playlists.Count;

            miRename.Enabled = has;
            miClear.Enabled = has;
            miClose.Enabled = has && playlists.Count > 1;

            tabMenu.Show(screen);
        }

        // Папки/файли, скинуті на смугу вкладок
        private void PlaylistTabs_FilesDropped(string[] paths)
        {
            // Відкладаємо, щоб не блокувати Провідник усередині drag-and-drop
            BeginInvoke(new Action(() => HandleTabsDrop(paths)));
        }

        private void HandleTabsDrop(string[] paths)
        {
            PlaylistData first = null;
            var loose = new List<string>();

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    var p = AddFolderAsPlaylists(path);
                    first ??= p;
                }
                else
                {
                    loose.Add(path);
                }
            }

            if (loose.Count > 0)
            {
                AddFilesToViewList(loose);
                SavePlaylist(viewList);
            }

            SaveTabs();

            if (first != null)
            {
                ShowPlaylist(first);
                if (waveOutDevice == null && first.Tracks.Count > 0)
                    SelectAndPlay(0);
            }
            else
            {
                RefreshTabs();
            }
        }

        // ==================== Плейлисти: завантаження і збереження ====================
        private void LoadPlaylists()
        {
            playlists.Clear();
            int active = 0;

            try
            {
                Directory.CreateDirectory(PlaylistDir);

                if (File.Exists(TabsFilePath))
                {
                    foreach (string raw in File.ReadLines(TabsFilePath))
                    {
                        string line = raw.Trim();

                        if (line.StartsWith("Active=", StringComparison.Ordinal))
                        {
                            int.TryParse(line.Substring(7), out active);
                        }
                        else if (line.StartsWith("Tab=", StringComparison.Ordinal))
                        {
                            string v = line.Substring(4);
                            int bar = v.IndexOf('|');
                            if (bar <= 0) continue;

                            string file = Path.GetFileName(v.Substring(0, bar));
                            string name = v.Substring(bar + 1).Trim();
                            if (name.Length == 0) name = Path.GetFileNameWithoutExtension(file);

                            playlists.Add(LoadPlaylistFile(Path.Combine(PlaylistDir, file), name));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження вкладок: {ex.Message}");
            }

            // Перший запуск або старий формат: один список стає вкладкою «Default»
            if (playlists.Count == 0)
                playlists.Add(LoadPlaylistFile(PlaylistFilePath, "За замовчуванням"));

            viewList = playlists[Math.Clamp(active, 0, playlists.Count - 1)];
        }

        private PlaylistData LoadPlaylistFile(string path, string name)
        {
            var p = new PlaylistData { Name = name, FilePath = path };

            try
            {
                if (File.Exists(path))
                {
                    foreach (string line in File.ReadLines(path))
                    {
                        string track = line.Trim();
                        if (track.Length == 0 || track.StartsWith("#") || !File.Exists(track))
                            continue;

                        p.Add(track);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження треків: {ex.Message}");
            }

            return p;
        }

        private void SavePlaylist(PlaylistData p)
        {
            if (p == null) return;

            try
            {
                Directory.CreateDirectory(PlaylistDir);
                File.WriteAllLines(p.FilePath,
                    new[] { "#EXTM3U" }.Concat(p.Tracks.Select(t => t.FilePath)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження плейлиста: {ex.Message}");
            }
        }

        private void SaveTabs()
        {
            if (playlists.Count == 0) return;

            try
            {
                Directory.CreateDirectory(PlaylistDir);

                var lines = new List<string> { $"Active={Math.Max(0, playlists.IndexOf(viewList))}" };
                foreach (var p in playlists)
                    lines.Add($"Tab={Path.GetFileName(p.FilePath)}|{p.Name}");

                File.WriteAllLines(TabsFilePath, lines);
            }
            catch
            {
                // не критично
            }
        }

        private string UniquePlaylistPath(string name)
        {
            string safe = string.Concat(name.Select(c =>
                Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c)).Trim();
            if (safe.Length == 0) safe = "playlist";

            string path = Path.Combine(PlaylistDir, safe + ".m3u8");
            int n = 2;

            while (playlists.Any(x => string.Equals(x.FilePath, path, StringComparison.OrdinalIgnoreCase))
                   || File.Exists(path))
            {
                path = Path.Combine(PlaylistDir, $"{safe} ({n++}).m3u8");
            }

            return path;
        }

        private void RefreshTabs()
        {
            if (playlistTabs == null) return;
            playlistTabs.SetTabs(playlists.Select(p => p.Name).ToList(), playlists.IndexOf(viewList));
        }

        // Показує плейлист у списку. Музика, що грає, не переривається.
        private void ShowPlaylist(PlaylistData p)
        {
            if (p == null) return;

            viewList = p;
            searchBox?.Clear();

            isChangingTrack = true;
            ListPlayer.BeginUpdate();
            try
            {
                ListPlayer.Items.Clear();
                ListPlayer.Items.AddRange(p.Tracks.Cast<object>().ToArray());

                if (p == playList && playingTrack != null)
                {
                    int i = p.Tracks.IndexOf(playingTrack);
                    if (i >= 0) ListPlayer.SelectedIndex = i;
                }
            }
            finally
            {
                ListPlayer.EndUpdate();
                isChangingTrack = false;
            }

            RefreshTabs();
            UpdateAllTracksTimeLabel();
            SaveTabs();
        }

        private void CycleTab(int step)
        {
            int n = playlists.Count;
            if (n < 2) return;

            int idx = Math.Max(0, playlists.IndexOf(viewList));
            ShowPlaylist(playlists[(idx + step + n) % n]);
        }

        // ==================== Вкладки: дії ====================
        private PlaylistData NewPlaylistFromPaths(string name, IEnumerable<string> paths)
        {
            var p = new PlaylistData { Name = name };
            AddFilesCore(p, paths);

            if (p.Tracks.Count == 0) return null;

            p.FilePath = UniquePlaylistPath(name);
            playlists.Add(p);
            SavePlaylist(p);
            return p;
        }

        // Створює вкладку (або вкладки) з папки. Повертає першу створену.
        private PlaylistData AddFolderAsPlaylists(string folder)
        {
            PlaylistData first = null;

            string baseName = new DirectoryInfo(folder).Name;
            if (string.IsNullOrWhiteSpace(baseName)) baseName = folder;

            if (SubfoldersAsTabs)
            {
                string[] direct = AudioFilesIn(folder, SearchOption.TopDirectoryOnly);
                if (direct.Length > 0)
                    first ??= NewPlaylistFromPaths(baseName, direct);

                string[] subs;
                try
                {
                    subs = Directory.GetDirectories(folder);
                    Array.Sort(subs, StringComparer.CurrentCultureIgnoreCase);
                }
                catch
                {
                    subs = Array.Empty<string>();
                }

                foreach (string sub in subs)
                {
                    var p = NewPlaylistFromPaths(new DirectoryInfo(sub).Name, new[] { sub });
                    first ??= p;
                }
            }
            else
            {
                first = NewPlaylistFromPaths(baseName, new[] { folder });
            }

            if (first == null)
                MessageBox.Show("У цій папці немає підтримуваних аудіофайлів.");

            return first;
        }

        private void AddFolderPlaylistFromDialog()
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Оберіть папку з музикою",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var first = AddFolderAsPlaylists(dlg.SelectedPath);
            if (first == null) return;

            SaveTabs();
            ShowPlaylist(first);

            if (waveOutDevice == null && first.Tracks.Count > 0)
                SelectAndPlay(0);
        }

        private void CreateEmptyPlaylist()
        {
            string name = AskText("Новий плейлист", "Новий плейлист");
            if (string.IsNullOrWhiteSpace(name)) return;

            var p = new PlaylistData { Name = name, FilePath = UniquePlaylistPath(name) };
            playlists.Add(p);
            SavePlaylist(p);
            ShowPlaylist(p);
        }

        private void RenameTab(int index)
        {
            if (index < 0 || index >= playlists.Count) return;

            var p = playlists[index];
            string name = AskText("Перейменувати плейлист", p.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            p.Name = name;
            RefreshTabs();
            SaveTabs();
        }

        private void CloseTab(int index)
        {
            if (index < 0 || index >= playlists.Count || playlists.Count <= 1) return;

            var p = playlists[index];
            if (p == playList) ResetPlayback();

            playlists.RemoveAt(index);

            try { if (File.Exists(p.FilePath)) File.Delete(p.FilePath); }
            catch { }

            if (p == viewList)
            {
                ShowPlaylist(playlists[Math.Min(index, playlists.Count - 1)]);
            }
            else
            {
                RefreshTabs();
                SaveTabs();
            }
        }

        // Маленьке темне вікно для введення назви
        private string AskText(string title, string initial)
        {
            using var dlg = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(300, 100),
                BackColor = Theme.Bg,
                ForeColor = Theme.Text
            };

            var tb = new TextBox
            {
                Left = 12,
                Top = 14,
                Width = 276,
                Text = initial ?? "",
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.FixedSingle
            };

            var ok = new RoundButton
            {
                Text = "OK",
                Kind = ButtonKind.Accent,
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = Color.White,
                Radius = 8,
                Size = new Size(84, 28),
                Location = new Point(124, 58)
            };

            var cancel = new RoundButton
            {
                Text = "Скасувати",
                Kind = ButtonKind.Soft,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.Text,
                Radius = 8,
                Size = new Size(84, 28),
                Location = new Point(212, 58)
            };

            ok.Click += (s, e) => dlg.DialogResult = DialogResult.OK;
            cancel.Click += (s, e) => dlg.DialogResult = DialogResult.Cancel;

            tb.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    dlg.DialogResult = DialogResult.OK;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    dlg.DialogResult = DialogResult.Cancel;
                }
            };

            dlg.HandleCreated += (s, e) =>
            {
                int on = 1;
                DwmSetWindowAttribute(dlg.Handle, 20, ref on, sizeof(int));   // темний заголовок
            };
            dlg.Shown += (s, e) => { tb.SelectAll(); tb.Focus(); };

            dlg.Controls.AddRange(new Control[] { tb, ok, cancel });

            return dlg.ShowDialog(this) == DialogResult.OK ? tb.Text.Trim() : null;
        }

        // ==================== Додавання аудіофайлів ====================
        // Формати, які підтримує поточний ланцюжок відтворення NAudio.
        private static readonly HashSet<string> SupportedAudioExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".wav", ".aiff", ".aif", ".wma",
                ".m4a", ".aac", ".mp4", ".flac"
            };

        private static bool IsSupportedAudioFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   SupportedAudioExtensions.Contains(Path.GetExtension(path));
        }

        private static string[] AudioFilesIn(string dir, SearchOption option)
        {
            try
            {
                string[] files = Directory.GetFiles(dir, "*.*", option)
                    .Where(IsSupportedAudioFile)
                    .ToArray();

                Array.Sort(files, StringComparer.CurrentCultureIgnoreCase);
                return files;
            }
            catch (Exception ex) when (
                ex is UnauthorizedAccessException || ex is IOException)
            {
                return Array.Empty<string>();
            }
        }

        private void AddFilesCore(PlaylistData p, IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    AddFilesCore(p, AudioFilesIn(path, SearchOption.AllDirectories));
                }
                else if (File.Exists(path) && IsSupportedAudioFile(path))
                {
                    p.Add(Path.GetFullPath(path));
                }
            }
        }

        // Додає треки до відкритої вкладки і показує їх у списку
        private void AddFilesToViewList(IEnumerable<string> paths)
        {
            var p = viewList;
            int before = p.Tracks.Count;

            AddFilesCore(p, paths);

            ListPlayer.BeginUpdate();
            try
            {
                for (int i = before; i < p.Tracks.Count; i++)
                    ListPlayer.Items.Add(p.Tracks[i]);
            }
            finally
            {
                ListPlayer.EndUpdate();
            }

            UpdateAllTracksTimeLabel();
        }

        private void AddTracksFromDialog()
        {
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            AddFilesToViewList(openFileDialog.FileNames);
            if (viewList.Tracks.Count > 0 && waveOutDevice == null)
                SelectAndPlay(0);
            SavePlaylist(viewList);
        }

        // Зупиняє музику і скидає «що грає»
        private void ResetPlayback()
        {
            StopMusic();
            playList = null;
            playingTrack = null;
            NameTrack.Text = "Немає треку!";

            _coverVersion++; // скасовуємо пошук обкладинки, що міг ще виконуватися
            SetCover(LoadDefaultCover());
        }

        private void ClearList(PlaylistData p)
        {
            if (p == null) return;

            if (p == playList) ResetPlayback();

            p.Tracks.Clear();
            p.Files.Clear();

            if (p == viewList)
            {
                ListPlayer.Items.Clear();
                UpdateAllTracksTimeLabel();
            }

            SavePlaylist(p);
        }

        private void ClearPlaylist() => ClearList(viewList);

        private void добавитьПлейлистToolStripMenuItem_Click(object sender, EventArgs e) => AddTracksFromDialog();
        private void очиститьПлейлистToolStripMenuItem_Click(object sender, EventArgs e) => ClearPlaylist();
        private void AddPlayList_Click(object sender, EventArgs e) => AddTracksFromDialog();
        private void DeletePlayList_Click(object sender, EventArgs e) => ClearPlaylist();

        // ==================== Вибір і перемикання треків ====================

        // Запускає трек із відкритої вкладки
        private void SelectAndPlay(int index) => PlayAt(viewList, index);

        // Запускає трек зі списку (вкладка може бути не відкрита)
        private void PlayAt(PlaylistData list, int index)
        {
            if (list == null || index < 0 || index >= list.Tracks.Count) return;

            playList = list;
            Track track = list.Tracks[index];

            if (list == viewList)
            {
                if (ListPlayer.SelectedIndex == index)
                    PlayTrack(track);
                else
                    ListPlayer.SelectedIndex = index;   // обробник сам запустить трек
            }
            else
            {
                PlayTrack(track);
            }
        }

        private void MoveSelection(int step)
        {
            var list = playList ?? viewList;
            if (list == null) return;

            int count = list.Tracks.Count;
            if (count == 0) return;

            int index = playingTrack == null ? -1 : list.Tracks.IndexOf(playingTrack);
            int next;

            if (step > 0 && isShuffle && count > 1)
            {
                do { next = random.Next(count); }
                while (next == index);
            }
            else
            {
                next = index < 0
                    ? (step > 0 ? 0 : count - 1)
                    : (index + step + count) % count;
            }

            PlayAt(list, next);
        }

        // Запускає поточний трек, якщо музика не грає
        private bool StartCurrent()
        {
            if (playingTrack != null && playList != null)
            {
                PlayTrack(playingTrack);
                return true;
            }

            if (ListPlayer.SelectedItem is Track selected)
            {
                playList = viewList;
                PlayTrack(selected);
                return true;
            }

            return false;
        }

        private void ListPlayer_MouseClick(object sender, MouseEventArgs e) { }

        private void ListPlayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isChangingTrack) return;

            if (ListPlayer.SelectedItem is Track track)
            {
                isChangingTrack = true;
                try
                {
                    playList = viewList;
                    PlayTrack(track);
                }
                finally { isChangingTrack = false; }
            }
        }

        // ==================== Відтворення ====================
        // ModernSlider stores real percentage values (0..100/125/150).
        // MasterVolumeSampleProvider applies gain after the spectrum tap.
        private float CurrentVolume()
        {
            // Volume.Maximum уже = volumeLimit (100/125/150)
            float v = Volume.Value / (float)Math.Max(1, Volume.Maximum);
            return Math.Clamp(v, 0f, 1.5f);
        }

        private float GetFadeFactor()
        {
            if (audioFileReader == null || FadeDurationSeconds <= 0)
                return 1f;

            double total = audioFileReader.TotalTime.TotalSeconds;
            if (total <= 0.05)
                return 1f;

            // Fade не довший за 15% треку і не довший за FadeDurationSeconds
            double fade = Math.Min(FadeDurationSeconds, total * 0.15);
            // На дуже коротких треках майже без fade
            if (total < 4.0)
                fade = Math.Min(fade, total * 0.08);
            if (fade < 0.05)
                return 1f;

            double elapsed = audioFileReader.CurrentTime.TotalSeconds;
            double remaining = total - elapsed;

            // Плавна S-крива (cosine), без лінійних «сходинок»
            static float Smooth(double t)
            {
                t = Math.Clamp(t, 0.0, 1.0);
                return (float)(0.5 - 0.5 * Math.Cos(t * Math.PI));
            }

            float fadeIn = Smooth(elapsed / fade);
            float fadeOut = Smooth(remaining / fade);
            float factor = Math.Min(fadeIn, fadeOut);

            // Не глушимо повністю в нуль — менше клацань на старті/кінці
            return Math.Max(0.0008f, factor);
        }

        private void PlayTrack(Track track)
        {
            try
            {
                StopMusic();
                playingTrack = track;

                audioFileReader = new AudioFileReader(track.FilePath);
                var reader = audioFileReader;

                // Ланцюжок: файл -> швидкість -> 10-смуговий EQ -> Bass/Virtualizer/Sub-bass
                // -> баланс -> візуалізатор -> майстер-гучність.
                // Майстер-гучність стоїть ПІСЛЯ візуалізатора, тому його показники
                // не змінюються при переміщенні повзунка 0–150%.

                // Обробка швидкості без зміни висоти тону
                var speedInput = new WaveChannel32(reader)
                {
                    PadWithZeroes = false
                };

                soundTouchStream = new SoundTouchWaveStream(speedInput)
                {
                    Tempo = speed
                };

                ISampleProvider source = new WaveToSampleProvider(soundTouchStream);

                // Моно перетворюємо на стерео для коректної роботи балансу
                if (reader.WaveFormat.Channels == 1)
                    source = new MonoToStereoSampleProvider(source);

                equalizer = new EqualizerSampleProvider(source);
                equalizer.SetAllGains(eqGains, immediate: true);
                equalizer.Enabled = eqEnabled;

                // Додаткові ефекти йдуть після 10-смугового EQ,
                // тому стандартні пресети та ручні смуги не змінюються.
                audioEffects = new AudioEffectsSampleProvider(equalizer)
                {
                    BassBoostDb = bassBoostDb,
                    Virtualizer = virtualizerPercent,
                    SubBassDb = subBassDb,
                    Enabled = eqEnabled
                };

                balanceProvider = new BalanceSampleProvider(audioEffects)
                {
                    Balance = balance
                };

                // Візуалізатор отримує сигнал до майстер-гучності,
                // тому його амплітуда не залежить від положення Volume.
                spectrum = new SpectrumSampleProvider(balanceProvider);
                masterVolume = new MasterVolumeSampleProvider(spectrum)
                {
                    Volume = 0.0008f   // не 0f
                };

                waveOutDevice = new WasapiOut();
                waveOutDevice.PlaybackStopped += WaveOutDevice_PlaybackStopped;
                waveOutDevice.Init(new SampleToWaveProvider(masterVolume));
                waveOutDevice.Play();
                spectrumView.Source = spectrum;
                fadeTimer.Start();

                progressBarMusic.Maximum = Math.Max(1, (int)audioFileReader.TotalTime.TotalSeconds);
                progressBarMusic.Value = 0;
                TimerMusicComponent.Start();
                PauseButton.Text = PauseGlyph;
                NameTrack.Text = track.Name;
                LoadCoverArt(track.FilePath);
            }
            catch (Exception ex)
            {
                StopMusic();
                MessageBox.Show($"Помилка відтворення файлу {track.Name}: {ex.Message}");
            }
        }

        private void StopMusic()
        {
            TimerMusicComponent.Stop();
            fadeTimer.Stop();

            spectrumView.Source = null;
            spectrum = null;
            equalizer = null;
            audioEffects = null;
            balanceProvider = null;
            masterVolume = null;

            if (waveOutDevice != null)
            {
                waveOutDevice.PlaybackStopped -= WaveOutDevice_PlaybackStopped;
                waveOutDevice.Stop();
                waveOutDevice.Dispose();
                waveOutDevice = null;
            }

            if (soundTouchStream != null)
            {
                soundTouchStream.Dispose();
                soundTouchStream = null;
                audioFileReader = null;
            }
            else
            {
                audioFileReader?.Dispose();
                audioFileReader = null;
            }

            progressBarMusic.Value = 0;
            TimerLabel.Text = "00:00:00";
            PauseButton.Text = PlayGlyph;
        }

        private void WaveOutDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (!ReferenceEquals(sender, waveOutDevice) || audioFileReader == null)
                return;
            if (e.Exception != null) return;
            if (audioFileReader.TotalTime - audioFileReader.CurrentTime > TimeSpan.FromSeconds(1))
                return;

            var list = playList;
            if (list == null || list.Tracks.Count == 0) return;

            if (isRepeating && playingTrack != null)
            {
                PlayTrack(playingTrack);
                return;
            }

            int count = list.Tracks.Count;
            int current = playingTrack == null ? -1 : list.Tracks.IndexOf(playingTrack);

            if (isShuffle && count > 1)
            {
                int next;
                do { next = random.Next(count); }
                while (next == current);
                PlayAt(list, next);
                return;
            }

            PlayAt(list, (current + 1) % count);
        }

        // ==================== Кнопки ====================
        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (waveOutDevice == null)
            {
                StartCurrent();
                return;
            }

            if (waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                waveOutDevice.Pause();
                PauseButton.Text = PlayGlyph;
            }
            else
            {
                waveOutDevice.Play();
                PauseButton.Text = PauseGlyph;
            }
        }

        private void StopButton_Click(object sender, EventArgs e) => StopMusic();
        private void ForwardButton_Click(object sender, EventArgs e) => MoveSelection(+1);
        private void BackButton_Click(object sender, EventArgs e) => MoveSelection(-1);

        private void repeatTrack_Click(object sender, EventArgs e)
        {
            SetShuffleMode(false);
            isRepeating = true;
            repeatTrack.Visible = false;
            noRepeatTrack.Visible = true;
        }

        private void noRepeatTrack_Click(object sender, EventArgs e)
        {
            isRepeating = false;
            noRepeatTrack.Visible = false;
            repeatTrack.Visible = true;
        }

        // ==================== Гучність і прогрес ====================
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (masterVolume != null)
                masterVolume.Volume = CurrentVolume() * GetFadeFactor();

            UpdateVolumeToolTip();
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            if (masterVolume == null) return;
            masterVolume.Volume = CurrentVolume() * GetFadeFactor();
        }

        private void TimerMusicComponent_Tick(object sender, EventArgs e)
        {
            if (audioFileReader == null || isDragging) return;
            TimeSpan current = audioFileReader.CurrentTime;
            progressBarMusic.Value = (int)current.TotalSeconds;
            TimerLabel.Text = current.ToString(@"hh\:mm\:ss");
        }

        private void progressBarMusic_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;
            SeekOrStart();
            ShowSliderTime();
        }

        private void progressBarMusic_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            SeekOrStart();
        }

        private void progressBarMusic_Scroll(object sender, EventArgs e)
        {
            if (isDragging) ShowSliderTime();
        }

        private void ShowSliderTime() =>
            TimerLabel.Text = TimeSpan.FromSeconds(progressBarMusic.Value).ToString(@"hh\:mm\:ss");

        private void SeekOrStart()
        {
            int target = progressBarMusic.Value;
            bool stopped = waveOutDevice == null ||
                           waveOutDevice.PlaybackState == PlaybackState.Stopped;

            if (stopped && StartCurrent())
            {
                progressBarMusic.Value = target;
            }

            if (audioFileReader == null) return;

            TimeSpan position = TimeSpan.FromSeconds(progressBarMusic.Value);
            if (position > audioFileReader.TotalTime)
                position = audioFileReader.TotalTime;
            audioFileReader.CurrentTime = position;
            soundTouchStream?.Flush();
        }

        // ==================== Еквалайзер ====================
        private void BuildEqualizerUi()
        {
            eqPresetMenu = new ContextMenuStrip
            {
                Renderer = new DarkMenuRenderer(),
                Font = new Font("Segoe UI", 9.5F),
                ShowImageMargin = false
            };

            RebuildPresetMenu();
        }

        private void LoadEqualizerSettings()
        {
            try
            {
                if (File.Exists(EqSettingsPath))
                {
                    foreach (string line in File.ReadLines(EqSettingsPath))
                    {
                        int p = line.IndexOf('=');
                        if (p <= 0) continue;

                        string key = line.Substring(0, p).Trim();
                        string value = line.Substring(p + 1).Trim();

                        switch (key)
                        {
                            case "Enabled":
                                eqEnabled = value != "0";
                                break;

                            case "Speed":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float sp))
                                    speed = Math.Clamp(sp, 0.25f, 4f);
                                break;

                            case "Balance":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float bal))
                                    balance = Math.Clamp(bal, -1f, 1f);
                                break;

                            case "Preset":
                                if (value.Length > 0) eqPreset = value;
                                break;

                            case "Gains":
                                {
                                    string[] parts = value.Split(';');
                                    for (int i = 0; i < eqGains.Length && i < parts.Length; i++)
                                    {
                                        if (float.TryParse(parts[i], NumberStyles.Float,
                                                CultureInfo.InvariantCulture, out float v))
                                        {
                                            eqGains[i] = Math.Clamp(v,
                                                -EqualizerSampleProvider.MaxGainDb,
                                                EqualizerSampleProvider.MaxGainDb);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
            catch
            {
                // пошкоджений файл: працюємо зі значеннями за замовчуванням
            }

            balanceSlider.Value = (int)Math.Round(balance * 100);
            UpdateSpeedUi();

            equalizerView.SetGains(eqGains);
            UpdateEqToggleUi();
            UpdateEqPresetUi();
        }

        private void SaveEqualizerSettings()
        {
            try
            {
                string gains = string.Join(";",
                    eqGains.Select(g => g.ToString("0.0", CultureInfo.InvariantCulture)));

                Directory.CreateDirectory(Path.GetDirectoryName(EqSettingsPath));
                File.WriteAllLines(EqSettingsPath, new[]
                {
                    $"Enabled={(eqEnabled ? 1 : 0)}",
                    $"Preset={eqPreset}",
                    $"Gains={gains}",
                    $"Balance={balance.ToString("0.00", CultureInfo.InvariantCulture)}",
                    $"Speed={speed.ToString("0.00", CultureInfo.InvariantCulture)}"
                });
            }
            catch
            {
                // не вдалося записати: не критично
            }
        }

        private void UpdateEqToggleUi()
        {
            eqToggleButton.Text = eqEnabled ? "Увімкнути" : "Вимкнути";
            eqToggleButton.Kind = eqEnabled ? ButtonKind.Accent : ButtonKind.Soft;
            eqToggleButton.ForeColor = eqEnabled ? Color.White : Color.FromArgb(200, 200, 208);
            equalizerView.Active = eqEnabled;
            UpdateAudioEnhancementsEnabled();

            // Кнопка EQ підсвічується, поки панель відкрита
            eqButton.ForeColor = eqPanelOpen
                ? Color.FromArgb(76, 160, 255)
                : Color.FromArgb(235, 235, 238);
        }

        private void UpdateEqPresetUi() =>
            eqPresetButton.Text = "≡  " + eqPreset;

        private void ApplyPreset(string name)
        {
            foreach (var preset in EqPresets.Concat(customPresets))
            {
                if (preset.Name != name) continue;

                Array.Copy(preset.Gains, eqGains, eqGains.Length);
                eqPreset = name;

                equalizerView.SetGains(eqGains);
                equalizer?.SetAllGains(eqGains, immediate: false);

                UpdateEqPresetUi();
                SaveEqualizerSettings();
                return;
            }
        }

        // Показує / ховає панель: список і нижні кнопки зсуваються, вікно змінює висоту
        private void SetEqPanelOpen(bool open)
        {
            if (open == eqPanelOpen) return;

            int delta = eqPanel.Height + EqGap;
            eqPanelOpen = open;

            Rectangle area = Screen.FromControl(this).WorkingArea;
            int maxH = area.Height;

            if (open)
            {
                // 1. ЗАПОМИНАЕМ текущую высоту окна (когда EQ был закрыт)
                _heightBeforeEq = Height;

                // 2. Пытаемся увеличить окно на высоту EQ
                int targetH = Math.Min(Height + delta, maxH);
                int applied = targetH - Height;

                MaximumSize = new Size(MaximumSize.Width, maxH);

                // Временно снимаем ограничение MinimumSize, чтобы окно могло расти
                int oldMinHeight = MinimumSize.Height;
                MinimumSize = new Size(MinimumSize.Width, 0);

                Height = targetH;

                // 3. Возвращаем MinimumSize, но уже с учетом высоты EQ
                MinimumSize = new Size(MinimumSize.Width, oldMinHeight + delta);

                // 4. Позиционируем EQ и плейлист
                int shortfall = delta - applied;
                if (shortfall > 0)
                {
                    ListPlayer.Top += shortfall;
                    eqPanel.Top = ListPlayer.Top - EqGap - eqPanel.Height;
                }
                else
                {
                    eqPanel.Top = playlistTabs.Bottom + EqGap;
                    ListPlayer.Top = eqPanel.Bottom + EqGap;
                }
            }
            else
            {
                // Закрываем EQ
                ListPlayer.Top = playlistTabs.Bottom + EqGap;

                // Временно снимаем ограничение, чтобы окно могло сжаться
                int currentMinH = MinimumSize.Height;
                MinimumSize = new Size(MinimumSize.Width, 0);

                // Восстанавливаем высоту, которая была до открытия EQ
                Height = _heightBeforeEq;

                // Возвращаем MinimumSize к исходному значению (без учета EQ)
                MinimumSize = new Size(MinimumSize.Width, Math.Max(100, currentMinH - delta));

                MaximumSize = new Size(MaximumSize.Width, maxH);
            }

            eqPanel.Visible = open;
            UpdateEqToggleUi();

            SyncPlaylistToFormHeight();
            ClampFormToScreen();

            if (!open)
                SaveEqualizerSettings();
        }


        private void ClampFormToScreen()
        {
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int maxH = area.Height; // можна area.Height - 8

            if (MaximumSize.Height <= 0 || MaximumSize.Height > maxH)
                MaximumSize = new Size(MaximumSize.Width, maxH);

            if (Height > maxH)
                Height = maxH;

            if (Bottom > area.Bottom)
                Top = Math.Max(area.Top, area.Bottom - Height);
            if (Top < area.Top)
                Top = area.Top;
        }

        private void eqButton_Click(object sender, EventArgs e)
        {
            if (isCustomSize) return;
            SetEqPanelOpen(!eqPanelOpen);
        }

        private void eqPresetButton_Click(object sender, EventArgs e)
        {
            int itemHeight = (int)(28 * DeviceDpi / 96f);
            int width = eqPresetButton.Width;

            eqPresetMenu.MinimumSize = new Size(width, 0);

            foreach (ToolStripItem item in eqPresetMenu.Items)
            {
                if (item is ToolStripSeparator) continue;

                item.AutoSize = false;
                item.Size = new Size(width - eqPresetMenu.Padding.Horizontal, itemHeight);
            }

            eqPresetMenu.Show(eqPresetButton, new Point(0, eqPresetButton.Height + 2));
        }

        private void eqToggleButton_Click(object sender, EventArgs e)
        {
            eqEnabled = !eqEnabled;
            if (equalizer != null) equalizer.Enabled = eqEnabled;

            UpdateEqToggleUi();
            SaveEqualizerSettings();
        }

        // Користувач рухає повзунки
        private void equalizerView_GainsChanged(object sender, EventArgs e)
        {
            Array.Copy(equalizerView.Gains, eqGains, eqGains.Length);

            if (eqPreset != EqCustomName)
            {
                eqPreset = EqCustomName;
                UpdateEqPresetUi();
            }

            equalizer?.SetAllGains(eqGains, immediate: false);
        }


        /// <summary>
        /// Після зміни висоти вікна: плейлист розтягується, нижні кнопки їдуть за ним.
        /// </summary>
        private void SyncPlaylistToFormHeight()
        {
            ClampFormToScreen();

            if (isCustomSize || onlineOpen)
                return;

            const int bottomBarHeight = 36;
            const int bottomPad = 8;
            const int gap = 8;

            // Кнопки ВСЕГДА прижаты к низу окна
            int buttonsY = ClientSize.Height - bottomPad - bottomBarHeight;

            // Защита: если окно слишком сжали
            if (buttonsY < ListPlayer.Top + 80)
                buttonsY = ListPlayer.Top + 80;

            // Плейлист занимает всё место от своего верха (он под EQ) до кнопок
            ListPlayer.Height = buttonsY - gap - ListPlayer.Top;

            // Расставляем кнопки
            if (searchBox != null) searchBox.Top = buttonsY;
            if (speedButton != null) speedButton.Top = buttonsY;
            if (repeatTrack != null) repeatTrack.Top = buttonsY;
            if (noRepeatTrack != null) noRepeatTrack.Top = buttonsY;
            if (yesshuffleTrack != null) yesshuffleTrack.Top = buttonsY;
            if (nosshuffleTrack != null) nosshuffleTrack.Top = buttonsY;
            if (AddPlayList != null) AddPlayList.Top = buttonsY;
            if (DeletePlayList != null) DeletePlayList.Top = buttonsY;

            _lastClientHeight = ClientSize.Height;
            originalSize = Size;
        }

        // ==================== Сповіщення про трек ====================
        private async void ShowTrackToast(string filePath, int version)
        {
            if (!toastEnabled || toast == null) return;

            // При повторі одного й того ж треку не набридаємо сповіщенням
            if (isRepeating && string.Equals(filePath, _lastToastFile, StringComparison.OrdinalIgnoreCase))
                return;

            (string Title, string Artist, string Album, TimeSpan Duration) info;
            try
            {
                info = await Task.Run(() => ReadTrackInfo(filePath));
            }
            catch
            {
                return;
            }

            // Трек уже змінився, плейлист очищено або форму закрито
            if (version != _coverVersion || IsDisposed) return;

            _lastToastFile = filePath;

            string line2 =
                string.IsNullOrWhiteSpace(info.Artist) ? info.Album :
                string.IsNullOrWhiteSpace(info.Album) ? info.Artist :
                $"{info.Artist} — {info.Album}";

            var parts = new List<string>();

            if (playList != null && playingTrack != null)
            {
                int idx = playList.Tracks.IndexOf(playingTrack);
                if (idx >= 0) parts.Add($"{idx + 1} / {playList.Tracks.Count}");
            }

            if (info.Duration > TimeSpan.Zero)
            {
                parts.Add(info.Duration.TotalHours >= 1
                    ? info.Duration.ToString(@"h\:mm\:ss")
                    : info.Duration.ToString(@"m\:ss"));
            }

            toast.ShowTrack(pictureCover.Image, info.Title, line2,
                string.Join("  •  ", parts), Screen.FromControl(this).WorkingArea);
        }

        // Назва, виконавець, альбом і тривалість з тегів (працює у фоновому потоці)
        private static (string Title, string Artist, string Album, TimeSpan Duration) ReadTrackInfo(string path)
        {
            string title = Path.GetFileNameWithoutExtension(path);
            string artist = "", album = "";
            TimeSpan duration = TimeSpan.Zero;

            try
            {
                using var file = TagLib.File.Create(path);

                if (!string.IsNullOrWhiteSpace(file.Tag.Title))
                    title = file.Tag.Title.Trim();

                artist = file.Tag.FirstPerformer?.Trim() ?? "";
                album = file.Tag.Album?.Trim() ?? "";
                duration = file.Properties.Duration;
            }
            catch
            {
                // немає тегів: лишається ім'я файлу
            }

            return (title, artist, album, duration);
        }

        private void LoadToastSetting()
        {
            try
            {
                if (File.Exists(ToastSettingsPath))
                    toastEnabled = File.ReadAllText(ToastSettingsPath).Trim() != "0";
            }
            catch { }
        }

        private void SaveToastSetting()
        {
            try { File.WriteAllText(ToastSettingsPath, toastEnabled ? "1" : "0"); }
            catch { }
        }

        // ==================== Обкладинка ====================

        private static readonly string[] ImageExtensions =
            { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff" };

        // Точні назви файлів-обкладинок у порядку пріоритету (без пробілів, "_" і "-")
        private static readonly string[] CoverNames =
        {
            "cover", "folder", "front", "album", "albumart", "albumartsmall", "albumartlarge",
            "artwork", "art", "coverart", "frontcover", "albumcover",
            "thumb", "thumbnail", "poster", "default",
            "обкладинка", "обкладинки", "обложка", "обложки"
        };

        // Якщо точного збігу немає, підійде файл, назва якого починається з цих слів
        private static readonly string[] CoverPrefixes =
            { "cover", "folder", "front", "album", "artwork", "обкладинк", "обложк" };

        // Підпапки, де зазвичай лежать обкладинки
        private static readonly string[] CoverSubfolders =
            { "Covers", "Cover", "Artwork", "Art", "Scans", "Images", "Image" };

        // Шукає обкладинку у фоні, щоб вікно не підвисало (мережеві диски, великі файли)
        private async void LoadCoverArt(string filePath)
        {
            int version = ++_coverVersion;
            Image cover = null;

            try
            {
                cover = await Task.Run(() => FindCover(filePath));
            }
            catch
            {
            }

            // Трек уже змінився, плейлист очищено або форма закрита: результат застарів
            if (version != _coverVersion || IsDisposed)
            {
                cover?.Dispose();
                return;
            }

            SetCover(cover ?? LoadDefaultCover());
            ShowTrackToast(filePath, version);
        }

        // Повертає обкладинку для треку або null (працює у фоновому потоці)
        private static Image FindCover(string trackPath)
        {
            Image img;

            if (PreferFolderCover)
            {
                img = FindFolderCover(trackPath) ?? FindEmbeddedCover(trackPath);
            }
            else
            {
                img = FindEmbeddedCover(trackPath) ?? FindFolderCover(trackPath);
            }

            return img;
        }

        // 1) Обкладинка, вшита в сам трек (ID3 / FLAC / M4A та інші формати TagLib)
        private static Image FindEmbeddedCover(string trackPath)
        {
            try
            {
                using var file = TagLib.File.Create(trackPath);
                var pictures = file.Tag.Pictures;
                if (pictures == null || pictures.Length == 0) return null;

                // Спочатку «Front Cover», потім будь-яка непорожня картинка
                var ordered = pictures
                    .Where(p => p.Data != null && p.Data.Count > 0)
                    .OrderBy(p => p.Type == TagLib.PictureType.FrontCover ? 0 : 1);

                foreach (var picture in ordered)
                {
                    Image img = ImageFromBytes(picture.Data.Data);
                    if (img != null) return img;
                }
            }
            catch
            {
                // немає тегів або вони пошкоджені
            }

            return null;
        }

        // 2) Файл-обкладинка поруч із треком (cover.jpg, folder.jpg тощо)
        private static Image FindFolderCover(string trackPath)
        {
            try
            {
                foreach (string path in EnumerateCoverFiles(trackPath))
                {
                    Image img = ImageFromFile(path);
                    if (img != null) return img;
                }
            }
            catch
            {
            }

            return null;
        }

        private static IEnumerable<string> EnumerateCoverFiles(string trackPath)
        {
            string dir = Path.GetDirectoryName(trackPath);
            if (string.IsNullOrEmpty(dir)) yield break;

            // а) файл з тим самим ім'ям, що й трек: song.mp3 -> song.jpg
            foreach (string ext in ImageExtensions)
            {
                string same = Path.ChangeExtension(trackPath, ext);
                if (File.Exists(same)) yield return same;
            }

            // б) папка треку
            foreach (string p in CoversInDirectory(dir, allowSingleImage: true))
                yield return p;

            // в) підпапки Covers / Artwork / Scans ...
            foreach (string sub in CoverSubfolders)
            {
                string subDir = Path.Combine(dir, sub);
                if (!Directory.Exists(subDir)) continue;

                foreach (string p in CoversInDirectory(subDir, allowSingleImage: true))
                    yield return p;
            }

            // г) батьківська папка (альбом із підпапками CD1 / CD2)
            string parent = Path.GetDirectoryName(dir);
            if (!string.IsNullOrEmpty(parent))
            {
                foreach (string p in CoversInDirectory(parent, allowSingleImage: false))
                    yield return p;
            }
        }

        // Повертає файли-кандидати з папки в порядку пріоритету
        private static List<string> CoversInDirectory(string dir, bool allowSingleImage)
        {
            var result = new List<string>();

            try
            {
                var byName = new Dictionary<string, string>();
                var all = new List<(string Norm, string Path)>();

                foreach (string file in Directory.EnumerateFiles(dir))
                {
                    string ext = Path.GetExtension(file);
                    if (!ImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                        continue;

                    string norm = NormalizeName(Path.GetFileNameWithoutExtension(file));
                    all.Add((norm, file));
                    byName.TryAdd(norm, file);
                }

                // Точні назви в порядку пріоритету
                foreach (string name in CoverNames)
                {
                    if (byName.TryGetValue(name, out string exact) && !result.Contains(exact))
                        result.Add(exact);
                }

                // Назви, що починаються з типового слова: cover1.jpg, Folder_big.jpg, AlbumArt_{GUID}_Large.jpg
                foreach (var item in all)
                {
                    if (result.Contains(item.Path)) continue;

                    string norm = item.Norm;
                    if (CoverPrefixes.Any(prefix => norm.StartsWith(prefix, StringComparison.Ordinal)))
                        result.Add(item.Path);
                }

                // У папці рівно одна картинка: вважаємо її обкладинкою
                if (allowSingleImage && all.Count == 1 && !result.Contains(all[0].Path))
                    result.Add(all[0].Path);
            }
            catch
            {
                // папка недоступна: пропускаємо
            }

            return result;
        }

        // "Cover Art" / "cover_art" / "COVER-ART" -> "coverart"
        private static string NormalizeName(string name) =>
            string.Concat(name.Where(char.IsLetterOrDigit)).ToLowerInvariant();

        private static Image ImageFromFile(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length == 0 || info.Length > 50L * 1024 * 1024)
                    return null;

                // Читаємо в пам'ять, щоб не блокувати файл
                return ImageFromBytes(File.ReadAllBytes(path));
            }
            catch
            {
                return null;
            }
        }

        private static Image ImageFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                using var ms = new MemoryStream(data);
                using var src = Image.FromStream(ms);
                return ScaleDown(src);
            }
            catch
            {
                return null; // непідтримуваний формат (наприклад, WebP) або пошкоджена картинка
            }
        }

        // Робить незалежну копію та зменшує дуже великі зображення
        private static Image ScaleDown(Image src)
        {
            int w = src.Width, h = src.Height;
            double k = Math.Min(1.0, (double)MaxCoverSize / Math.Max(w, h));
            int nw = Math.Max(1, (int)Math.Round(w * k));
            int nh = Math.Max(1, (int)Math.Round(h * k));

            var bmp = new Bitmap(nw, nh, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(src, 0, 0, nw, nh);
            return bmp;
        }

        // Обкладинка за замовчуванням: Image\unknown_artist.jpg (якщо немає, CoverBox малює заглушку)
        private Image LoadDefaultCover()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Image", "unknown_artist.jpg");
            return ImageFromFile(path);
        }

        private void SetCover(Image image)
        {
            Image old = pictureCover.Image;
            pictureCover.Image = image;
            old?.Dispose();
        }

        // ==================== Тривалість ====================
        private async void UpdateAllTracksTimeLabel()
        {
            int count = ListPlayer.Items.Count;
            if (count == 0)
            {
                AllTracksTime.Text = "Немає доріжок!";
                return;
            }

            int version = ++_durationVersion;
            var paths = ListPlayer.Items.Cast<Track>().Select(t => t.FilePath).ToList();
            AllTracksTime.Text = $"Усього {count} {TrackWord(count)} — …";

            try
            {
                TimeSpan total = await Task.Run(() =>
                {
                    TimeSpan sum = TimeSpan.Zero;
                    foreach (string path in paths)
                        sum += GetDuration(path);
                    return sum;
                });

                if (version != _durationVersion || IsDisposed) return;
                AllTracksTime.Text = $"Усього {count} {TrackWord(count)} — {FormatDuration(total)}";
                ListPlayer.Invalidate();   // перемалювати тривалості, які щойно порахувалися
            }
            catch { }
        }

        private TimeSpan GetDuration(string path)
        {
            if (_durations.TryGetValue(path, out TimeSpan cached))
                return cached;

            TimeSpan duration = TimeSpan.Zero;
            try
            {
                using var tag = TagLib.File.Create(path);
                duration = tag.Properties.Duration;
            }
            catch { }

            if (duration <= TimeSpan.Zero)
            {
                try
                {
                    using var reader = new AudioFileReader(path);
                    duration = reader.TotalTime;
                }
                catch { }
            }

            if (duration > TimeSpan.Zero)
                _durations[path] = duration;
            return duration;
        }

        private static string TrackWord(int n)
        {
            int n100 = n % 100, n10 = n % 10;
            if (n100 >= 11 && n100 <= 14) return "треків";
            if (n10 == 1) return "трек";
            if (n10 >= 2 && n10 <= 4) return "трека";
            return "треків";
        }

        private static string FormatDuration(TimeSpan d)
        {
            int hours = (int)d.TotalHours;
            return hours > 0
                ? $"{hours} год {d.Minutes} хв. {d.Seconds} с."
                : $"{d.Minutes} хв. {d.Seconds} с.";
        }

        // ==================== Згортання / розгортання (лише double-click) ====================

        private void SaveLayoutOnce()
        {
            if (_layoutSaved) return;

            Control[] controls =
            {
                pictureCover, NameTrack,
                BackButton, ForwardButton, PauseButton, StopButton,
                VolumeLabel, Volume,
                TimerLabel, progressBarMusic,
                AllTracksTime, ListPlayer, spectrumView,
                repeatTrack, noRepeatTrack,
                yesshuffleTrack, nosshuffleTrack,
                AddPlayList, DeletePlayList
            };

            foreach (var c in controls)
                _savedLocations[c] = c.Location;

            _savedMinSize = MinimumSize;
            _layoutSaved = true;
        }

        private void RestoreLayout()
        {
            foreach (var kv in _savedLocations)
                kv.Key.Location = kv.Value;
        }

        private void CollapseToCorner()
        {
            // Спочатку закриваємо панель еквалайзера, щоб збережені позиції були «базовими»
            SetEqPanelOpen(false);

            SaveLayoutOnce();
            isCustomSize = true;

            MinimumSize = new Size(304, 120);

            pictureCover.Visible = false;
            ListPlayer.Visible = false;
            spectrumView.Visible = false;
            eqButton.Visible = false;
            AllTracksTime.Visible = false;
            repeatTrack.Visible = false;
            noRepeatTrack.Visible = false;
            yesshuffleTrack.Visible = false;
            nosshuffleTrack.Visible = false;
            AddPlayList.Visible = false;
            DeletePlayList.Visible = false;
            searchBox.Visible = false;
            playlistTabs.Visible = false;
            balanceSlider.Visible = false;
            speedButton.Visible = false;

            if (webView != null) SetOnlineVisible(false);
            if (onlineButton != null) onlineButton.ForeColor = Color.FromArgb(210, 214, 222);

            int y = TitleBarHeight + 8;
            NameTrack.Location = new Point(8, y);
            y += NameTrack.Height + 8;

            BackButton.Location = new Point(8, y);
            ForwardButton.Location = new Point(57, y);
            PauseButton.Location = new Point(106, y);
            StopButton.Location = new Point(155, y);
            VolumeLabel.Location = new Point(204, y + 8);
            Volume.Location = new Point(229, y + 6);
            y += 46;

            TimerLabel.Location = new Point(8, y + 2);
            progressBarMusic.Location = new Point(70, y);

            int bottom = Math.Max(
                TimerLabel.Bottom,
                progressBarMusic.Bottom
            );

            Height = bottom + 12;

            Rectangle area = Screen.FromControl(this).WorkingArea;
            Location = new Point(area.Right - Width, area.Bottom - Height);
        }

        private void RestoreWindow()
        {
            if (!isCustomSize) return;

            MinimumSize = _savedMinSize;
            Size = originalSize;
            isCustomSize = false;

            RestoreLayout();

            pictureCover.Visible = true;
            ListPlayer.Visible = true;
            spectrumView.Visible = true;
            eqButton.Visible = true;
            AllTracksTime.Visible = true;
            AddPlayList.Visible = true;
            DeletePlayList.Visible = true;
            searchBox.Visible = true;
            playlistTabs.Visible = true;
            balanceSlider.Visible = true;
            speedButton.Visible = true;

            repeatTrack.Visible = !isRepeating;
            noRepeatTrack.Visible = isRepeating;
            yesshuffleTrack.Visible = !isShuffle;
            nosshuffleTrack.Visible = isShuffle;

            Rectangle area = Screen.FromControl(this).WorkingArea;
            Location = new Point(
                area.X + (area.Width - Width) / 2,
                area.Y + (area.Height - Height) / 2);

            _lastClientHeight = ClientSize.Height;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Normal) return;

            if (reopenOnlineAfterRestore && webView != null)
            {
                reopenOnlineAfterRestore = false;
                SetOnlineVisible(true);
                onlineButton.ForeColor = Theme.Accent2;
            }

            if (isCustomSize) return;


        }

        // Подвійний клік по обкладинці → згорнути
        private void pictureCover_DoubleClick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal && !isCustomSize)
                CollapseToCorner();
        }

        // Подвійний клік по формі / назві / таймеру → розгорнути
        private void Form1_DoubleClick(object sender, EventArgs e)
        {
            if (isCustomSize)
                RestoreWindow();
        }

        private void CollapsedArea_DoubleClick(object sender, EventArgs e)
        {
            if (isCustomSize)
                RestoreWindow();
        }

        // Порожній обробник: у Designer лишився Click, нічого не робить
        private void pictureCover_Click(object sender, EventArgs e) { }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // Разрешаем ресайз при открытом EQ. Блокируем только если окно свёрнуто в мини-режим.
            if (e.Button != MouseButtons.Left || isCustomSize || onlineOpen)
                return;

            if (e.Y >= ClientSize.Height - BottomResizeBorder)
            {
                _resizingBottom = true;
                _resizeStartY = Cursor.Position.Y;
                _resizeStartHeight = Height;
                _listStartHeight = ListPlayer.Height;
                Capture = true;
                Cursor = Cursors.SizeNS;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!_resizingBottom)
            {
                if (!isCustomSize && !onlineOpen &&
                    e.Y >= ClientSize.Height - BottomResizeBorder)
                    Cursor = Cursors.SizeNS;
                else
                    Cursor = Cursors.Default;
                return;
            }

            int dy = Cursor.Position.Y - _resizeStartY;
            int newH = _resizeStartHeight + dy;

            int minH = MinimumSize.Height > 0 ? MinimumSize.Height : 400;
            int maxH = MaximumSize.Height > 0 ? MaximumSize.Height : 5000;
            newH = Math.Clamp(newH, minH, maxH);

            Height = newH;
            SyncPlaylistToFormHeight();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_resizingBottom) return;

            _resizingBottom = false;
            Capture = false;
            Cursor = Cursors.Default;
            originalSize = Size; // після collapse/restore збереже нову висоту
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCLIENT = 1;
            const int HTBOTTOM = 15;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020;

            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt64() & 0xFFF0) == SC_MINIMIZE)
            {
                if (isCustomSize || IsOnlineActive)
                {
                    reopenOnlineAfterRestore = onlineOpen;
                    if (webView != null) SetOnlineVisible(false);
                    base.WndProc(ref m);
                }
                else
                {
                    CollapseToCorner();
                }
                return;
            }

            if (m.Msg == WM_NCHITTEST && !isCustomSize && !onlineOpen)
            {
                base.WndProc(ref m);
                if (m.Result == (IntPtr)HTCLIENT)
                {
                    Point p = PointToClient(Cursor.Position);
                    if (p.Y >= ClientSize.Height - BottomResizeBorder)
                        m.Result = (IntPtr)HTBOTTOM;
                }
                return;
            }

            base.WndProc(ref m);
        }

        // ==================== Drag & Drop ====================
        private void ListPlayer_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void ListPlayer_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            try
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths == null || paths.Length == 0) return;

                int previousCount = viewList.Tracks.Count;
                AddFilesToViewList(paths);

                if (viewList.Tracks.Count > 0 && waveOutDevice == null)
                {
                    int index = previousCount > 0 ? ListPlayer.SelectedIndex : 0;
                    if (index < 0) index = 0;
                    SelectAndPlay(index);
                }

                SavePlaylist(viewList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не вдалося додати файли до плейлиста:\n{ex.Message}",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== Гарячі клавіші ====================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (onlineOpen) return base.ProcessCmdKey(ref msg, keyData);

            if (ActiveControl is TextBoxBase)
                return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.Space:
                    PauseButton_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.Right:
                    MoveSelection(+1);
                    return true;
                case Keys.Control | Keys.Left:
                    MoveSelection(-1);
                    return true;
                case Keys.Control | Keys.Tab:
                    CycleTab(+1);
                    return true;
                case Keys.Control | Keys.Shift | Keys.Tab:
                    CycleTab(-1);
                    return true;
                case Keys.Control | Keys.O:
                    AddTracksFromDialog();
                    return true;
                case Keys.Control | Keys.E:
                    if (!isCustomSize) SetEqPanelOpen(!eqPanelOpen);
                    return true;
                case Keys.Control | Keys.T:
                    toastEnabled = !toastEnabled;
                    SaveToastSetting();
                    if (toastEnabled)
                    {
                        _lastToastFile = null;
                        if (playingTrack != null)
                            ShowTrackToast(playingTrack.FilePath, _coverVersion);
                    }
                    else
                    {
                        toast?.HideNow();
                    }
                    return true;
                case Keys.Control | Keys.R:
                    SetShuffleMode(!isShuffle);
                    MessageBox.Show(
                        isShuffle ? "Випадкове відтворення увімкнено." : "Випадкове відтворення вимкнено.",
                        "Режим відтворення", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                case Keys.Control | Keys.Delete:
                    ClearPlaylist();
                    return true;
                case Keys.Escape:
                    StopMusic();
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SetShuffleMode(bool enabled)
        {
            isShuffle = enabled;
            yesshuffleTrack.Visible = !enabled;
            nosshuffleTrack.Visible = enabled;
        }

        private void yesshuffleTrack_Click(object sender, EventArgs e) => SetShuffleMode(true);
        private void nosshuffleTrack_Click(object sender, EventArgs e) => SetShuffleMode(false);


        // Вирівнюємо верхній ряд панелі EQ за краями equalizerView
        private void LayoutEqPanel()
        {
            const int margin = 4;   // той самий відступ, що й у equalizerView
            const int gap = 6;

            // Кнопка ввімк/вимк притиснута до правого краю
            eqToggleButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            eqToggleButton.Location = new Point(
                eqPanel.ClientSize.Width - margin - eqToggleButton.Width,
                margin);

            // Кнопка пресетів займає все місце, що лишилось ліворуч
            eqPresetButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            eqPresetButton.Location = new Point(margin, margin);
            eqPresetButton.Width = eqToggleButton.Left - gap - margin;

            eqToggleButton.Height = eqPresetButton.Height;

            equalizerView.Location = new Point(margin, eqPresetButton.Bottom + 4);
        }

        // ==================== Пошук у плейлисті ====================
        private void BuildSearchBox()
        {
            int left = (speedButton?.Right ?? yesshuffleTrack.Right) + 6;
            int right = AddPlayList.Left - 8;

            searchBox = new SearchBox
            {
                Name = "searchBox",
                Location = new Point(left, repeatTrack.Top),
                Size = new Size(right - left, repeatTrack.Height)
            };

            searchBox.SearchChanged += SearchBox_SearchChanged;
            searchBox.SearchKeyDown += SearchBox_KeyDown;

            Controls.Add(searchBox);
            toolTip.SetToolTip(searchBox, "Пошук треку (Enter: відтворити, Esc: очистити)");
        }

        private void SearchBox_SearchChanged(object sender, EventArgs e)
        {
            ListPlayer.SearchText = searchBox.Query;

            int first = ListPlayer.FindMatch(0);
            if (first >= 0) ListPlayer.TopIndex = first;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = e.SuppressKeyPress = true;

                // Якщо поточний трек уже збігається, переходимо до наступного збігу
                int sel = ListPlayer.SelectedIndex;
                int start = sel >= 0 && ListPlayer.FindMatch(sel) == sel ? sel + 1 : 0;

                int idx = ListPlayer.FindMatch(start);
                if (idx >= 0)
                {
                    SelectAndPlay(idx);
                    ListPlayer.Focus();   // щоб Пробіл знову працював як «Пауза»
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = e.SuppressKeyPress = true;
                searchBox.Clear();
                ListPlayer.Focus();
            }
        }

        // ==================== Баланс, оцінки, тривалість ====================
        private void BuildListExtras()
        {
            // Напис «Усього…» звужуємо, праворуч від нього ставимо баланс
            AllTracksTime.AutoSize = false;
            AllTracksTime.AutoEllipsis = true;
            AllTracksTime.TextAlign = ContentAlignment.MiddleLeft;
            AllTracksTime.Size = new Size(196, 18);

            int left = AllTracksTime.Right + 4;

            balanceSlider = new BalanceSlider
            {
                Name = "balanceSlider",
                Location = new Point(left, AllTracksTime.Top - 1),
                Size = new Size(ClientSize.Width - 8 - left, 20)
            };

            balanceSlider.ValueChanged += (s, e) =>
            {
                balance = balanceSlider.Value / 100f;
                if (balanceProvider != null) balanceProvider.Balance = balance;
                UpdateBalanceTip();
            };

            Controls.Add(balanceSlider);
            UpdateBalanceTip();

            // Оцінки та тривалість у списку
            ListPlayer.RatingProvider = o =>
                o is Track t && ratings.TryGetValue(t.FilePath, out int r) ? r : 0;

            ListPlayer.DurationProvider = o =>
                o is Track t && _durations.TryGetValue(t.FilePath, out TimeSpan d) ? d : TimeSpan.Zero;

            ListPlayer.RatingClicked += ListPlayer_RatingClicked;
        }

        private void UpdateBalanceTip()
        {
            int v = balanceSlider.Value;
            string text = v == 0 ? "центр" : v < 0 ? $"лівий {-v}%" : $"правий {v}%";
            toolTip.SetToolTip(balanceSlider, $"Баланс: {text} (подвійний клік — центр)");
        }

        private void ListPlayer_RatingClicked(int index, int stars)
        {
            if (index < 0 || index >= ListPlayer.Items.Count) return;
            if (ListPlayer.Items[index] is not Track t) return;

            ratings.TryGetValue(t.FilePath, out int current);
            int value = current == stars ? 0 : stars;     // повторний клік знімає оцінку

            if (value == 0) ratings.Remove(t.FilePath);
            else ratings[t.FilePath] = value;

            SaveRatings();
            ListPlayer.Invalidate();
        }

        private void LoadRatings()
        {
            ratings.Clear();

            try
            {
                if (!File.Exists(RatingsFilePath)) return;

                foreach (string line in File.ReadLines(RatingsFilePath))
                {
                    int bar = line.IndexOf('|');
                    if (bar <= 0) continue;

                    if (int.TryParse(line.Substring(0, bar), out int r) && r >= 1 && r <= 5)
                        ratings[line.Substring(bar + 1)] = r;
                }
            }
            catch
            {
                // пошкоджений файл: починаємо без оцінок
            }
        }

        private void SaveRatings()
        {
            try
            {
                Directory.CreateDirectory(PlaylistDir);
                File.WriteAllLines(RatingsFilePath, ratings.Select(kv => $"{kv.Value}|{kv.Key}"));
            }
            catch
            {
                // не критично
            }
        }

        // ==================== Власні пресети ====================
        private void LoadCustomPresets()
        {
            customPresets.Clear();

            try
            {
                if (!File.Exists(PresetsFilePath)) return;

                foreach (string line in File.ReadLines(PresetsFilePath))
                {
                    int bar = line.IndexOf('|');
                    if (bar <= 0) continue;

                    string name = line.Substring(0, bar).Trim();
                    string[] parts = line.Substring(bar + 1).Split(';');
                    if (name.Length == 0 || parts.Length < EqualizerSampleProvider.BandCount) continue;

                    var gains = new float[EqualizerSampleProvider.BandCount];
                    bool ok = true;

                    for (int i = 0; i < gains.Length; i++)
                    {
                        if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                        {
                            ok = false;
                            break;
                        }

                        gains[i] = Math.Clamp(v,
                            -EqualizerSampleProvider.MaxGainDb,
                            EqualizerSampleProvider.MaxGainDb);
                    }

                    if (ok) customPresets.Add((name, gains));
                }
            }
            catch
            {
                // пошкоджений файл: починаємо без власних пресетів
            }
        }

        private void SaveCustomPresets()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PresetsFilePath));

                File.WriteAllLines(PresetsFilePath, customPresets.Select(p =>
                    p.Name + "|" + string.Join(";",
                        p.Gains.Select(g => g.ToString("0.0", CultureInfo.InvariantCulture)))));
            }
            catch
            {
                // не критично
            }
        }

        private void RebuildPresetMenu()
        {
            eqPresetMenu.Items.Clear();

            foreach (var preset in EqPresets)
            {
                string name = preset.Name;
                eqPresetMenu.Items.Add(name, null, (s, e) => ApplyPreset(name));
            }

            foreach (var preset in customPresets)
            {
                string name = preset.Name;
                eqPresetMenu.Items.Add(name, null, (s, e) => ApplyPreset(name));
            }

            eqPresetMenu.Items.Add(new ToolStripSeparator());
            eqPresetMenu.Items.Add("＋  Зберегти поточні як пресет…", null, (s, e) => SaveCurrentAsPreset());

            if (customPresets.Count > 0)
            {
                var del = new ToolStripMenuItem("Видалити пресет");

                foreach (var preset in customPresets)
                {
                    string name = preset.Name;
                    del.DropDownItems.Add(name, null, (s, e) => DeleteCustomPreset(name));
                }

                eqPresetMenu.Items.Add(del);
            }
        }

        private void SaveCurrentAsPreset()
        {
            string initial = eqPreset == EqCustomName ? "Мій пресет" : eqPreset + " 2";
            string name = AskText("Зберегти пресет", initial);
            if (name == null) return;

            name = name.Replace("|", " ").Trim();
            if (name.Length == 0) return;

            bool reserved =
                name.Equals(EqCustomName, StringComparison.CurrentCultureIgnoreCase) ||
                EqPresets.Any(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

            if (reserved)
            {
                MessageBox.Show("Це ім'я вже зайняте стандартним пресетом. Оберіть інше.");
                return;
            }

            var entry = (name, (float[])eqGains.Clone());
            int i = customPresets.FindIndex(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

            if (i >= 0) customPresets[i] = entry;
            else customPresets.Add(entry);

            eqPreset = name;

            SaveCustomPresets();
            RebuildPresetMenu();
            UpdateEqPresetUi();
            SaveEqualizerSettings();
        }

        private void DeleteCustomPreset(string name)
        {
            customPresets.RemoveAll(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

            if (eqPreset.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                eqPreset = EqCustomName;

            SaveCustomPresets();
            RebuildPresetMenu();
            UpdateEqPresetUi();
            SaveEqualizerSettings();
        }

        // ==================== Швидкість відтворення ====================
        private void BuildSpeedButton()
        {
            speedButton = new RoundButton
            {
                Name = "speedButton",
                Kind = ButtonKind.Soft,
                Font = new Font("Segoe UI Semibold", 9f),
                ForeColor = Theme.Text,
                Radius = 10,
                Size = new Size(44, repeatTrack.Height),
                Location = new Point(yesshuffleTrack.Right + 8, repeatTrack.Top)
            };

            speedMenu = new ContextMenuStrip
            {
                Renderer = new DarkMenuRenderer(),
                Font = new Font("Segoe UI", 9.5F),
                ShowImageMargin = false
            };

            foreach (float step in SpeedSteps)
            {
                float s = step;
                string label = s == 1f ? "1×  (звичайна)" : s.ToString("0.##", CultureInfo.InvariantCulture) + "×";

                var item = new ToolStripMenuItem(label) { Tag = s };
                item.Click += (a, b) => SetSpeed(s);
                speedMenu.Items.Add(item);
            }

            speedButton.Click += (a, b) =>
            {
                foreach (ToolStripMenuItem item in speedMenu.Items)
                    item.Checked = Math.Abs((float)item.Tag - speed) < 0.001f;

                speedMenu.Show(speedButton.PointToScreen(Point.Empty), ToolStripDropDownDirection.AboveRight);
            };

            Controls.Add(speedButton);
            UpdateSpeedUi();
        }

        private void SetSpeed(float value)
        {
            speed = Math.Clamp(value, 0.25f, 4f);

            if (soundTouchStream != null)
                soundTouchStream.Tempo = speed;

            UpdateSpeedUi();
            SaveEqualizerSettings();
        }

        private void UpdateSpeedUi()
        {
            if (speedButton == null) return;

            speedButton.Text = speed.ToString("0.##", CultureInfo.InvariantCulture) + "×";
            speedButton.ForeColor = Math.Abs(speed - 1f) < 0.001f ? Theme.Text : Theme.Accent2;
            toolTip.SetToolTip(speedButton, "Швидкість відтворення");
        }

        // ==================== YouTube Music (WebView2) ====================
        private void BuildOnlineButton()
        {
            onlineButton = CreateTitleButton("♫");
            onlineButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            onlineButton.Location = new Point(ClientSize.Width - 132, 0);
            onlineButton.Font = new Font("Segoe UI Symbol", 10F);

            onlineMenu = new ContextMenuStrip
            {
                Renderer = new DarkMenuRenderer(),
                Font = new Font("Segoe UI", 9.5F),
                ShowImageMargin = false
            };

            var yt = new ToolStripMenuItem("YouTube Music");
            var sp = new ToolStripMenuItem("Spotify");
            var am = new ToolStripMenuItem("Apple Music");
            yt.Click += async (s, e) => await SwitchServiceAsync(OnlineService.YouTubeMusic);
            sp.Click += async (s, e) => await SwitchServiceAsync(OnlineService.Spotify);
            am.Click += async (s, e) => await SwitchServiceAsync(OnlineService.AppleMusic);

            onlineMenu.Items.Add(yt);
            onlineMenu.Items.Add(sp);
            onlineMenu.Items.Add(am);
            onlineMenu.Items.Add(new ToolStripSeparator());
            onlineMenu.Items.Add("Вийти з акаунтів…", null, async (s, e) => await SignOutOnlineAsync());

            onlineMenu.Opening += (s, e) =>
            {
                yt.Checked = onlineService == OnlineService.YouTubeMusic;
                sp.Checked = onlineService == OnlineService.Spotify;
                am.Checked = onlineService == OnlineService.AppleMusic;
            };

            onlineButton.MouseUp += async (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    onlineMenu.Show(onlineButton, new Point(0, onlineButton.Height));
                else if (e.Button == MouseButtons.Left)
                    await ToggleOnlineAsync();
            };

            titleBar.Controls.Add(onlineButton);
            toolTip.SetToolTip(onlineButton,
                "Онлайн-музика: клік відкриває, правий клік вибирає сервіс");
        }

        // Онлайн-сервіс відкритий або в ньому зараз грає звук
        private bool IsOnlineActive =>
            onlineOpen ||
            (webView?.CoreWebView2 != null && webView.CoreWebView2.IsDocumentPlayingAudio);


        // Згортання на панель завдань: спочатку стискаємо веб-вигляд, щоб звук не переривався
        private void MinimizeKeepingAudio()
        {
            reopenOnlineAfterRestore = onlineOpen;
            if (webView != null) SetOnlineVisible(false);
            WindowState = FormWindowState.Minimized;
        }

        private void SetOnlineVisible(bool show)
        {
            onlineOpen = show;
            if (webView == null) return;

            bool wide = onlineService != OnlineService.YouTubeMusic;

            // Спочатку Anchor, потім розмір, щоб форма не розтягувала веб-вигляд
            webView.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            if (show)
            {
                int sbH = wide ? (int)Math.Round(17 * DeviceDpi / 96.0) : 0;
                int viewH = ClientSize.Height - TitleBarHeight - sbH;

                if (wide)
                {
                    onlineScroll.Minimum = 0;
                    onlineScroll.LargeChange = ClientSize.Width;
                    onlineScroll.Maximum = OnlineWideWidth - 1;   // макс. значення = ширина сторінки - ширина вікна
                    onlineScroll.Location = new Point(0, ClientSize.Height - sbH);
                    onlineScroll.Size = new Size(ClientSize.Width, sbH);
                    onlineScroll.Visible = true;
                    onlineScroll.BringToFront();

                    webView.Location = new Point(-onlineScroll.Value, TitleBarHeight);
                    webView.Size = new Size(OnlineWideWidth, viewH);
                }
                else
                {
                    onlineScroll.Visible = false;
                    onlineScroll.Value = 0;

                    webView.Location = new Point(0, TitleBarHeight);
                    webView.Size = new Size(ClientSize.Width, viewH);
                    webView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                }

                webView.BringToFront();
                titleBar.BringToFront();
                if (wide) onlineScroll.BringToFront();
            }
            else
            {
                if (onlineScroll != null)
                {
                    onlineScroll.Visible = false;
                    onlineScroll.Value = 0;
                }

                webView.Size = new Size(1, 1);
                webView.Location = new Point(0, TitleBarHeight);
                webView.SendToBack();
            }
        }


        private static string ServiceUrl(OnlineService s) => s switch
        {
            OnlineService.Spotify => "https://open.spotify.com",
            OnlineService.AppleMusic => "https://music.apple.com",
            _ => "https://music.youtube.com"
        };

        private void ApplyServiceSettings(Microsoft.Web.WebView2.Core.CoreWebView2 core)
        {
            bool mobile = onlineService == OnlineService.YouTubeMusic && UseMobileUserAgent;
            core.Settings.UserAgent = mobile ? MobileUserAgent : DesktopUserAgent;
        }

        private async Task SwitchServiceAsync(OnlineService s)
        {
            bool created = webView == null;
            bool changed = onlineService != s;
            onlineService = s;

            if (created)
            {
                if (!await CreateWebViewAsync()) return;   // сам відкриє потрібну адресу
            }
            else if (changed)
            {
                ApplyServiceSettings(webView.CoreWebView2);
                webView.CoreWebView2.Navigate(ServiceUrl(s));

                if (onlineOpen) SetOnlineVisible(true);   // перерахувати ширину та смугу прокрутки
            }

            if (!onlineOpen) await ToggleOnlineAsync();
        }

        private async Task ToggleOnlineAsync()
        {
            if (webView == null && !await CreateWebViewAsync())
                return;

            // З міні-форми спочатку повертаємо звичайне вікно
            if (isCustomSize) RestoreWindow();

            bool open = !onlineOpen;
            SetOnlineVisible(open);
            onlineButton.ForeColor = open ? Theme.Accent2 : Color.FromArgb(210, 214, 222);

            if (open)
            {
                if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Playing)
                {
                    waveOutDevice.Pause();
                    PauseButton.Text = PlayGlyph;
                }
                webView.Focus();
            }
            else
            {
                ListPlayer.Focus();
            }
        }

        private async Task<bool> CreateWebViewAsync()
        {
            try
            {
                Directory.CreateDirectory(WebViewProfileDir);

                webView = new Microsoft.Web.WebView2.WinForms.WebView2
                {
                    Location = new Point(0, TitleBarHeight),
                    Size = new Size(ClientSize.Width, ClientSize.Height - TitleBarHeight),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    DefaultBackgroundColor = Theme.Bg
                };

                Controls.Add(webView);

                onlineScroll = new HScrollBar
                {
                    Visible = false,
                    SmallChange = 40,
                    Minimum = 0
                };
                onlineScroll.ValueChanged += (s, e) =>
                {
                    if (webView != null && onlineScroll.Visible)
                        webView.Left = -onlineScroll.Value;
                };
                Controls.Add(onlineScroll);

                onlineScroll.HandleCreated += (s, e) =>
                {
                    try { SetWindowTheme(onlineScroll.Handle, "DarkMode_Explorer", null); } catch { }
                };

                SetOnlineVisible(false);

                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment
                    .CreateAsync(null, WebViewProfileDir);

                await webView.EnsureCoreWebView2Async(env);

                var core = webView.CoreWebView2;
                ApplyServiceSettings(core);
                core.Settings.AreDefaultContextMenusEnabled = false;

                // Нові вікна та спливні посилання відкриваємо в тому самому вікні
                core.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true;
                    core.Navigate(e.Uri);
                };

                core.Navigate(ServiceUrl(onlineService));
                return true;
            }
            catch (Exception ex)
            {
                if (webView != null)
                {
                    Controls.Remove(webView);
                    webView.Dispose();
                    webView = null;
                }

                MessageBox.Show(
                    "Не вдалося запустити вбудований браузер (WebView2).\n" +
                    "Встановіть WebView2 Runtime від Microsoft.\n\n" + ex.Message);
                return false;
            }
        }

        // Вихід з акаунта: видаляємо cookies та дані сайтів у профілі
        private async Task SignOutOnlineAsync()
        {
            if (webView?.CoreWebView2 == null)
            {
                MessageBox.Show("Спочатку відкрийте YouTube Music.");
                return;
            }

            var answer = MessageBox.Show(
                "Вийти з акаунту і видалити збережені дані входу?",
                "AUREON", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (answer != DialogResult.Yes) return;

            try
            {
                await webView.CoreWebView2.Profile.ClearBrowsingDataAsync();
                webView.CoreWebView2.Navigate(ServiceUrl(onlineService));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося очистити дані: " + ex.Message);
            }
        }


        // ==================== Додаткові аудіоефекти AUREON ====================
        // Три крутилки EQ та налаштування додаткових аудіоефектів.
        private RotaryKnobControl bassBoostKnob;
        private RotaryKnobControl virtualizerKnob;
        private RotaryKnobControl subBassKnob;

        private float bassBoostDb;
        private float virtualizerPercent;
        private float subBassDb;

        // Максимальний рівень гучності: 100, 125 або 150 відсотків.
        private int volumeLimit = 100;
        private int savedVolumePercent = 100;
        private ContextMenuStrip volumeLimitMenu;

        private static readonly string AudioEnhancementSettingsPath =
            Path.Combine(AppContext.BaseDirectory, "equalizer", "audio-effects.cfg");

        /// <summary>
        /// Додає три крутилки до наявної панелі EQ.
        /// Викликається з конструктора після LayoutEqPanel().
        /// </summary>
        private void BuildAudioEnhancementsUi()
        {
            eqPanel.SuspendLayout();
            try
            {
                // Панель EQ стає вищою на 54 px. Щоб вікно не стало
                // помітно вищим, звільняємо майже стільки ж місця за рахунок
                // порожньої області плейлиста та зсуву нижнього ряду керування.
                int oldPanelHeight = eqPanel.Height;

                equalizerView.Location = new Point(4, 32);
                equalizerView.Size = new Size(
                    Math.Max(100, eqPanel.ClientSize.Width - 8), 76);

                const int knobTop = 111;
                const int knobHeight = 70;
                const int sideMargin = 4;
                int slotWidth = Math.Max(70,
                    (eqPanel.ClientSize.Width - sideMargin * 2) / 3);

                // Підписи крутилок: БАС, ПРОСТІР (віртуалайзер), СУББАС
                bassBoostKnob = CreateEffectsKnob(
                    "БАС", " дБ", 0, 12, 0,
                    new Point(sideMargin, knobTop),
                    new Size(slotWidth, knobHeight));

                virtualizerKnob = CreateEffectsKnob(
                    "ПРОСТІР", "%", 0, 100, 0,
                    new Point(sideMargin + slotWidth, knobTop),
                    new Size(slotWidth, knobHeight));

                subBassKnob = CreateEffectsKnob(
                    "СУББАС", " дБ", 0, 12, 0,
                    new Point(sideMargin + slotWidth * 2, knobTop),
                    new Size(slotWidth, knobHeight));

                bassBoostKnob.ValueChanged += (s, e) =>
                {
                    bassBoostDb = bassBoostKnob.Value;
                    ApplyAudioEnhancementValues();
                };
                bassBoostKnob.ValueChangeCompleted += (s, e) => SaveAudioEnhancementSettings();

                virtualizerKnob.ValueChanged += (s, e) =>
                {
                    virtualizerPercent = virtualizerKnob.Value;
                    ApplyAudioEnhancementValues();
                };
                virtualizerKnob.ValueChangeCompleted += (s, e) => SaveAudioEnhancementSettings();

                subBassKnob.ValueChanged += (s, e) =>
                {
                    subBassDb = subBassKnob.Value;
                    ApplyAudioEnhancementValues();
                };
                subBassKnob.ValueChangeCompleted += (s, e) => SaveAudioEnhancementSettings();

                eqPanel.Controls.Add(bassBoostKnob);
                eqPanel.Controls.Add(virtualizerKnob);
                eqPanel.Controls.Add(subBassKnob);

                // Підсумкова висота: шапка + десять смуг + три крутилки.
                eqPanel.Height = knobTop + knobHeight + 5;

                int panelGrowth = Math.Max(0, eqPanel.Height - oldPanelHeight);
                int reduction = Math.Min(Math.Max(0, panelGrowth - 8),
                    Math.Max(0, ListPlayer.Height - 100));

                if (reduction > 0)
                {
                    MinimumSize = new Size(MinimumSize.Width, Math.Max(0, MinimumSize.Height - reduction));
                    ClientSize = new Size(ClientSize.Width, ClientSize.Height - reduction);
                    if (MaximumSize.Height > 0)
                    {
                        MaximumSize = new Size(MaximumSize.Width, Math.Max(ClientSize.Height, MaximumSize.Height - reduction));
                    }
                    ListPlayer.Height -= reduction; // <-- ОШИБКА: ломает привязки (Anchor)

                    Control[] lowerControls =
                    {
        searchBox, speedButton, repeatTrack, noRepeatTrack,
        yesshuffleTrack, nosshuffleTrack, AddPlayList, DeletePlayList
    };

                    foreach (Control control in lowerControls)
                    {
                        if (control != null) control.Top -= reduction; // <-- ОШИБКА: сдвигает кнопки вверх, создавая дыру
                    }
                    originalSize = Size;
                }

                toolTip.SetToolTip(bassBoostKnob,
                    "Бас: підсилення басу близько 110 Гц. Потягніть вгору/вниз або скористайтеся колесом миші.");
                toolTip.SetToolTip(virtualizerKnob,
                    "Простір: розширення стереобази. Для моно-файлів ефект не чутно.");
                toolTip.SetToolTip(subBassKnob,
                    "Суббас: підсилення найнижчих частот близько 45 Гц.");
            }
            finally
            {
                eqPanel.ResumeLayout(true);
            }

            UpdateAudioEnhancementsEnabled();
        }

        private RotaryKnobControl CreateEffectsKnob(
            string caption, string unit, int min, int max, int initial,
            Point location, Size size)
        {
            var knob = new RotaryKnobControl
            {
                Caption = caption,
                Unit = unit,
                Minimum = min,
                Maximum = max,
                Value = initial,
                Location = location,
                Size = size,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            return knob;
        }

        /// <summary>Готує ModernSlider до шкали реальних відсотків 0–100.</summary>
        private void InitializeVolumeControl()
        {
            Volume.Minimum = 0;
            Volume.Maximum = 100;
            Volume.Value = 100;

            volumeLimit = 100;
            volumeLimitMenu = new ContextMenuStrip
            {
                Renderer = new DarkMenuRenderer(),
                Font = new Font("Segoe UI", 9.5f),
                ShowImageMargin = false
            };

            foreach (int limit in new[] { 100, 125, 150 })
            {
                int value = limit;
                var item = new ToolStripMenuItem($"Максимум {value}%")
                {
                    Tag = value,
                    CheckOnClick = false
                };
                item.Click += (s, e) => SetVolumeLimit(value);
                volumeLimitMenu.Items.Add(item);
            }

            volumeLimitMenu.Opening += (s, e) =>
            {
                foreach (ToolStripMenuItem item in volumeLimitMenu.Items)
                    item.Checked = (int)item.Tag == volumeLimit;
            };

            // Правий клік по самому повзунку відкриває меню ліміту.
            Volume.ContextMenuStrip = volumeLimitMenu;

            // Клік по значку динаміка — альтернативний спосіб відкрити це меню.
            VolumeLabel.Cursor = Cursors.Hand;
            VolumeLabel.Click += (s, e) =>
            {
                volumeLimitMenu.Show(VolumeLabel,
                    new Point(0, VolumeLabel.Height), ToolStripDropDownDirection.BelowRight);
            };

            UpdateVolumeToolTip();
        }

        private void SetVolumeLimit(int limit, bool save = true)
        {
            if (limit != 100 && limit != 125 && limit != 150)
                limit = 100;

            int currentValue = Volume.Value;
            volumeLimit = limit;
            Volume.Maximum = limit;
            Volume.Value = Math.Min(currentValue, limit);

            if (masterVolume != null)
                masterVolume.Volume = CurrentVolume() * GetFadeFactor();

            UpdateVolumeToolTip();

            if (save)
                SaveAudioEnhancementSettings();
        }

        private void UpdateVolumeToolTip()
        {
            string text = $"Гучність: {Volume.Value}% (максимум {volumeLimit}%). Правий клік — змінити межу.";
            toolTip?.SetToolTip(Volume, text);
            toolTip?.SetToolTip(VolumeLabel, text);
        }

        private void LoadAudioEnhancementSettings()
        {
            try
            {
                if (File.Exists(AudioEnhancementSettingsPath))
                {
                    foreach (string line in File.ReadLines(AudioEnhancementSettingsPath))
                    {
                        int separator = line.IndexOf('=');
                        if (separator <= 0) continue;

                        string key = line.Substring(0, separator).Trim();
                        string raw = line.Substring(separator + 1).Trim();

                        switch (key)
                        {
                            case "BassBoost":
                                if (float.TryParse(raw, NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out float bass))
                                    bassBoostDb = Math.Clamp(bass, 0f, 12f);
                                break;

                            case "Virtualizer":
                                if (float.TryParse(raw, NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out float virt))
                                    virtualizerPercent = Math.Clamp(virt, 0f, 100f);
                                break;

                            case "SubBass":
                                if (float.TryParse(raw, NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out float sub))
                                    subBassDb = Math.Clamp(sub, 0f, 12f);
                                break;

                            case "VolumeLimit":
                                if (int.TryParse(raw, out int limit) &&
                                    (limit == 100 || limit == 125 || limit == 150))
                                    volumeLimit = limit;
                                break;

                            case "VolumePercent":
                                if (int.TryParse(raw, out int percent))
                                    savedVolumePercent = Math.Clamp(percent, 0, 150);
                                break;
                        }
                    }
                }
            }
            catch
            {
                // Пошкоджений файл налаштувань не заважає запуску плеєра.
            }

            bassBoostKnob?.SetValue((int)Math.Round(bassBoostDb), false);
            virtualizerKnob?.SetValue((int)Math.Round(virtualizerPercent), false);
            subBassKnob?.SetValue((int)Math.Round(subBassDb), false);

            SetVolumeLimit(volumeLimit, save: false);
            Volume.Value = Math.Min(savedVolumePercent, volumeLimit);
            UpdateVolumeToolTip();
            ApplyAudioEnhancementValues();
        }

        private void SaveAudioEnhancementSettings()
        {
            try
            {
                string directory = Path.GetDirectoryName(AudioEnhancementSettingsPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllLines(AudioEnhancementSettingsPath, new[]
                {
                    "BassBoost=" + bassBoostDb.ToString("0", CultureInfo.InvariantCulture),
                    "Virtualizer=" + virtualizerPercent.ToString("0", CultureInfo.InvariantCulture),
                    "SubBass=" + subBassDb.ToString("0", CultureInfo.InvariantCulture),
                    "VolumeLimit=" + volumeLimit.ToString(CultureInfo.InvariantCulture),
                    "VolumePercent=" + Volume.Value.ToString(CultureInfo.InvariantCulture)
                });
            }
            catch
            {
                // Налаштування ефектів необов'язкові; помилка запису не зупиняє плеєр.
            }
        }

        private void ApplyAudioEnhancementValues()
        {
            if (audioEffects == null) return;

            audioEffects.BassBoostDb = bassBoostDb;
            audioEffects.Virtualizer = virtualizerPercent;
            audioEffects.SubBassDb = subBassDb;
        }

        private void UpdateAudioEnhancementsEnabled()
        {
            if (audioEffects != null)
                audioEffects.Enabled = eqEnabled;

            if (bassBoostKnob != null) bassBoostKnob.Enabled = eqEnabled;
            if (virtualizerKnob != null) virtualizerKnob.Enabled = eqEnabled;
            if (subBassKnob != null) subBassKnob.Enabled = eqEnabled;
        }
    }
}