# Game Master

Game Master is a life-gamification hub application that manages a single player's game state (points, XP, inventory) across devices and exposes that state to other "connected apps" via a granular permissions system.

## Prerequisites
- .NET 8 or 9 SDK
- Avalonia UI for the desktop application
- Android SDK for the Android application

## How to Build
To build the solution, navigate to the `C#` directory and run:

```bash
dotnet build GameMaster.sln
```

## How to pair devices
Devices can be paired over LAN.
1. Run `GameMaster.Desktop` on both devices.
2. In the REPL terminal on Device A, type `sync pair`. This will generate a 6-digit alphanumeric code valid for 5 minutes.
3. On Device B, enter the code in the REPL or UI to complete the pairing handshake.
4. The devices will automatically synchronize game state whenever they are on the same LAN using mDNS discovery.

## Enabling Auto-Start on Linux
A systemd user service is provided to launch the desktop application automatically on boot. To enable it:
1. Copy the `gamemaster.service` file to `~/.config/systemd/user/`:
```bash
mkdir -p ~/.config/systemd/user/
cp gamemaster.service ~/.config/systemd/user/
```
2. Enable and start the service:
```bash
systemctl --user enable gamemaster.service
systemctl --user start gamemaster.service
```
