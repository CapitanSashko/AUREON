using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace MP3
{
    internal static class Program
    {
        private static readonly string UserKey = new string(
            Environment.UserName.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

        internal static readonly string PipeName = "AUREON_MP3_Player_" + UserKey;
        private static readonly string MutexName = @"Local\AUREON_MP3_Player_" + UserKey;

        internal static string[] StartupFiles { get; private set; } = Array.Empty<string>();

        private static readonly string[] SupportedExtensions =
        {
            ".mp3", ".wav", ".aiff", ".aif", ".wma",
            ".m4a", ".aac", ".mp4", ".flac"
        };

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        [STAThread]
        private static void Main(string[] args)
        {
            using var mutex = new Mutex(false, MutexName);
            bool ownsMutex;

            try
            {
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // Попередній екземпляр аварійно завершився — цей процес отримав mutex.
                ownsMutex = true;
            }

            if (!ownsMutex)
            {
                string[] requestedFiles = FilterAudioPaths(args);

                // Пробуємо передати файли вже запущеному екземпляру
                if (TrySendToRunningInstance(requestedFiles))
                    return;

                // Якщо не вийшло (наприклад, перший екземпляр "завис" або не слухає pipe),
                // примусово закриваємо попередній процес і запускаємо новий.
                try
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName("AUREON");
                    foreach (var p in processes)
                    {
                        if (p.Id != System.Diagnostics.Process.GetCurrentProcess().Id)
                        {
                            p.Kill();
                            p.WaitForExit(1000);
                        }
                    }
                }
                catch { /* Ігноруємо помилки видалення */ }

                // Тепер ми — єдиний екземпляр. Продовжуємо запуск.
                ownsMutex = true;
            }

            StartupFiles = FilterAudioPaths(args);

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

        internal static bool IsSupportedAudioFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        private static string[] FilterAudioPaths(string[] args)
        {
            if (args == null || args.Length == 0)
                return Array.Empty<string>();

            return args
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim().Trim('"'))
                .Where(path => File.Exists(path) && IsSupportedAudioFile(path))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool TrySendToRunningInstance(string[] files)
        {
            string payload = JsonSerializer.Serialize(files ?? Array.Empty<string>());

            // Даємо першому екземпляру час завершити запуск форми та створити pipe-сервер.
            // 50 спроб по 200 мс = 10 секунд максимального очікування.
            const int attempts = 50;

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    // Дозволяємо першому екземпляру вийти на передній план
                    AllowSetForegroundWindow(-1);

                    using var client = new NamedPipeClientStream(
                        ".", PipeName, PipeDirection.Out, PipeOptions.None);

                    // Збільшено таймаут підключення до 1000 мс
                    client.Connect(1000);

                    using var writer = new StreamWriter(client, new UTF8Encoding(false));
                    writer.WriteLine(payload);
                    writer.Flush();
                    return true;
                }
                catch (TimeoutException)
                {
                    Thread.Sleep(200);
                }
                catch (IOException)
                {
                    Thread.Sleep(200);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(200);
                }
                catch (Exception)
                {
                    Thread.Sleep(200);
                }
            }

            return false;
        }
    }
}