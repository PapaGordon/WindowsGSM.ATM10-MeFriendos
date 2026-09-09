using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class ATM10
    {
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.ATM10",
            author = "MeFriendos",
            description = "WindowsGSM plugin for Minecraft: All the Mods 10 (ATM10 / NeoForge)",
            version = "0.1.7",
            url = "https://github.com/PapaGordon/WindowsGSM.ATM10-MeFriendos",
            color = "#7AC943"
        };

        private readonly ServerConfig _serverData;
        public string Error, Notice;

        public ATM10(ServerConfig serverData) => _serverData = serverData;

        public string StartPath => "startserver.bat";
        public string FullName = "Minecraft: All the Mods 10 (ATM10)";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public object QueryMethod = new UT3();

        public string Port = "25565";
        public string QueryPort = "25565";
        public string Defaultmap = "world";
        public string Maxplayers = "20";
        public string Additional = "";

        private const string VersionMarker = ".wgsm-atm10-version.txt";
        private const string CurseForgeUrl = "https://www.curseforge.com/minecraft/modpacks/all-the-mods-10";

        public async void CreateServerCFG()
        {
            try
            {
                EnsureServerProperties();
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }

        public async Task<Process> Start()
        {
            if (!RemoveAutomaticBroadFirewallRule())
            {
                Error = "Automatic firewall access could not be disabled. Start WindowsGSM as administrator or remove the broad startserver.bat rule manually.";
                return null;
            }

            string root = ServerPath.GetServersServerFiles(_serverData.ServerID);
            string javaPath = FindJava21();
            if (string.IsNullOrWhiteSpace(javaPath))
                return null;

            string winArgs = FindWinArgsPath(root);
            if (string.IsNullOrWhiteSpace(winArgs))
            {
                Error = "NeoForge runtime not found. Run Install/Update first so the NeoForge installer can create libraries\\...\\win_args.txt.";
                return null;
            }

            string eulaPath = Path.Combine(root, "eula.txt");
            if (!File.Exists(eulaPath) || File.ReadAllText(eulaPath).IndexOf("eula=true", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Error = "Minecraft EULA is not accepted. Set eula=true in eula.txt first.";
                return null;
            }

            try
            {
                EnsureServerProperties();
            }
            catch (Exception e)
            {
                Error = "Unable to update server.properties: " + e.Message;
                return null;
            }

            string relativeWinArgs = MakeRelativePath(root, winArgs).Replace('/', '\\');
            var args = new StringBuilder();
            args.Append("@user_jvm_args.txt ");
            args.Append("@").Append(relativeWinArgs).Append(" ");
            args.Append("nogui ");
            if (!string.IsNullOrWhiteSpace(_serverData.ServerParam))
                args.Append(_serverData.ServerParam).Append(" ");

            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = root,
                    FileName = javaPath,
                    Arguments = args.ToString(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            if (_serverData.EmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
            }

            try
            {
                p.Start();
                if (_serverData.EmbedConsole)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }
        }

        private bool RemoveAutomaticBroadFirewallRule()
        {
            string startPath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            string escapedPath = startPath.Replace("'", "''");
            string command =
                "$path = '" + escapedPath + "'; " +
                "$rules = Get-NetFirewallApplicationFilter -Program $path -PolicyStore ActiveStore -ErrorAction Stop | " +
                "Get-NetFirewallRule -ErrorAction Stop | " +
                "Where-Object { $rule = $_; $port = $rule | Get-NetFirewallPortFilter -ErrorAction Stop; " +
                "$address = $rule | Get-NetFirewallAddressFilter -ErrorAction Stop; " +
                "$rule.Direction -eq 'Inbound' -and $rule.Action -eq 'Allow' -and " +
                "$port.LocalPort -eq 'Any' -and $address.LocalAddress -eq 'Any' -and $address.RemoteAddress -eq 'Any' }; " +
                "if ($rules) { $rules | Remove-NetFirewallRule -Confirm:$false -ErrorAction Stop }";
            string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

            try
            {
                using (var firewallCleanup = new Process())
                {
                    firewallCleanup.StartInfo.FileName = "powershell.exe";
                    firewallCleanup.StartInfo.Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encodedCommand;
                    firewallCleanup.StartInfo.CreateNoWindow = true;
                    firewallCleanup.StartInfo.UseShellExecute = false;
                    firewallCleanup.Start();

                    if (!firewallCleanup.WaitForExit(10000))
                    {
                        firewallCleanup.Kill();
                        return false;
                    }

                    return firewallCleanup.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p == null || p.HasExited)
                    return;

                try
                {
                    if (p.StartInfo.RedirectStandardInput)
                    {
                        p.StandardInput.WriteLine("stop");
                        p.StandardInput.Flush();
                    }
                    else
                    {
                        ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "stop");
                    }

                    if (!p.WaitForExit(30000))
                        p.Kill();
                }
                catch
                {
                    try
                    {
                        if (!p.HasExited)
                            p.Kill();
                    }
                    catch { }
                }
            });
        }

        public async Task<Process> Install()
        {
            var agreedPrompt = await UI.CreateYesNoPromptV1(
                "Minecraft EULA",
                "By continuing you confirm that you agree to the Minecraft EULA.\nhttps://www.minecraft.net/eula",
                "Agree",
                "Decline");

            if (!agreedPrompt)
            {
                Error = "Minecraft EULA was not accepted.";
                return null;
            }

            string javaPath = FindJava21();
            if (string.IsNullOrWhiteSpace(javaPath))
                return null;

            string packZip = FindNewestServerPackZip();
            if (string.IsNullOrWhiteSpace(packZip))
            {
                Error = "ATM10 server pack not found. Download ServerFiles-*.zip from CurseForge and place it in plugins\\ATM10.cs, the WindowsGSM root folder, or this server's serverfiles folder, then click Install again.";
                return null;
            }

            string root = ServerPath.GetServersServerFiles(_serverData.ServerID);

            // Preserve custom JVM memory settings when Install is used on an
            // existing server (for example WindowsGSM Reinstall/repair). The
            // official ATM10 ServerFiles ZIP contains its own user_jvm_args.txt
            // and would otherwise reset a custom -Xms/-Xmx back to pack defaults.
            string memoryArgs = ReadMemoryArgs(Path.Combine(root, "user_jvm_args.txt"));

            try
            {
                await ExtractServerPack(packZip, root, false);
                RestoreMemoryArgs(Path.Combine(root, "user_jvm_args.txt"), memoryArgs);
                File.WriteAllText(Path.Combine(root, "eula.txt"), "eula=true" + Environment.NewLine);
                EnsureServerProperties();
                WriteLocalVersion(root, packZip);
            }
            catch (Exception e)
            {
                Error = "ATM10 extraction failed: " + e.Message;
                return null;
            }

            return StartNeoForgeInstaller(root, javaPath);
        }

        public async Task<Process> Update()
        {
            string javaPath = FindJava21();
            if (string.IsNullOrWhiteSpace(javaPath))
                return null;

            string packZip = FindNewestServerPackZip();
            if (string.IsNullOrWhiteSpace(packZip))
            {
                Error = "No ServerFiles-*.zip found. Download the new ATM10 server pack and place it in the WindowsGSM root folder, plugins\\ATM10.cs, or this server's serverfiles folder, then click Update again.";
                return null;
            }

            string root = ServerPath.GetServersServerFiles(_serverData.ServerID);
            string localBuild = GetLocalBuild();
            string packBuild = GetBuildFromServerPackZip(packZip);

            // WindowsGSM still allows the Update action when local and remote build
            // are identical. Do NOT unpack/reinstall the same 13+ GB modpack again.
            if (!string.IsNullOrWhiteSpace(localBuild) &&
                !string.IsNullOrWhiteSpace(packBuild) &&
                !string.Equals(localBuild, "unknown", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(localBuild, packBuild, StringComparison.OrdinalIgnoreCase))
            {
                Notice = "ATM10 " + localBuild + " is already up to date.";
                return null;
            }

            string memoryArgs = ReadMemoryArgs(Path.Combine(root, "user_jvm_args.txt"));

            try
            {
                await ExtractServerPack(packZip, root, true);
                RestoreMemoryArgs(Path.Combine(root, "user_jvm_args.txt"), memoryArgs);
                EnsureServerProperties();

                // Run the NeoForge installer internally and wait for it here.
                // Returning the Java Process to WindowsGSM caused the update UI to
                // remain on e.g. "8.1 > 8.1" while WindowsGSM waited on its streams.
                bool installerOk = await RunNeoForgeInstallerForUpdate(root, javaPath);
                if (!installerOk)
                    return null;

                WriteLocalVersion(root, packZip);
                Notice = "ATM10 updated successfully to " + packBuild + ".";
                return null;
            }
            catch (Exception e)
            {
                Error = "ATM10 update failed: " + e.Message;
                return null;
            }
        }

        public bool IsInstallValid()
        {
            string root = ServerPath.GetServersServerFiles(_serverData.ServerID);
            return File.Exists(Path.Combine(root, "user_jvm_args.txt")) &&
                   !string.IsNullOrWhiteSpace(FindWinArgsPath(root));
        }

        public bool IsImportValid(string path)
        {
            bool valid = Directory.Exists(path) &&
                         File.Exists(Path.Combine(path, "user_jvm_args.txt")) &&
                         (!string.IsNullOrWhiteSpace(FindWinArgsPath(path)) || File.Exists(Path.Combine(path, StartPath)));
            if (!valid)
                Error = "Invalid ATM10 server folder. user_jvm_args.txt and an installed NeoForge runtime/startserver.bat are required.";
            return valid;
        }

        public string GetLocalBuild()
        {
            string marker = ServerPath.GetServersServerFiles(_serverData.ServerID, VersionMarker);
            if (!File.Exists(marker))
                return "unknown";

            try
            {
                return File.ReadAllText(marker).Trim();
            }
            catch
            {
                return "unknown";
            }
        }

        public async Task<string> GetRemoteBuild()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) WindowsGSM-ATM10/0.1";
                    string html = await wc.DownloadStringTaskAsync(CurseForgeUrl);
                    Match match = Regex.Match(html, @"ServerFiles-([0-9][0-9A-Za-z._-]*)\.zip", RegexOptions.IgnoreCase);
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }
            catch (Exception e)
            {
                Notice = "Could not check latest ATM10 version on CurseForge: " + e.Message;
            }

            return GetLocalBuild();
        }

        private string FindJava21()
        {
            // 1) Prefer JAVA_HOME when it points to Java 21+.
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                string fromJavaHome = Path.Combine(javaHome.Trim().Trim('\"'), "bin", "java.exe");
                if (IsJava21OrNewer(fromJavaHome))
                    return fromJavaHome;
            }

            // 2) Search common Windows JDK locations. This also covers the
            //    MeFriendos installation C:\Program Files\Java\jdk-21.0.12.
            string[] commonRoots =
            {
                @"C:\Program Files\Java",
                @"C:\Program Files\Eclipse Adoptium",
                @"C:\Program Files\Microsoft",
                @"C:\Program Files\Amazon Corretto",
                @"C:\Program Files\BellSoft"
            };

            foreach (string root in commonRoots)
            {
                try
                {
                    if (!Directory.Exists(root))
                        continue;

                    foreach (string dir in Directory.GetDirectories(root, "*21*", SearchOption.TopDirectoryOnly)
                                                     .OrderByDescending(d => Directory.GetLastWriteTimeUtc(d)))
                    {
                        string candidate = Path.Combine(dir, "bin", "java.exe");
                        if (IsJava21OrNewer(candidate))
                            return candidate;
                    }
                }
                catch { }
            }

            // 3) Fall back to WindowsGSM/PATH detection.
            string javaPath = JavaHelper.FindJavaExecutableAbsolutePath();
            if (IsJava21OrNewer(javaPath))
                return javaPath;

            if (!string.IsNullOrWhiteSpace(javaPath) && File.Exists(javaPath))
            {
                int major = GetJavaMajorVersion(javaPath);
                Error = "Java " + (major > 0 ? major.ToString() : "unknown") +
                        " was found, but ATM10 / Minecraft 1.21.1 requires Java 21 or newer. " +
                        "Set JAVA_HOME to your Java 21 JDK or add its bin folder to PATH.";
            }
            else
            {
                Error = "Java 21 was not found. Install a 64-bit Java 21 JDK, set JAVA_HOME, " +
                        "or add its bin folder to PATH. Common locations such as C:\\Program Files\\Java\\jdk-21* are checked automatically.";
            }

            return null;
        }

        private bool IsJava21OrNewer(string javaPath)
        {
            if (string.IsNullOrWhiteSpace(javaPath) || !File.Exists(javaPath))
                return false;

            int major = GetJavaMajorVersion(javaPath);
            return major >= 21;
        }

        private int GetJavaMajorVersion(string javaPath)
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = javaPath,
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                string text = p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd();
                p.WaitForExit(10000);

                Match m = Regex.Match(text, "version\\s+\\\"(?<major>\\d+)(?:\\.(?<minor>\\d+))?");
                if (!m.Success)
                    return 0;

                int first = int.Parse(m.Groups["major"].Value);
                if (first == 1 && m.Groups["minor"].Success)
                    return int.Parse(m.Groups["minor"].Value);
                return first;
            }
            catch
            {
                return 0;
            }
        }

        private string GetBuildFromServerPackZip(string zipPath)
        {
            if (string.IsNullOrWhiteSpace(zipPath))
                return string.Empty;

            Match match = Regex.Match(
                Path.GetFileName(zipPath),
                @"^ServerFiles-(.+)\.zip$",
                RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private async Task<bool> RunNeoForgeInstallerForUpdate(string root, string javaPath)
        {
            string installer = FindNeoForgeInstaller(root);

            // Some pack updates do not ship a new installer. If a valid NeoForge
            // runtime already exists, there is nothing to do.
            if (string.IsNullOrWhiteSpace(installer))
            {
                if (!string.IsNullOrWhiteSpace(FindWinArgsPath(root)))
                    return true;

                Error = "NeoForge installer JAR was not found and no installed NeoForge runtime is available.";
                return false;
            }

            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = root,
                    FileName = javaPath,
                    Arguments = "-jar \"" + installer + "\" --installServer",
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,

                    // IMPORTANT: do not redirect these streams. Update() waits for
                    // the process itself and then returns null to WindowsGSM.
                    // This avoids the synchronous/asynchronous stream conflict and
                    // the update screen getting stuck.
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                p.Start();
                await Task.Run(() => p.WaitForExit());

                if (p.ExitCode != 0)
                {
                    Error = "NeoForge installer exited with code " + p.ExitCode + ".";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(FindWinArgsPath(root)))
                {
                    Error = "NeoForge installer finished, but win_args.txt was not created.";
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Error = "Could not run NeoForge installer during update: " + e.Message;
                return false;
            }
            finally
            {
                p.Dispose();
            }
        }

        private Process StartNeoForgeInstaller(string root, string javaPath)
        {
            if (!string.IsNullOrWhiteSpace(FindWinArgsPath(root)))
            {
                string installerExisting = FindNeoForgeInstaller(root);
                if (string.IsNullOrWhiteSpace(installerExisting))
                    return null;
            }

            string installer = FindNeoForgeInstaller(root);
            if (string.IsNullOrWhiteSpace(installer))
            {
                Error = "NeoForge installer JAR was not found in the ATM10 server pack.";
                return null;
            }

            // IMPORTANT: Install()/Update() return this Process to WindowsGSM.
            // WindowsGSM v1.25.1 reads Installer.StandardOutput synchronously in
            // Button_Install_Click. Therefore the plugin MUST NOT call
            // BeginOutputReadLine()/BeginErrorReadLine() on this process, otherwise
            // .NET throws: "Cannot mix synchronous and asynchronous operation on process stream.".
            //
            // Redirect stdout for the WindowsGSM install log. stderr is intentionally
            // left unredirected so there is no unread redirected stream that could
            // block a verbose Java installer.
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = root,
                    FileName = javaPath,
                    Arguments = "-jar \"" + installer + "\" --installServer",
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = "Could not start NeoForge installer: " + e.Message;
                return null;
            }
        }

        private string FindNeoForgeInstaller(string root)
        {
            if (!Directory.Exists(root))
                return null;

            return Directory.GetFiles(root, "neoforge-*-installer.jar", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .FirstOrDefault();
        }

        private string FindWinArgsPath(string root)
        {
            try
            {
                string neoForgeRoot = Path.Combine(root, "libraries", "net", "neoforged", "neoforge");
                if (!Directory.Exists(neoForgeRoot))
                    return null;

                var candidates = Directory.GetDirectories(neoForgeRoot)
                    .Select(d => new
                    {
                        Directory = d,
                        Version = ParseVersionSafe(Path.GetFileName(d))
                    })
                    .OrderByDescending(x => x.Version)
                    .ThenByDescending(x => Directory.GetLastWriteTimeUtc(x.Directory));

                foreach (var candidate in candidates)
                {
                    string winArgs = Path.Combine(candidate.Directory, "win_args.txt");
                    if (File.Exists(winArgs))
                        return winArgs;
                }
            }
            catch { }

            return null;
        }

        private Version ParseVersionSafe(string text)
        {
            Version v;
            if (Version.TryParse(text, out v))
                return v;
            return new Version(0, 0);
        }

        private string FindNewestServerPackZip()
        {
            var roots = new List<string>();

            // The WindowsGSM process can have its AppDomain base inside \bin,
            // so derive the real WindowsGSM root from the known serverfiles path.
            string serverFiles = ServerPath.GetServersServerFiles(_serverData.ServerID);
            roots.Add(serverFiles);

            try
            {
                DirectoryInfo serverFilesDir = new DirectoryInfo(serverFiles);
                // ...\WindowsGSM\servers\<id>\serverfiles -> ...\WindowsGSM
                DirectoryInfo wgsmRoot = serverFilesDir.Parent != null &&
                                         serverFilesDir.Parent.Parent != null
                    ? serverFilesDir.Parent.Parent.Parent
                    : null;

                if (wgsmRoot != null)
                {
                    roots.Add(wgsmRoot.FullName);
                    roots.Add(Path.Combine(wgsmRoot.FullName, "plugins", "ATM10.cs"));
                }
            }
            catch { }

            // Keep the previous locations as fallbacks for unusual WindowsGSM layouts.
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                roots.Add(baseDir);
                roots.Add(Path.Combine(baseDir, "plugins", "ATM10.cs"));

                DirectoryInfo baseInfo = new DirectoryInfo(baseDir);
                if (baseInfo.Parent != null)
                {
                    roots.Add(baseInfo.Parent.FullName);
                    roots.Add(Path.Combine(baseInfo.Parent.FullName, "plugins", "ATM10.cs"));
                }
            }
            catch { }

            try
            {
                roots.Add(Directory.GetCurrentDirectory());
            }
            catch { }

            var files = new List<string>();
            foreach (string root in roots
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    if (!Directory.Exists(root))
                        continue;

                    files.AddRange(Directory.GetFiles(root, "ServerFiles-*", SearchOption.TopDirectoryOnly)
                        .Where(f => string.Equals(Path.GetExtension(f), ".zip", StringComparison.OrdinalIgnoreCase)));
                }
                catch { }
            }

            return files
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .FirstOrDefault();
        }

        private async Task ExtractServerPack(string zipPath, string root, bool update)
        {
            string temp = Path.Combine(root, "_wgsm_atm10_extract");
            if (Directory.Exists(temp))
                Directory.Delete(temp, true);
            Directory.CreateDirectory(temp);

            try
            {
                await FileManagement.ExtractZip(zipPath, temp);

                string packRoot = FindExtractedPackRoot(temp);
                if (string.IsNullOrWhiteSpace(packRoot))
                    throw new Exception("startserver.bat was not found inside the selected server pack ZIP.");

                if (update)
                {
                    foreach (string oldInstaller in Directory.GetFiles(root, "neoforge-*-installer.jar", SearchOption.TopDirectoryOnly))
                    {
                        try { File.Delete(oldInstaller); } catch { }
                    }
                    CopyPackForUpdate(packRoot, root);
                }
                else
                {
                    CopyDirectoryContents(packRoot, root, true);
                }
            }
            finally
            {
                try
                {
                    if (Directory.Exists(temp))
                        Directory.Delete(temp, true);
                }
                catch { }
            }
        }

        private string FindExtractedPackRoot(string temp)
        {
            string direct = Path.Combine(temp, StartPath);
            if (File.Exists(direct))
                return temp;

            string found = Directory.GetFiles(temp, StartPath, SearchOption.AllDirectories)
                .OrderBy(f => f.Length)
                .FirstOrDefault();
            return string.IsNullOrWhiteSpace(found) ? null : Path.GetDirectoryName(found);
        }

        private void CopyPackForUpdate(string source, string destination)
        {
            var preserveFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "eula.txt",
                "server.properties",
                "ops.json",
                "whitelist.json",
                "banned-ips.json",
                "banned-players.json",
                VersionMarker
            };

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file);
                if (preserveFiles.Contains(name))
                    continue;

                string dest = Path.Combine(destination, name);
                File.Copy(file, dest, true);
            }

            var preserveDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "world",
                "world_nether",
                "world_the_end",
                "logs",
                "crash-reports",
                "backups"
            };

            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(dir);
                if (preserveDirectories.Contains(name))
                    continue;

                string dest = Path.Combine(destination, name);

                if (Directory.Exists(dest))
                    Directory.Delete(dest, true);

                CopyDirectoryContents(dir, dest, true);
            }
        }

        private void CopyDirectoryContents(string source, string destination, bool overwrite)
        {
            Directory.CreateDirectory(destination);

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.TopDirectoryOnly))
            {
                string dest = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, dest, overwrite);
            }

            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.TopDirectoryOnly))
            {
                string dest = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectoryContents(dir, dest, overwrite);
            }
        }

        private void EnsureServerProperties()
        {
            string root = ServerPath.GetServersServerFiles(_serverData.ServerID);
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "server.properties");

            var lines = File.Exists(path)
                ? File.ReadAllLines(path).ToList()
                : new List<string>();

            // Preserve a custom MOTD. WindowsGSM's ServerName is not the Minecraft
            // MOTD and must not overwrite server.properties on every start/update.
            bool hasMotd = lines.Any(line =>
                line.StartsWith("motd=", StringComparison.OrdinalIgnoreCase));

            if (!hasMotd)
                SetProperty(lines, "motd", "Minecraft: All the Mods 10 (ATM10)");

            SetProperty(lines, "server-port", _serverData.ServerPort);
            SetProperty(lines, "enable-query", "true");
            SetProperty(lines, "query.port", _serverData.ServerQueryPort);
            SetProperty(lines, "max-players", _serverData.ServerMaxPlayer);
            SetProperty(lines, "allow-flight", "true");

            File.WriteAllLines(path, lines.ToArray());
        }

        private void SetProperty(List<string> lines, string key, string value)
        {
            string prefix = key + "=";
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = prefix + EscapePropertyValue(value);
                    return;
                }
            }
            lines.Add(prefix + EscapePropertyValue(value));
        }

        private string EscapePropertyValue(string value)
        {
            if (value == null)
                return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\r", "").Replace("\n", " ");
        }

        private string ReadMemoryArgs(string userJvmArgsPath)
        {
            if (!File.Exists(userJvmArgsPath))
                return string.Empty;

            try
            {
                string text = File.ReadAllText(userJvmArgsPath);
                Match xms = Regex.Match(text, @"(?i)(?:^|\s)(-Xms\S+)", RegexOptions.Multiline);
                Match xmx = Regex.Match(text, @"(?i)(?:^|\s)(-Xmx\S+)", RegexOptions.Multiline);
                return (xms.Success ? xms.Groups[1].Value : "") + "|" + (xmx.Success ? xmx.Groups[1].Value : "");
            }
            catch
            {
                return string.Empty;
            }
        }

        private void RestoreMemoryArgs(string userJvmArgsPath, string memoryArgs)
        {
            if (string.IsNullOrWhiteSpace(memoryArgs) || !File.Exists(userJvmArgsPath))
                return;

            string[] parts = memoryArgs.Split('|');
            string xms = parts.Length > 0 ? parts[0] : "";
            string xmx = parts.Length > 1 ? parts[1] : "";

            var lines = File.ReadAllLines(userJvmArgsPath).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = Regex.Replace(lines[i], @"(?i)(?:^|\s)-Xm[sx]\S+", "").Trim();
            }
            lines.RemoveAll(l => string.IsNullOrWhiteSpace(l));

            if (!string.IsNullOrWhiteSpace(xms))
                lines.Add(xms);
            if (!string.IsNullOrWhiteSpace(xmx))
                lines.Add(xmx);

            File.WriteAllLines(userJvmArgsPath, lines.ToArray());
        }

        private void WriteLocalVersion(string root, string zipPath)
        {
            string version = GetBuildFromServerPackZip(zipPath);
            if (string.IsNullOrWhiteSpace(version))
                version = Path.GetFileNameWithoutExtension(zipPath);

            File.WriteAllText(Path.Combine(root, VersionMarker), version);
        }

        private string MakeRelativePath(string basePath, string fullPath)
        {
            string baseFull = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string targetFull = Path.GetFullPath(fullPath);
            if (targetFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                return targetFull.Substring(baseFull.Length);
            return targetFull;
        }
    }
}
