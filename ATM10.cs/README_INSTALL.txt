VERSION 0.1.7
Java detection now checks JAVA_HOME and common Java 21 JDK install folders (including C:\Program Files\Java\jdk-21*) before PATH.

WindowsGSM.ATM10 - MeFriendos build 0.1.7
==========================================

Purpose
-------
WindowsGSM plugin for Minecraft All the Mods 10 (ATM10) on NeoForge / Minecraft 1.21.1.

Why the server pack is not bundled/downloaded automatically
-------------------------------------------------------------
ATM10 server files are distributed by the ATM Team through CurseForge and are not included in this plugin.
CurseForge requires authenticated API access for programmatic CDN downloads in 2026, so this plugin intentionally uses a locally downloaded official ServerFiles-*.zip instead of embedding an API key or scraping a protected download.

Requirements
------------
- WindowsGSM 1.21+ / 1.23.x style plugin support
- 64-bit Java 21 available through JAVA_HOME, a common JDK installation folder or PATH
- Official ATM10 ServerFiles-*.zip from CurseForge

Plugin installation
-------------------
1. Extract the folder "ATM10.cs" into:
     <WindowsGSM>\plugins\
2. Reload plugins or restart WindowsGSM.
3. Download the current ATM10 SERVER PACK from CurseForge (the file is named like ServerFiles-8.1.zip).
4. Put that ZIP in ONE of these locations:
     <WindowsGSM>\WindowsGSM root folder (same folder as WindowsGSM.exe)\
     <WindowsGSM>\
     <WindowsGSM>\servers\<ID>\serverfiles\
5. Add/install "Minecraft: All the Mods 10 (ATM10)" in WindowsGSM.
6. The plugin asks for Minecraft EULA acceptance, extracts the server pack and runs the bundled NeoForge installer.
7. When install finishes, edit:
     <serverfiles>\user_jvm_args.txt
   and set your RAM, for example:
     -Xms8G
     -Xmx12G
8. Start the server from WindowsGSM.

What the plugin manages
-----------------------
- Starts NeoForge directly with:
    java @user_jvm_args.txt @libraries\net\neoforged\neoforge\<version>\win_args.txt nogui
- Embedded WindowsGSM console support
- Graceful "stop" command
- server.properties values from WindowsGSM:
    motd
    server-port
    enable-query=true
    query.port
    max-players
    allow-flight=true
- Local ATM10 version marker
- Best-effort latest-version detection from the public CurseForge project page
- Removal of WindowsGSM's broad automatic inbound rule for this server's startserver.bat

Firewall behavior
-----------------
Automatic firewall port opening is intentionally disabled in this MeFriendos build.

WindowsGSM may create a broad inbound application rule for startserver.bat before the server starts. The plugin removes broad inbound allow rules on any network profile for that exact path when every local port and every local and remote address are allowed. It does not create game-port rules, and port-specific or address-restricted manual rules are preserved.

Configure only the required Minecraft ports manually with the correct protocol, Windows profile and remote scope. This is safer than allowing the complete server start program through the Public firewall profile.

If Windows cannot verify or remove the broad rule, the plugin refuses to start the server and shows an error. Run WindowsGSM as administrator.

Updating ATM10
--------------
1. STOP the server.
2. Download the new official ServerFiles-*.zip from CurseForge.
3. Put it in WindowsGSM root folder (same folder as WindowsGSM.exe) (or one of the other supported locations).
4. Click UPDATE in WindowsGSM.

During update the plugin preserves:
- eula.txt
- server.properties
- ops.json
- whitelist.json
- banned-ips.json
- banned-players.json
- world / world_nether / world_the_end (if present in the new pack)
- logs / crash-reports / backups (if present in the new pack)
- your existing -Xms / -Xmx values from user_jvm_args.txt

The modpack-managed directories from the new server pack (mods, config, defaultconfigs, kubejs, etc.) are replaced with the versions from the new pack to avoid stale mods.

Important
---------
- Always make a world backup before an ATM10 update.
- Client and server ATM10 versions must match.
- The plugin does not distribute ATM10 files, mods, NeoForge or Minecraft.
- This is a custom MeFriendos plugin and has not been officially endorsed by the ATM Team, CurseForge or WindowsGSM.

Troubleshooting
---------------
"Java was not found" / "requires Java 21"
  Install 64-bit Java 21 and make sure `java -version` in CMD reports Java 21.

"ATM10 server pack not found"
  Put ServerFiles-*.zip in WindowsGSM root folder (same folder as WindowsGSM.exe) and click Install/Update again.

"NeoForge runtime not found"
  The NeoForge installer did not finish successfully. Check the WindowsGSM console and ensure Java 21 is active.

"startserver.bat was not found inside the selected server pack ZIP"
  You likely downloaded the normal client modpack instead of the separate ATM10 Server Pack.


0.1.3 FIX
---------
- Fixes WindowsGSM v1.25.1 crash during Install/Update:
  Cannot mix synchronous and asynchronous operation on process stream.
- The NeoForge installer process is now returned to WindowsGSM without starting
  asynchronous stdout/stderr readers. WindowsGSM owns the installer stdout stream.


0.1.4 UPDATE FIX
----------------
- Clicking Update when the installed ServerFiles version is already the same now
  exits immediately instead of unpacking/reinstalling the same pack.
- NeoForge update installation is awaited inside the plugin and Update() returns
  only after it is finished. This avoids WindowsGSM staying on e.g. "8.1 > 8.1".
- Existing world, server.properties, player lists and Xms/Xmx values are preserved.


0.1.5 MOTD FIX
--------------
- Existing motd= in server.properties is now preserved.
- WindowsGSM ServerName no longer overwrites the Minecraft MOTD on Start,
  CreateServerCFG or Update.
- If server.properties has no motd entry at all, the plugin adds the default:
  Minecraft: All the Mods 10 (ATM10)


0.1.6 RAM PRESERVATION FIX
---------------------------
- Install/Reinstall now preserves existing -Xms and -Xmx values from
  user_jvm_args.txt before extracting the official ATM10 ServerFiles ZIP.
- The saved memory values are restored immediately after extraction, so the
  server pack can no longer silently reset custom RAM settings to its defaults.
- Update keeps the existing 0.1.4+ RAM-preservation behavior unchanged.


0.1.7 FIREWALL SAFETY
----------------------
- Uses the PapaGordon GitHub repository as the plugin project URL.
- Removes WindowsGSM's broad automatic inbound rule for this server's exact startserver.bat path before Java starts.
- Prevents server startup if the firewall safety check itself fails.
- Does not open ports automatically or change manually configured firewall rules.
