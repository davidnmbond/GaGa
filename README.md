
## About

Soma FM is a fork of the GaGa app, specifically for Soma FM.

When run, a System Tray Icon appears with the Soma FM "s" in one of the following colors:
- Red: music is playing.
- Red: music is playing, but muted.
- Green: no music is playing.

Your regular multi-media controls should work (pause, play, mute, volume up and down). 

In Windows 10, to make the icon permanently visible, just click the ^ and drag the "s" to the system tray area.

## Known issues

- At present, there is no automatic start of your favourite Soma FM station.  This will follow.
- At present, quitting the app causes a crash, as your preferences are not correctly saved.

## The streams file

GaGa was designed to make it convenient to add, move or share radio stations
between users using an INI file and Soma FM continues to permit this.  This file is automatically
reloaded on changes and clicking "Edit streams file" opens it in your
default editor for the .ini extension.

## Compiling and installation

Building SomFm is a matter of opening the included Visual Studio 2015
solution and clicking the build button (or using msbuild). The source code
has no dependencies other than the .NET Framework 4.0+.

There are binaries for the latest version in the [Releases][] tab above.

Soma FM uses a WiX installer.

## Portability

Soma FM is tested on Windows 10, using the .NET Framework 4.0+.
Mono is not supported, because it doesn't implement MediaPlayer (in PresentationCore) which is used for playback.

## Status

This project is just starting.  Please let me know if you would like to contribute.

## License

This is Free Software.   You can fork and modify as you see fit. See the [Documentation][] folder for more information. No warranty though.

[Documentation]: Documentation
[Releases]: https://github.com/davidnmbond/SomaFm/releases
