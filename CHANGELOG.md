# Changelog

## 0.1.7 — 2026-09-09

- Uses the PapaGordon GitHub repository as the plugin project URL.
- Removes WindowsGSM's broad automatic inbound rule for this server's exact `startserver.bat` path before Java starts.
- Prevents server startup if the firewall safety check itself fails.
- Keeps automatic port opening disabled and leaves manual firewall rules unchanged.
- Added GitHub-ready documentation, security guidance and a testing checklist.

## 0.1.6

- Install/Reinstall preserves existing `-Xms` and `-Xmx` values from `user_jvm_args.txt`.

## 0.1.5

- Preserves an existing `motd` in `server.properties`.

## 0.1.4

- Skips reinstalling an already installed ATM10 server-pack version.
- Waits for the NeoForge updater internally so WindowsGSM no longer remains stuck on the update screen.
- Preserves worlds, server properties, player lists and JVM memory settings during updates.

## 0.1.3

- Fixed the WindowsGSM process-stream conflict during Install/Update.

## 0.1.2 and earlier

- Added local server-pack installation, NeoForge setup, embedded-console support and version tracking.
