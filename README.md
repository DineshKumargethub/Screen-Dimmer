# ScreenDimmer for Windows

A lightweight Windows utility that dims the whole screen below the normal Windows brightness minimum, with warmth adjustment, presets, hotkeys, and scheduling.

I built ScreenDimmer because the screen-dimming tools I tried felt unreliable or did not behave correctly for my needs. I wanted a simple utility that was dependable, straightforward to use, and brought the controls I wanted together in one place.

## Features

- Dimming from 0–95%; 0% means normal Windows appearance.
- Warmth slider to reduce blue light for night use.
- Presets, system-tray menu, and single-instance behaviour.
- Automatic dimming schedule, including schedules that cross midnight.
- Optional start with Windows and start hidden in the tray.
- **Customizable global hotkeys** (click **Customize hotkeys** in the app). Defaults: `Ctrl+Alt+D` dim more, `Ctrl+Alt+W` dim less, `Ctrl+Alt+T` toggle dimming.
- Closing the window hides it in the tray; choose **Exit** from the tray menu to quit.
- Restores the normal screen appearance when the application exits.

## Requirements

- Windows 10 or Windows 11 (64-bit).
- Nothing else: the released `ScreenDimmer.exe` is **self-contained** and includes the .NET runtime, so no separate .NET install is needed.

## Build from source

1. Install the .NET SDK (8 or newer). It is only needed to build, not to run the app.
2. Run `build.bat` from a Windows command prompt.
3. The self-contained single-file executable is written to `dist\ScreenDimmer.exe`.

You can also build it without installing anything locally: push the repository to GitHub and run the **build-exe** workflow in the Actions tab.

Windows SmartScreen may warn about an unsigned exe: choose *More info → Run anyway*.

## Settings and exit

Settings, including your hotkeys, are stored in `%AppData%\ScreenDimmer\settings.txt`. Closing the main window hides the app to the system tray. To quit, use **Exit** in the tray menu.

## Author

Created and maintained by **Dineshkumar G.** I am a passionate developer who enjoys building practical software to solve everyday problems and improve usability.

GitHub: https://github.com/DineshKumargethub

## Support the project

If ScreenDimmer is useful to you, you can optionally support its continued development. Donations are voluntary and are not required to use the software. Thank you for trying it!

[Buy me a coffee](https://buymeacoffee.com/dineshkumarg)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
