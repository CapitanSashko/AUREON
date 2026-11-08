# AUREON Audio Player

[English](README.md) | [Українська](README.uk.md)

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?logo=windows)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet)
![Language](https://img.shields.io/badge/language-C%23-239120?logo=csharp)
![License](https://img.shields.io/badge/license-MIT-green)

**A modern Windows music player built with C# and .NET 8.**

AUREON is a compact desktop player for your local music collection. It combines a 10-band equalizer with bass, stereo-width and sub-bass controls, pitch-preserving speed control, tabbed playlists, cover art, track notifications and built-in access to online music services. All of it lives in a dark, borderless window that collapses into a small mini-player when you need it out of the way.

## Features

### Playback
- Supports MP3, FLAC, WAV, AIFF, WMA, M4A, AAC and MP4 audio files.
- Smooth fade-in and fade-out between tracks.
- Shuffle and repeat modes.
- Playback speed from 0.5× to 2× **without changing pitch** (SoundTouch).
- Volume up to 150% with a configurable limit (100%, 125% or 150%).
- Left/right balance control.
- Real-time audio spectrum visualization.

### Sound
- 10-band equalizer with ready-made presets (Flat, Bass, Treble, Vocal, Rock, Pop, Jazz).
- Save, apply and delete your own presets.
- Three effect knobs: **Bass Boost**, **Stereo Widening** (virtualizer) and **Sub-bass**.
- One switch to turn the equalizer and all effects on or off.

### Library and playlists
- Playlists as tabs: add a folder, create an empty playlist, rename, clear or close tabs.
- Drag and drop files or whole folders onto the list or the tab bar.
- Instant search inside the current playlist.
- Star ratings (1–5) for every track and total playlist duration.
- Playlists, ratings and settings are saved automatically.
- Open audio files with AUREON from Explorer: if it is set as the default player, files are added to the playlist.

### Interface
- Custom dark window with rounded corners and a built-in title bar.
- Cover art from the track tags or from image files in the folder (`cover.jpg`, `folder.jpg`, an `Artwork` subfolder, etc.).
- Pop-up notification with cover, title, artist and album when the track changes.
- Mini-player mode: double-click the cover to collapse, double-click again to restore.
- Keyboard shortcuts for the main actions (see below).

### Online music
- YouTube Music, Spotify and Apple Music inside the player via Microsoft Edge WebView2.
- Switch services from the context menu of the ♫ button in the title bar.
- Sign-in data is kept in a local WebView2 profile on your PC and can be wiped from the same menu.

## Screenshots

### Main Window
<img width="346" height="649" alt="1" src="https://github.com/user-attachments/assets/13b46bd2-f702-4601-836b-78a474bed56f" />

### Playlist

<img width="325" height="169" alt="image" src="https://github.com/user-attachments/assets/ab4b89d1-0119-42a3-ac16-f150992ff8fd" />

### Equalizer

<img width="331" height="241" alt="image" src="https://github.com/user-attachments/assets/2e297646-bcc1-4acd-b35e-ca0e642e8ddd" />

### Collapsed view

<img width="346" height="137" alt="image" src="https://github.com/user-attachments/assets/fcc81184-a54e-4ae2-b9fe-07522e44ed8f" />

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Space` | Play / pause |
| `Ctrl` + `→` / `Ctrl` + `←` | Next / previous track |
| `Ctrl` + `Tab` / `Ctrl` + `Shift` + `Tab` | Next / previous playlist tab |
| `Ctrl` + `O` | Add tracks |
| `Ctrl` + `E` | Show / hide the equalizer |
| `Ctrl` + `R` | Toggle shuffle |
| `Ctrl` + `T` | Toggle track notifications |
| `Ctrl` + `Delete` | Clear the current playlist |
| `Esc` | Stop playback |

## System Requirements

- Windows 10 or Windows 11, x64.
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (needed for the online services).
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) for framework-dependent builds.

## Download

Download the latest version from [GitHub Releases](../../releases).

## Build from Source

1. Install Visual Studio with the **.NET desktop development** workload.
2. Clone this repository:

   ```bash
   git clone https://github.com/CapitanSashko/AUREON.git
   ```

3. Open `MP3 Play.sln` in Visual Studio.
4. Restore NuGet packages.
5. Build the project using the **Release** configuration.

## Where AUREON Stores Data

Settings are stored next to the executable, in plain text files:

| Folder / file | Contents |
|---|---|
| `playlist/` | Playlists (`.m3u8`), the tab list and track ratings |
| `equalizer/` | Equalizer settings, custom presets and effect values |
| `toast.cfg` | Track notification on/off |

The WebView2 profile (online services) is kept in `%LocalAppData%\AUREON\webview2`.

## Technologies

- C# and .NET 8
- Windows Forms
- [NAudio](https://github.com/naudio/NAudio): audio playback and processing
- [SoundTouch.Net](https://github.com/owoudenberg/soundtouch.net): tempo change with preserved pitch
- [TagLib#](https://github.com/mono/taglib-sharp): tags, cover art and durations
- Microsoft Edge WebView2: online music services

## Feedback

Found a bug or have an idea? Please open an [issue](../../issues).

## License

AUREON is distributed under the MIT License. See [LICENSE](https://github.com/CapitanSashko/AUREON/blob/main/MP3/LICENSE) for details.

## Author

Developed by Oleksandr Echkenko.
