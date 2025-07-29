# SlippiComboRenderer
A project to render out live button sequences as they occur in a tracked Dolphin instance. This is primarily meant to be used as a stream overlay, but has some
functionality built in to launch replays and automatically record queues of clips with OBS.

Built on top of [Slippi.NET](https://github.com/asundheim/Slippi.NET)

![](docs/sample.gif)

**Note: this only works with Fox. It can be made to work with other characters and was designed to be extensible.
Please reach out if you'd like to help by adding other characters.**

# To use
Download the latest release and run `ComboRenderer.exe`. 

On first launch the settings window will open, on subsequent launches it will start collapsed in the taskbar.
Click the icon in the taskbar tray to change settings or quit.

It will eagerly connect to the first Dolphin it finds, so ensure that only one instance of Dolphin
is open when launching or reconnecting.

Set display name and connect code filters to handle ambiguities with dittos.

# Development
## Build
Build the `ComboRenderer` project with Visual Studio by opening the solution file at the root of the project or directly invoke `msbuild`.

## Publish
Publish with `dotnet publish -p:PublishProfile=FolderProfile .\ComboRenderer.csproj`. For some reason VS publish does not like this setup.