namespace MP3
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            pictureCover = new CoverBox();
            BackButton = new RoundButton();
            PauseButton = new RoundButton();
            ForwardButton = new RoundButton();
            StopButton = new RoundButton();
            VolumeLabel = new Label();
            TimerLabel = new Label();
            ListPlayer = new ModernListBox();
            contextMenuStripListPlayer = new ContextMenuStrip(components);
            добавитьПлейлистToolStripMenuItem = new ToolStripMenuItem();
            очиститьПлейлистToolStripMenuItem = new ToolStripMenuItem();
            TimerMusicComponent = new System.Windows.Forms.Timer(components);
            openFileDialog = new OpenFileDialog();
            Volume = new ModernSlider();
            progressBarMusic = new ModernSlider();
            AllTracksTime = new Label();
            NameTrack = new Label();
            repeatTrack = new RoundButton();
            noRepeatTrack = new RoundButton();
            AddPlayList = new RoundButton();
            DeletePlayList = new RoundButton();
            toolTip = new ToolTip(components);
            yesshuffleTrack = new RoundButton();
            nosshuffleTrack = new RoundButton();
            spectrumView = new SpectrumView();
            eqButton = new RoundButton();
            eqPresetButton = new RoundButton();
            eqToggleButton = new RoundButton();
            eqPanel = new CardPanel();
            equalizerView = new EqualizerView();
            ((System.ComponentModel.ISupportInitialize)pictureCover).BeginInit();
            contextMenuStripListPlayer.SuspendLayout();
            eqPanel.SuspendLayout();
            SuspendLayout();
            // 
            // pictureCover
            // 
            pictureCover.BackColor = Color.FromArgb(32, 32, 38);
            pictureCover.Location = new Point(0, 0);
            pictureCover.Name = "pictureCover";
            pictureCover.Size = new Size(350, 210);
            pictureCover.SizeMode = PictureBoxSizeMode.Zoom;
            pictureCover.TabIndex = 1;
            pictureCover.TabStop = false;
            pictureCover.Click += pictureCover_Click;
            pictureCover.DoubleClick += pictureCover_DoubleClick;
            // 
            // BackButton
            // 
            BackButton.BackColor = Color.Transparent;
            BackButton.Font = new Font("Segoe MDL2 Assets", 13F);
            BackButton.ForeColor = Color.FromArgb(235, 235, 238);
            BackButton.Kind = ButtonKind.Soft;
            BackButton.Location = new Point(10, 238);
            BackButton.Name = "BackButton";
            BackButton.Radius = 14;
            BackButton.Size = new Size(43, 34);
            BackButton.TabIndex = 2;
            BackButton.TabStop = false;
            BackButton.Text = "";
            toolTip.SetToolTip(BackButton, "Попередній трек");
            BackButton.Click += BackButton_Click;
            // 
            // PauseButton
            // 
            PauseButton.BackColor = Color.Transparent;
            PauseButton.Font = new Font("Segoe MDL2 Assets", 16F);
            PauseButton.ForeColor = Color.White;
            PauseButton.Kind = ButtonKind.Accent;
            PauseButton.Location = new Point(108, 238);
            PauseButton.Name = "PauseButton";
            PauseButton.Radius = 24;
            PauseButton.Size = new Size(43, 34);
            PauseButton.TabIndex = 3;
            PauseButton.TabStop = false;
            PauseButton.Text = "";
            toolTip.SetToolTip(PauseButton, "Пауза / відтворення");
            PauseButton.Click += PauseButton_Click;
            // 
            // ForwardButton
            // 
            ForwardButton.BackColor = Color.Transparent;
            ForwardButton.Font = new Font("Segoe MDL2 Assets", 13F);
            ForwardButton.ForeColor = Color.FromArgb(235, 235, 238);
            ForwardButton.Kind = ButtonKind.Soft;
            ForwardButton.Location = new Point(59, 238);
            ForwardButton.Name = "ForwardButton";
            ForwardButton.Radius = 14;
            ForwardButton.Size = new Size(43, 34);
            ForwardButton.TabIndex = 4;
            ForwardButton.TabStop = false;
            ForwardButton.Text = "";
            toolTip.SetToolTip(ForwardButton, "Наступний трек");
            ForwardButton.Click += ForwardButton_Click;
            // 
            // StopButton
            // 
            StopButton.BackColor = Color.Transparent;
            StopButton.Font = new Font("Segoe MDL2 Assets", 13F);
            StopButton.ForeColor = Color.FromArgb(235, 235, 238);
            StopButton.Kind = ButtonKind.Soft;
            StopButton.Location = new Point(157, 238);
            StopButton.Name = "StopButton";
            StopButton.Radius = 14;
            StopButton.Size = new Size(43, 34);
            StopButton.TabIndex = 5;
            StopButton.TabStop = false;
            StopButton.Text = "";
            toolTip.SetToolTip(StopButton, "Зупинити");
            StopButton.Click += StopButton_Click;
            // 
            // VolumeLabel
            // 
            VolumeLabel.AutoSize = true;
            VolumeLabel.Font = new Font("Segoe MDL2 Assets", 13F);
            VolumeLabel.ForeColor = Color.FromArgb(150, 150, 160);
            VolumeLabel.Location = new Point(210, 246);
            VolumeLabel.Name = "VolumeLabel";
            VolumeLabel.Size = new Size(26, 18);
            VolumeLabel.TabIndex = 6;
            VolumeLabel.Text = "";
            // 
            // TimerLabel
            // 
            TimerLabel.AutoSize = true;
            TimerLabel.Font = new Font("Segoe UI", 9.5F);
            TimerLabel.ForeColor = Color.FromArgb(150, 150, 160);
            TimerLabel.Location = new Point(8, 284);
            TimerLabel.Name = "TimerLabel";
            TimerLabel.Size = new Size(56, 17);
            TimerLabel.TabIndex = 8;
            TimerLabel.Text = "00:00:00";
            // 
            // ListPlayer
            // 
            ListPlayer.AllowDrop = true;
            ListPlayer.BackColor = Color.FromArgb(30, 30, 35);
            ListPlayer.BorderStyle = BorderStyle.None;
            ListPlayer.ContextMenuStrip = contextMenuStripListPlayer;
            ListPlayer.DrawMode = DrawMode.OwnerDrawFixed;
            ListPlayer.Font = new Font("Segoe UI", 10F);
            ListPlayer.ForeColor = Color.FromArgb(225, 225, 230);
            ListPlayer.FormattingEnabled = true;
            ListPlayer.IntegralHeight = false;
            ListPlayer.ItemHeight = 34;
            ListPlayer.Location = new Point(0, 377);
            ListPlayer.Name = "ListPlayer";
            ListPlayer.Size = new Size(350, 165);
            ListPlayer.TabIndex = 10;
            ListPlayer.MouseClick += ListPlayer_MouseClick;
            ListPlayer.SelectedIndexChanged += ListPlayer_SelectedIndexChanged;
            ListPlayer.DragDrop += ListPlayer_DragDrop;
            ListPlayer.DragEnter += ListPlayer_DragEnter;
            // 
            // contextMenuStripListPlayer
            // 
            contextMenuStripListPlayer.Font = new Font("Segoe UI", 9.5F);
            contextMenuStripListPlayer.ImageScalingSize = new Size(20, 20);
            contextMenuStripListPlayer.Items.AddRange(new ToolStripItem[] { добавитьПлейлистToolStripMenuItem, очиститьПлейлистToolStripMenuItem });
            contextMenuStripListPlayer.Name = "contextMenuStripListPlayer";
            contextMenuStripListPlayer.ShowImageMargin = false;
            contextMenuStripListPlayer.Size = new Size(163, 48);
            // 
            // добавитьПлейлистToolStripMenuItem
            // 
            добавитьПлейлистToolStripMenuItem.Name = "добавитьПлейлистToolStripMenuItem";
            добавитьПлейлистToolStripMenuItem.Size = new Size(162, 22);
            добавитьПлейлистToolStripMenuItem.Text = "Додати плейлист";
            добавитьПлейлистToolStripMenuItem.Click += добавитьПлейлистToolStripMenuItem_Click;
            // 
            // очиститьПлейлистToolStripMenuItem
            // 
            очиститьПлейлистToolStripMenuItem.Name = "очиститьПлейлистToolStripMenuItem";
            очиститьПлейлистToolStripMenuItem.Size = new Size(162, 22);
            очиститьПлейлистToolStripMenuItem.Text = "Видалити плейлист";
            очиститьПлейлистToolStripMenuItem.Click += очиститьПлейлистToolStripMenuItem_Click;
            // 
            // TimerMusicComponent
            // 
            TimerMusicComponent.Tick += TimerMusicComponent_Tick;
            // 
            // openFileDialog
            // 
            openFileDialog.FileName = "Виберіть, будь ласка, файли";
            openFileDialog.Filter = "\"MP3 Files|*.mp3|All Files|*.*\";";
            openFileDialog.Multiselect = true;
            // 
            // Volume
            // 
            Volume.BackColor = Color.Transparent;
            Volume.Location = new Point(235, 244);
            Volume.Maximum = 1000;
            Volume.Name = "Volume";
            Volume.Size = new Size(105, 21);
            Volume.TabIndex = 11;
            Volume.TabStop = false;
            Volume.Value = 1000;
            Volume.Scroll += trackBar1_Scroll;
            // 
            // progressBarMusic
            // 
            progressBarMusic.BackColor = Color.Transparent;
            progressBarMusic.Location = new Point(75, 281);
            progressBarMusic.Name = "progressBarMusic";
            progressBarMusic.Size = new Size(265, 21);
            progressBarMusic.TabIndex = 12;
            progressBarMusic.TabStop = false;
            progressBarMusic.Scroll += progressBarMusic_Scroll;
            progressBarMusic.MouseDown += progressBarMusic_MouseDown;
            progressBarMusic.MouseUp += progressBarMusic_MouseUp;
            // 
            // AllTracksTime
            // 
            AllTracksTime.AutoSize = true;
            AllTracksTime.Font = new Font("Segoe UI", 9F);
            AllTracksTime.ForeColor = Color.FromArgb(150, 150, 160);
            AllTracksTime.Location = new Point(8, 310);
            AllTracksTime.Name = "AllTracksTime";
            AllTracksTime.Size = new Size(167, 15);
            AllTracksTime.TabIndex = 13;
            AllTracksTime.Text = "Усього N доріжок - N хвилин";
            // 
            // NameTrack
            // 
            NameTrack.AutoEllipsis = true;
            NameTrack.Font = new Font("Segoe UI Semibold", 10F);
            NameTrack.ForeColor = Color.FromArgb(235, 235, 238);
            NameTrack.Location = new Point(8, 213);
            NameTrack.Name = "NameTrack";
            NameTrack.Size = new Size(334, 19);
            NameTrack.TabIndex = 14;
            NameTrack.Text = "Немає треку!";
            NameTrack.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // repeatTrack
            // 
            repeatTrack.BackColor = Color.Transparent;
            repeatTrack.Font = new Font("Segoe MDL2 Assets", 13F);
            repeatTrack.ForeColor = Color.FromArgb(200, 200, 208);
            repeatTrack.Location = new Point(8, 551);
            repeatTrack.Name = "repeatTrack";
            repeatTrack.Size = new Size(43, 31);
            repeatTrack.TabIndex = 15;
            repeatTrack.TabStop = false;
            repeatTrack.Text = "";
            toolTip.SetToolTip(repeatTrack, "Повторювати трек");
            repeatTrack.Click += repeatTrack_Click;
            // 
            // noRepeatTrack
            // 
            noRepeatTrack.BackColor = Color.Transparent;
            noRepeatTrack.Font = new Font("Segoe MDL2 Assets", 13F);
            noRepeatTrack.ForeColor = Color.FromArgb(76, 160, 255);
            noRepeatTrack.Location = new Point(8, 551);
            noRepeatTrack.Name = "noRepeatTrack";
            noRepeatTrack.Size = new Size(43, 31);
            noRepeatTrack.TabIndex = 16;
            noRepeatTrack.TabStop = false;
            noRepeatTrack.Text = "";
            toolTip.SetToolTip(noRepeatTrack, "Вимкнути повтор");
            noRepeatTrack.Visible = false;
            noRepeatTrack.Click += noRepeatTrack_Click;
            // 
            // AddPlayList
            // 
            AddPlayList.BackColor = Color.Transparent;
            AddPlayList.Font = new Font("Segoe MDL2 Assets", 13F);
            AddPlayList.ForeColor = Color.FromArgb(200, 200, 208);
            AddPlayList.Location = new Point(251, 551);
            AddPlayList.Name = "AddPlayList";
            AddPlayList.Size = new Size(43, 31);
            AddPlayList.TabIndex = 18;
            AddPlayList.TabStop = false;
            AddPlayList.Text = "";
            toolTip.SetToolTip(AddPlayList, "Додати плейлист");
            AddPlayList.Click += AddPlayList_Click;
            // 
            // DeletePlayList
            // 
            DeletePlayList.BackColor = Color.Transparent;
            DeletePlayList.Font = new Font("Segoe MDL2 Assets", 13F);
            DeletePlayList.ForeColor = Color.FromArgb(200, 200, 208);
            DeletePlayList.Location = new Point(300, 551);
            DeletePlayList.Name = "DeletePlayList";
            DeletePlayList.Size = new Size(43, 31);
            DeletePlayList.TabIndex = 19;
            DeletePlayList.TabStop = false;
            DeletePlayList.Text = "";
            toolTip.SetToolTip(DeletePlayList, "Очистити плейлист");
            DeletePlayList.Click += DeletePlayList_Click;
            // 
            // yesshuffleTrack
            // 
            yesshuffleTrack.BackColor = Color.Transparent;
            yesshuffleTrack.Font = new Font("Segoe MDL2 Assets", 13F);
            yesshuffleTrack.ForeColor = Color.FromArgb(200, 200, 208);
            yesshuffleTrack.Location = new Point(57, 551);
            yesshuffleTrack.Name = "yesshuffleTrack";
            yesshuffleTrack.Size = new Size(43, 31);
            yesshuffleTrack.TabIndex = 20;
            yesshuffleTrack.TabStop = false;
            yesshuffleTrack.Text = "";
            toolTip.SetToolTip(yesshuffleTrack, "Увімкнути випадкове відтворення");
            yesshuffleTrack.Click += yesshuffleTrack_Click;
            // 
            // nosshuffleTrack
            // 
            nosshuffleTrack.BackColor = Color.Transparent;
            nosshuffleTrack.Font = new Font("Segoe MDL2 Assets", 13F);
            nosshuffleTrack.ForeColor = Color.FromArgb(76, 160, 255);
            nosshuffleTrack.Location = new Point(57, 551);
            nosshuffleTrack.Name = "nosshuffleTrack";
            nosshuffleTrack.Size = new Size(43, 31);
            nosshuffleTrack.TabIndex = 21;
            nosshuffleTrack.TabStop = false;
            nosshuffleTrack.Text = "";
            toolTip.SetToolTip(nosshuffleTrack, "Вимкнути випадкове відтворення");
            nosshuffleTrack.Visible = false;
            nosshuffleTrack.Click += nosshuffleTrack_Click;
            // 
            // spectrumView
            // 
            spectrumView.Location = new Point(8, 330);
            spectrumView.Name = "spectrumView";
            spectrumView.Size = new Size(284, 44);
            spectrumView.TabIndex = 22;
            spectrumView.TabStop = false;
            toolTip.SetToolTip(spectrumView, "Візуалізатор");
            // 
            // eqButton
            // 
            eqButton.BackColor = Color.Transparent;
            eqButton.Font = new Font("Segoe UI Semibold", 9.5F);
            eqButton.ForeColor = Color.FromArgb(235, 235, 238);
            eqButton.Kind = ButtonKind.Soft;
            eqButton.Location = new Point(300, 330);
            eqButton.Name = "eqButton";
            eqButton.Size = new Size(42, 44);
            eqButton.TabIndex = 23;
            eqButton.TabStop = false;
            eqButton.Text = "EQ";
            toolTip.SetToolTip(eqButton, "Еквалайзер (Ctrl+E)");
            eqButton.Click += eqButton_Click;
            // 
            // eqPresetButton
            // 
            eqPresetButton.BackColor = Color.Transparent;
            eqPresetButton.Font = new Font("Segoe UI", 9F);
            eqPresetButton.ForeColor = Color.FromArgb(235, 235, 238);
            eqPresetButton.Kind = ButtonKind.Soft;
            eqPresetButton.Location = new Point(10, 5);
            eqPresetButton.Name = "eqPresetButton";
            eqPresetButton.Radius = 8;
            eqPresetButton.Size = new Size(84, 23);
            eqPresetButton.TabIndex = 0;
            eqPresetButton.TabStop = false;
            eqPresetButton.Text = "Плоский  ▾";
            toolTip.SetToolTip(eqPresetButton, "Вибрати пресет");
            eqPresetButton.Click += eqPresetButton_Click;
            // 
            // eqToggleButton
            // 
            eqToggleButton.BackColor = Color.Transparent;
            eqToggleButton.Font = new Font("Segoe UI Semibold", 9F);
            eqToggleButton.ForeColor = Color.White;
            eqToggleButton.Kind = ButtonKind.Accent;
            eqToggleButton.Location = new Point(243, 5);
            eqToggleButton.Name = "eqToggleButton";
            eqToggleButton.Radius = 8;
            eqToggleButton.Size = new Size(87, 23);
            eqToggleButton.TabIndex = 1;
            eqToggleButton.TabStop = false;
            eqToggleButton.Text = "Увімк.";
            toolTip.SetToolTip(eqToggleButton, "Увімкнути / вимкнути еквалайзер");
            eqToggleButton.Click += eqToggleButton_Click;
            // 
            // eqPanel
            // 
            eqPanel.BackColor = Color.FromArgb(30, 30, 35);
            eqPanel.Controls.Add(equalizerView);
            eqPanel.Controls.Add(eqToggleButton);
            eqPanel.Controls.Add(eqPresetButton);
            eqPanel.Location = new Point(8, 377);
            eqPanel.Name = "eqPanel";
            eqPanel.Size = new Size(334, 132);
            eqPanel.TabIndex = 24;
            eqPanel.Visible = false;
            // 
            // equalizerView
            // 
            equalizerView.BackColor = Color.Transparent;
            equalizerView.Location = new Point(4, 32);
            equalizerView.Name = "equalizerView";
            equalizerView.Size = new Size(326, 97);
            equalizerView.TabIndex = 2;
            equalizerView.TabStop = false;
            equalizerView.GainsChanged += equalizerView_GainsChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.FromArgb(22, 22, 26);
            ClientSize = new Size(350, 590);
            Controls.Add(eqPanel);
            Controls.Add(eqButton);
            Controls.Add(spectrumView);
            Controls.Add(nosshuffleTrack);
            Controls.Add(yesshuffleTrack);
            Controls.Add(DeletePlayList);
            Controls.Add(AddPlayList);
            Controls.Add(noRepeatTrack);
            Controls.Add(repeatTrack);
            Controls.Add(NameTrack);
            Controls.Add(AllTracksTime);
            Controls.Add(progressBarMusic);
            Controls.Add(Volume);
            Controls.Add(ListPlayer);
            Controls.Add(TimerLabel);
            Controls.Add(VolumeLabel);
            Controls.Add(StopButton);
            Controls.Add(ForwardButton);
            Controls.Add(PauseButton);
            Controls.Add(BackButton);
            Controls.Add(pictureCover);
            ForeColor = Color.FromArgb(235, 235, 238);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(350, 590);
            MinimumSize = new Size(350, 590);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AUREON";
            Load += Form1_Load;
            SizeChanged += Form1_SizeChanged;
            DoubleClick += Form1_DoubleClick;
            ((System.ComponentModel.ISupportInitialize)pictureCover).EndInit();
            contextMenuStripListPlayer.ResumeLayout(false);
            eqPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private CoverBox pictureCover;
        private RoundButton BackButton;
        private RoundButton PauseButton;
        private RoundButton ForwardButton;
        private RoundButton StopButton;
        private Label VolumeLabel;
        private Label TimerLabel;
        private ModernListBox ListPlayer;
        private System.Windows.Forms.Timer TimerMusicComponent;
        private OpenFileDialog openFileDialog;
        private ModernSlider Volume;
        private ModernSlider progressBarMusic;
        private ContextMenuStrip contextMenuStripListPlayer;
        private ToolStripMenuItem очиститьПлейлистToolStripMenuItem;
        private ToolStripMenuItem добавитьПлейлистToolStripMenuItem;
        private Label AllTracksTime;
        private Label NameTrack;
        private RoundButton repeatTrack;
        private RoundButton noRepeatTrack;
        private RoundButton AddPlayList;
        private RoundButton DeletePlayList;
        private ToolTip toolTip;
        private RoundButton yesshuffleTrack;
        private RoundButton nosshuffleTrack;
        private SpectrumView spectrumView;
        private RoundButton eqButton;
        private CardPanel eqPanel;
        private RoundButton eqPresetButton;
        private RoundButton eqToggleButton;
        private EqualizerView equalizerView;
    }
}