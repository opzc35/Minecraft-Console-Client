# Client Console

This is a small WinForms wrapper for `MinecraftClient.exe`.

## Build

Open `MinecraftClientGUI.sln` on Windows with Visual Studio or MSBuild that supports .NET Framework 4.8, then build the `Release|x86` configuration.

The output executable is `ClientConsole.exe`. Put `MinecraftClient.exe` in the same directory before running it.

## Use

- Enter an account, optional password, and server address.
- Click `Connect` to open a session tab.
- Use the bottom input to send text or commands to the active session.
- Enable `All sessions` to send to every connected session.
- Edit `macros.txt` next to the executable to customize the action buttons.

Passwords are not saved unless `Remember` is checked.
