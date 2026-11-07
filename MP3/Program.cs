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
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
