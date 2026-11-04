using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MP3
{
    public partial class Form1
    {
        private CancellationTokenSource singleInstancePipeCancellation;
        private Task singleInstancePipeTask;

        /// <summary>
        /// Запускає сервер, який приймає файли від наступних запусків AUREON.
        /// Викликається один раз із Form1_Load.
        /// </summary>
        private void StartSingleInstancePipe()
        {
            if (singleInstancePipeTask != null)
                return;

            singleInstancePipeCancellation = new CancellationTokenSource();
            CancellationToken token = singleInstancePipeCancellation.Token;

            singleInstancePipeTask = Task.Run(() => RunSingleInstancePipeAsync(token));

            FormClosed += (s, e) =>
            {
                try { singleInstancePipeCancellation?.Cancel(); }
                catch { }
            };
        }

        private async Task RunSingleInstancePipeAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        Program.PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                    using var reader = new StreamReader(server, Encoding.UTF8);
                    string line = await reader.ReadLineAsync(token).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] paths;
                    try
                    {
                        paths = JsonSerializer.Deserialize<string[]>(line) ?? Array.Empty<string>();
                    }
                    catch (JsonException)
                    {
                        continue;
                    }

                    if (token.IsCancellationRequested || IsDisposed || !IsHandleCreated)
                        continue;

                    try
                    {
                        BeginInvoke(new Action(() => HandleExternalAudioFiles(paths)));
                    }
                    catch (ObjectDisposedException)
                    {
                        // Форма вже закрита.
                    }
                    catch (InvalidOperationException)
                    {
                        // Форма вже закривається.
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    try
                    {
                        await Task.Delay(250, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Додає отримані файли до активної вкладки, запускає перший переданий трек
        /// та повертає вікно плеєра на передній план.
        /// </summary>
        private void HandleExternalAudioFiles(string[] paths)
        {
            if (IsDisposed || Disposing)
                return;

            BringPlayerToFront();

            if (paths == null || paths.Length == 0)
                return;

            string[] validPaths = paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim().Trim('"'))
                .Where(path => File.Exists(path) && IsSupportedAudioFile(path))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (validPaths.Length == 0)
                return;

            try
            {
                AddFilesToViewList(validPaths);

                SavePlaylist(viewList);
                SaveTabs();
                RefreshTabs();

                // Запускаємо саме перший переданий файл, навіть якщо він уже був у плейлисті.
                string firstPath = validPaths[0];
                int index = viewList.Tracks.FindIndex(track =>
                    string.Equals(
                        Path.GetFullPath(track.FilePath),
                        firstPath,
                        StringComparison.OrdinalIgnoreCase));

                if (index >= 0)
                    SelectAndPlay(index);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не вдалося відкрити передані аудіофайли:\n" + ex.Message,
                    "AUREON MP3 Player",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BringPlayerToFront()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;

            if (isCustomSize)
                RestoreWindow();

            if (!Visible)
                Show();

            Activate();
            BringToFront();
        }
    }
}
