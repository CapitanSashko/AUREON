# AUREON

**A modern Windows music player built with C# and .NET 8.**

AUREON is a desktop music player designed to provide a convenient music listening experience with audio processing, playlist management and a modern interface.

## Features

- Support for MP3, FLAC, WAV, AIFF, WMA, M4A, AAC and MP4 audio files.
- Playlist management.
- Audio equalizer.
- Playback speed control.
- Shuffle and repeat playback.
- Volume and balance control.
- Audio spectrum visualization.
- Smooth track transitions.
- Integrated music services through WebView2.

## Screenshots

### Main Window

![AUREON Main Window](Screenshots/main-window.png)

### Playlist

![AUREON Playlist](Screenshots/playlist.png)

### Equalizer

![AUREON Equalizer](Screenshots/equalizer.png)

## Technologies

- C#
- .NET 8
- Windows Forms
- NAudio
- SoundTouch
- TagLibSharp
- Microsoft Edge WebView2

## System Requirements

- Windows 10 or Windows 11, x64.
- Microsoft Edge WebView2 Runtime.
- .NET 8 Desktop Runtime is required for framework-dependent builds.

## Download

Download the latest version from [GitHub Releases](../../releases).

## Build from Source

1. Install Visual Studio with the .NET desktop development workload.
2. Clone this repository:

   ```bash
   git clone https://github.com/CapitanSashko/AUREON.git
   ```

3. Open `AUREON.sln` in Visual Studio.
4. Restore NuGet packages.
5. Build the project using the Release configuration.

## License

AUREON is distributed under the MIT License. See [LICENSE](LICENSE) for details.

## Author

Developed by Oleksandr Echkenko.
