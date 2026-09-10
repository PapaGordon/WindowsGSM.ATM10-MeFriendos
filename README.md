<p align="center">
  <img src="ATM10.cs/ATM10.png" alt="All the Mods 10" width="128">
</p>

# WindowsGSM.ATM10 — MeFriendos build

[![WindowsGSM](https://img.shields.io/badge/WindowsGSM-%E2%89%A51.21-38CDD4)](https://github.com/WindowsGSM/WindowsGSM)
[![Version](https://img.shields.io/badge/version-0.1.8-7AC943)](CHANGELOG.md)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

WindowsGSM plugin for Minecraft: All the Mods 10 on NeoForge and Minecraft 1.21.1.

The plugin installs a locally supplied official ATM10 server pack, manages the NeoForge runtime and starts the server directly with Java 21.

## Features

- Installs ATM10 from an official local `ServerFiles-*.zip`.
- Detects Java 21 through `JAVA_HOME`, common Windows JDK locations and `PATH`.
- Runs the bundled NeoForge installer.
- Supports the embedded WindowsGSM console and graceful shutdown.
- Writes the WindowsGSM server port, query port and player limit to `server.properties`.
- Preserves worlds, player lists, EULA, server properties and custom `-Xms`/`-Xmx` values during updates.
- Skips a reinstall when the selected server pack is already installed.
- Removes WindowsGSM's automatic firewall application exception for this server before Java starts.

## Requirements

- [WindowsGSM](https://github.com/WindowsGSM/WindowsGSM) 1.21 or newer
- 64-bit Java 21 or newer
- The official ATM10 `ServerFiles-*.zip` from [CurseForge](https://www.curseforge.com/minecraft/modpacks/all-the-mods-10)
- Administrator rights for WindowsGSM

ATM10, NeoForge, Minecraft and the server pack are not distributed with this plugin.

## Plugin installation

1. Download the [latest release archive](https://github.com/PapaGordon/WindowsGSM.ATM10-MeFriendos/releases/latest).
2. Extract the folder `ATM10.cs` into the WindowsGSM `plugins` folder.
3. Click **Reload Plugins** or restart WindowsGSM.
4. Download the matching ATM10 server pack from CurseForge.
5. Place `ServerFiles-<version>.zip` in the WindowsGSM root folder, `plugins\ATM10.cs`, or the target server's `serverfiles` folder.
6. Install **Minecraft: All the Mods 10 (ATM10)** in WindowsGSM and accept the Minecraft EULA prompt.
7. Configure memory in `user_jvm_args.txt`, for example `-Xms8G` and `-Xmx12G`.

## Updating ATM10

1. Stop the server and create a world backup.
2. Download the new official ATM10 `ServerFiles-*.zip`.
3. Place it in one of the supported locations.
4. Click **Update** in WindowsGSM.

The plugin replaces modpack-managed files while preserving server-owned data and memory settings. Client and server ATM10 versions must match.

## Security: automatic port opening is disabled

WindowsGSM may create an automatic application exception for `startserver.bat` before launching the plugin. This build removes that exact program exception before Java starts.

The cleanup uses the same Windows Firewall COM API family (`HNetCfg.FwMgr`) used by WindowsGSM for its own application exceptions and does not depend on PowerShell `NetSecurity` cmdlets.

Manual firewall rules are not created or removed. Rules for other server paths are not selected because the exact program path must match. After removal, the plugin checks the authorized-application list again and stops startup if the exception is still present or the firewall state cannot be verified.

Create only the Minecraft game and query rules required by your setup. Keep administrative services restricted to a Private VPN or trusted subnet.

## Troubleshooting

### Java 21 was not found

Install a 64-bit Java 21 JDK. Set `JAVA_HOME` or add its `bin` directory to `PATH`. Common locations such as `C:\Program Files\Java\jdk-21*` are checked automatically.

### ATM10 server pack was not found

Make sure you downloaded the separate official server pack named `ServerFiles-*.zip`, not the normal client modpack.

### NeoForge runtime was not found

Run **Install** or **Update** again and inspect the WindowsGSM console for errors from the NeoForge installer.

## Testing checklist

- WindowsGSM loads `ATM10.cs` without a plugin error.
- Java 21 and the NeoForge `win_args.txt` are detected.
- Install and Update finish without a stuck WindowsGSM progress state.
- Existing `-Xms` and `-Xmx` values remain unchanged after Install/Update.
- The embedded console receives server output and the **Stop** action shuts down cleanly.
- No automatic application exception remains for this server's `startserver.bat` after startup.
- Manually configured port rules remain present.
- A firewall-cleanup failure prevents the server process from starting.

## Project links

- Source: [PapaGordon/WindowsGSM.ATM10-MeFriendos](https://github.com/PapaGordon/WindowsGSM.ATM10-MeFriendos)
- ATM10: [CurseForge](https://www.curseforge.com/minecraft/modpacks/all-the-mods-10)
- Community: [mefriendos.de](https://mefriendos.de)

This is an independent community plugin. It is not affiliated with or endorsed by the ATM Team, CurseForge, NeoForge, Mojang, Microsoft or WindowsGSM.

## License

Released under the [MIT License](LICENSE).
