using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;


namespace NowPlaying
{
    public class NowPlaying : GenericPlugin
    {


        private static readonly ILogger logger = LogManager.GetLogger();

        public IPlayniteAPI Api { get; }
        public NowPlayingSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("db4e7ade-57fb-426c-8392-60e2347a0209");


        private NowPlayingData _gameData;

        public event PropertyChangedEventHandler PropertyChanged;
        public NowPlayingData GameData
        {
            get => _gameData;
            set
            {
                if (_gameData != value)
                {
                    _gameData = value;
                    OnPropertyChanged(nameof(GameData));
                    settings.RunningGame = value;
                }
            }
        }

        public static Window NowPlayingWindow { get; private set; }

        public ICommand LaunchCommand;
        public ICommand ReturnCommand;
        public ICommand ExitCommand;
        public ICommand LaunchCustomWindowCommand;

        private GlobalKeyboardHook keyboardHook;

        private static void ExecuteShowDialog(NowPlaying instance)
        {
            ShowNowPlayingDialog(instance);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NowPlaying(IPlayniteAPI api) : base(api)
        {
            Api = api;
            settings = new NowPlayingSettings(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            LaunchCommand = new RelayCommand(() => ExecuteShowDialog(this));
            ReturnCommand = new RelayCommand(() => ExecuteReturnToGame(api));
            ExitCommand = new RelayCommand(() => ExecuteCloseGame(this));
            LaunchCustomWindowCommand = new RelayCommand(() => ShowNowPlayingWindow(api));
            settings.OpenDialog = (RelayCommand)LaunchCommand;
            settings.CloseGame = (RelayCommand)ExitCommand;
            settings.ReturnToGame = (RelayCommand)ReturnCommand;
            settings.OpenCustomDialog = (RelayCommand)LaunchCustomWindowCommand;

            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "NowPlaying",
                SettingsRoot = $"settings"
            });

            // Initialize global keyboard hook
            keyboardHook = new GlobalKeyboardHook();
            keyboardHook.KeyPressed += OnKeyPressed;
        }

        public static void ExecuteReturnToGame(IPlayniteAPI api)
        {
            var GameData = CreateNowPlayingData(api, api.Database.Games.FirstOrDefault(g => g.IsRunning), null);
            ReturnToGame(GameData);
        }

        public static void ExecuteCloseGame(NowPlaying instance)
        {
            var GameData = CreateNowPlayingData(instance.Api, instance.Api.Database.Games.FirstOrDefault(g => g.IsRunning), null);
            CloseGame(GameData, instance);
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            GameData = CreateNowPlayingData(Api, args.Game, args.StartedProcessId);
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            settings.GameClosing = false; // Reset the closing flag
            GameData = null;
        }

        public override void OnControllerButtonStateChanged(OnControllerButtonStateChangedArgs args)
        {
            Debug.WriteLine($"Button {args.Button} ${args.State}");
            if (settings.OpenWithGuideButton && args.Button == ControllerInput.Guide && args.State == ControllerInputState.Pressed)
            {
                ShowPlaynite();
            }

            if (NowPlayingWindow != null && NowPlayingWindow.IsVisible && args.Button == ControllerInput.B && args.State == ControllerInputState.Pressed)
            {
                NowPlayingWindow.Close();
                NowPlayingWindow = null;
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override System.Windows.Controls.UserControl GetSettingsView(bool firstRunSettings)
        {
            return new NowPlayingSettingsView();
        }

        private void ShowPlaynite()
        {
            Debug.WriteLine("Trying to show playnite");
            try
            {
                var processes = Process.GetProcessesByName("Playnite.FullscreenApp");
                if (processes.Length > 0)
                {
                    Process.Start(Path.Combine(Api.Paths.ApplicationPath, "Playnite.FullscreenApp.exe"));
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error showing Playnite: {ex}");
            }
        }

        private void OnKeyPressed(Keys key, bool altPressed)
        {
            // Alt + ` (backtick) to focus playnite
            if (settings.OpenWithKeyboardShortcut && altPressed && key == Keys.Oem3)
            {
                ShowPlaynite();
            }
        }

        private static void ShowNowPlayingWindow(IPlayniteAPI api)
        {
            var parent = api.Dialogs.GetCurrentAppWindow();
            NowPlayingWindow = api.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            NowPlayingWindow.Height = parent.Height;
            NowPlayingWindow.Width = parent.Width;
            NowPlayingWindow.Title = "MoData";

            string xamlString = @"
            <Viewbox Stretch=""Uniform"" 
                     xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                     xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                <Grid Width=""1920"" Height=""1080"">
                    <ContentControl x:Name=""NowPlayingWindow""
                                    Focusable=""False""
                                    Style=""{DynamicResource NowPlayingWindowStyle}"" />
                </Grid>
            </Viewbox>";

            // Parse the XAML string
            var element = (FrameworkElement)XamlReader.Parse(xamlString);


            // Set content of a window. Can be loaded from xaml, loaded from UserControl or created from code behind
            NowPlayingWindow.Content = element;

            // Set data context if you want to use MVVM pattern
            NowPlayingWindow.DataContext = parent.DataContext;

            // Set owner if you need to create modal dialog window
            NowPlayingWindow.Owner = parent;
            NowPlayingWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Use Show or ShowDialog to show the window
            NowPlayingWindow.ShowDialog();
        }


        public static void ShowNowPlayingDialog(NowPlaying instance)
        {
            var runningGame = instance.Api.Database.Games.FirstOrDefault(g => g.IsRunning);
            var GameData = CreateNowPlayingData(instance.Api, runningGame, null);
            if (GameData == null)
            {
                return;
            }
            var returnToGameButton = new MessageBoxOption("Back to Game", true, false);
            var closeGameButton = new MessageBoxOption("Close Game", false, false);
            var cancelButton = new MessageBoxOption("Cancel", false, true);

            var response = instance.Api.Dialogs.ShowMessage("", GameData.GameName, MessageBoxImage.None, new List<MessageBoxOption>
            {
                returnToGameButton, closeGameButton, cancelButton
            });

            if (response.Title == "Back to Game") ReturnToGame(GameData);
            if (response.Title == "Close Game") CloseGame(GameData, instance);
        }

        private static NowPlayingData CreateNowPlayingData(IPlayniteAPI api, Game game, int? processId)
        {
            if (game == null) return null;
            return new NowPlayingData
            {
                GameName = game.Name,
                ProcessId = FindRunningGameProcess(game, processId)?.Id ?? processId ?? -1,
                IconPath = GetFullIconPath(api, game),
                Id = game.Id.ToString(),
                StartTime = DateTime.Now,
                CoverPath = GetFullCoverPath(api, game)
            };
        }

        private static string GetFullIconPath(IPlayniteAPI api, Game game)
        {
            if (string.IsNullOrEmpty(game.Icon))
                return null;

            try
            {
                return api.Database.GetFullFilePath(game.Icon);
            }
            catch (Exception ex)
            {
                logger.Error($"Error getting cover image path: {ex}");
                return null;
            }
        }

        private static string GetFullCoverPath(IPlayniteAPI api, Game game)
        {
            if (string.IsNullOrEmpty(game.CoverImage))
                return null;

            try
            {
                return api.Database.GetFullFilePath(game.CoverImage);
            }
            catch (Exception ex)
            {
                logger.Error($"Error getting cover image path: {ex}");
                return null;
            }
        }

        private static Process FindRunningGameProcess(Game game, int? pid)
        {
            if (game == null) return null;

            try
            {
                // Get process tree starting from the provided PID
                var processTree = GetProcessTree(pid);

                // Get all executable files in the game installation directory and subdirectories
                var gameExecutables = GetGameExecutables(game);

                // Get the game name words for title comparison
                string gameName = game.Name;
                string[] gameNameWords = gameName.ToLower().Split(new char[] { ' ', '-', '_', ':', '.', '(', ')', '[', ']' },
                    StringSplitOptions.RemoveEmptyEntries);

                logger.Debug($"Game name for matching: {gameName}, split into {gameNameWords.Length} words");
                logger.Debug($"Process tree contains {processTree.Count} processes");

                // Filter process tree to only include processes with main windows and sufficient memory
                var candidateProcesses = processTree.Where(p =>
                {
                    try
                    {
                        return !p.HasExited &&
                               p.MainWindowHandle != IntPtr.Zero &&
                               !string.IsNullOrEmpty(p.MainWindowTitle) &&
                               p.WorkingSet64 > 100 * 1024 * 1024;
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();

                logger.Debug($"Found {candidateProcesses.Count} candidate processes in tree");

                var candidates = new List<Process>();
                var nameMatchCandidates = new List<Process>();
                var titleMatchCandidates = new List<(Process Process, int MatchCount)>();
                var inaccessibleCandidates = new List<Process>();

                foreach (var p in candidateProcesses)
                {
                    try
                    {
                        // Check if process name matches any executable in the game folder
                        bool nameMatches = gameExecutables.Any(exe =>
                            string.Equals(exe, p.ProcessName, StringComparison.OrdinalIgnoreCase));

                        // Check window title for matches with game name
                        int titleMatchScore = CalculateTitleMatchScore(p, gameNameWords);

                        try
                        {
                            // Try to access the module info
                            var modulePath = p.MainModule.FileName;
                            if (modulePath.IndexOf(game.InstallDirectory, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                candidates.Add(p);
                            }
                            else if (nameMatches)
                            {
                                nameMatchCandidates.Add(p);
                            }
                            else if (titleMatchScore > 0)
                            {
                                titleMatchCandidates.Add((p, titleMatchScore));
                            }
                        }
                        catch
                        {
                            // Can't access module info (likely due to 32/64 bit mismatch)
                            if (nameMatches)
                            {
                                nameMatchCandidates.Add(p);
                            }
                            else if (titleMatchScore > 0)
                            {
                                titleMatchCandidates.Add((p, titleMatchScore));
                            }
                            else
                            {
                                inaccessibleCandidates.Add(p);
                            }
                        }
                    }
                    catch { /* Skip processes we can't access at all */ }
                }

                // Priority order: direct path match, window title match, process name match, then best guess
                var bestMatch = FindBestMatch(candidates, titleMatchCandidates, nameMatchCandidates, inaccessibleCandidates, pid);

                return bestMatch;
            }
            catch (Exception ex)
            {
                logger.Error($"Error finding game process: {ex.Message}");
                return null;
            }
        }

        private static List<Process> GetProcessTree(int? rootPid)
        {
            var processTree = new List<Process>();

            if (rootPid == null || rootPid <= 0)
            {
                logger.Debug("No root PID provided, using all processes with main windows");
                // Fallback to all processes with main windows if no PID provided
                return Process.GetProcesses()
                    .Where(p =>
                    {
                        try
                        {
                            return p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(p.MainWindowTitle);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();
            }

            try
            {
                // Start with the root process
                var rootProcess = Process.GetProcessById(rootPid.Value);
                if (rootProcess != null && !rootProcess.HasExited)
                {
                    processTree.Add(rootProcess);
                    logger.Debug($"Added root process: {rootProcess.ProcessName} (ID: {rootProcess.Id})");
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Could not find root process with ID {rootPid}: {ex.Message}");
            }

            // Get all child processes recursively
            var allProcesses = Process.GetProcesses();
            var processesById = allProcesses.ToDictionary(p => p.Id, p => p);
            var childProcesses = GetChildProcesses(rootPid.Value, processesById);

            processTree.AddRange(childProcesses);

            logger.Debug($"Process tree built with {processTree.Count} total processes");
            return processTree;
        }

        private static List<Process> GetChildProcesses(int parentId, Dictionary<int, Process> processesById)
        {
            var children = new List<Process>();

            try
            {
                // Use P/Invoke to get child processes via Windows API
                foreach (var process in processesById.Values)
                {
                    try
                    {
                        if (!process.HasExited && GetParentProcessId(process.Id) == parentId)
                        {
                            children.Add(process);
                            logger.Debug($"Added child process: {process.ProcessName} (ID: {process.Id})");

                            // Recursively get grandchildren
                            children.AddRange(GetChildProcesses(process.Id, processesById));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Debug($"Error checking process {process.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Debug($"Error getting child processes for PID {parentId}: {ex.Message}");
            }

            return children;
        }

        private static int GetParentProcessId(int processId)
        {
            try
            {
                var handle = OpenProcess(ProcessAccessFlags.QueryInformation, false, processId);
                if (handle == IntPtr.Zero)
                    return -1;

                try
                {
                    var pbi = new PROCESS_BASIC_INFORMATION();
                    int returnLength;
                    int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);

                    if (status == 0)
                        return pbi.InheritedFromUniqueProcessId.ToInt32();
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
            catch (Exception ex)
            {
                logger.Debug($"Error getting parent PID for process {processId}: {ex.Message}");
            }

            return -1;
        }

        private static List<string> GetGameExecutables(Game game)
        {
            var gameExecutables = new List<string>();
            try
            {
                gameExecutables = Directory.GetFiles(game.InstallDirectory, "*.exe", SearchOption.AllDirectories)
                    .Select(path => Path.GetFileNameWithoutExtension(path))
                    .ToList();

                // Add common variations
                var additionalNames = new List<string>();
                foreach (var exe in gameExecutables)
                {
                    additionalNames.Add(exe.Replace("-", ""));
                    additionalNames.Add(exe.Replace("_", ""));
                    additionalNames.Add(exe.Replace(" ", ""));
                    additionalNames.Add(exe.Replace("-", " "));
                    additionalNames.Add(exe.Replace("_", " "));
                }
                gameExecutables.AddRange(additionalNames);

                logger.Debug($"Found {gameExecutables.Count} potential game executables in {game.InstallDirectory}");
            }
            catch (Exception ex)
            {
                logger.Error($"Error scanning game directory: {ex.Message}");
            }

            return gameExecutables;
        }

        private static int CalculateTitleMatchScore(Process process, string[] gameNameWords)
        {
            if (process.MainWindowHandle == IntPtr.Zero || string.IsNullOrEmpty(process.MainWindowTitle))
                return 0;

            string[] windowTitleWords = process.MainWindowTitle.ToLower().Split(
                new char[] { ' ', '-', '_', ':', '.', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries);

            return gameNameWords.Count(gameWord =>
                windowTitleWords.Any(titleWord => titleWord.Contains(gameWord) || gameWord.Contains(titleWord)));
        }

        private static Process FindBestMatch(
            List<Process> candidates,
            List<(Process Process, int MatchCount)> titleMatchCandidates,
            List<Process> nameMatchCandidates,
            List<Process> inaccessibleCandidates,
            int? originalPid)
        {
            if (candidates.Count > 0)
            {
                var bestMatch = candidates.OrderByDescending(p => p.WorkingSet64).First();
                Debug.WriteLine($"Found process with matching path: {bestMatch.ProcessName} (ID: {bestMatch.Id})");
                return bestMatch;
            }


            if (nameMatchCandidates.Count > 0)
            {
                var bestMatch = nameMatchCandidates.OrderByDescending(p => p.WorkingSet64).First();
                Debug.WriteLine($"Found process with matching name: {bestMatch.ProcessName} (ID: {bestMatch.Id})");
                return bestMatch;
            }

            if (titleMatchCandidates.Count > 0)
            {
                var bestMatch = titleMatchCandidates.OrderByDescending(t => t.MatchCount)
                                                    .ThenByDescending(t => t.Process.WorkingSet64)
                                                    .First().Process;
                Debug.WriteLine($"Found process with matching window title: {bestMatch.ProcessName} (ID: {bestMatch.Id}, Title: {bestMatch.MainWindowTitle})");
                return bestMatch;
            }

            if (inaccessibleCandidates.Count > 0)
            {
                var processFromPlaynite = inaccessibleCandidates.FirstOrDefault(p => p.Id == originalPid);
                if (processFromPlaynite != null)
                {
                    Debug.WriteLine($"Found original process in tree: {processFromPlaynite.ProcessName} (ID: {processFromPlaynite.Id})");
                    return processFromPlaynite;
                }
            }

            return null;
        }

        private static void ReturnToGame(NowPlayingData data)
        {
            GameStateManager.ReturnToGame(data);
        }

        private static void CloseGame(NowPlayingData data, NowPlaying instance)
        {
            if (instance.settings.ConfirmClose)
            {
                var yesBtn = new MessageBoxOption("Close", true, false);
                var noBtn = new MessageBoxOption("Cancel", false, true);

                var response = instance.Api.Dialogs.ShowMessage("", "Do you want to close " + data.GameName + "?", MessageBoxImage.None, new List<MessageBoxOption> { yesBtn, noBtn });
                if (response.Title == "Close")
                {
                    instance.settings.GameClosing = true; // Set the closing flag to true
                    GameStateManager.CloseGame(data, instance.settings.CloseBehavior);
                }
            }
            else
            {
                instance.settings.GameClosing = true; // Set the closing flag to true
                GameStateManager.CloseGame(data, instance.settings.CloseBehavior);
            }
        }


        // P/Invoke declarations
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            QueryInformation = 0x00000400
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

    }

    public static class GameStateManager
    {
        public static Process FindProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch
            {
                return null;
            }
        }

        public static void ReturnToGame(NowPlayingData gameData)
        {
            if (gameData != null)
            {
                var proc = FindProcessById(gameData.ProcessId);
                if (proc != null)
                {
                    // Restore the window if it's minimized
                    if (WindowHelper.IsIconic(proc.MainWindowHandle))
                    {
                        WindowHelper.ShowWindow(proc.MainWindowHandle, WindowHelper.SW_RESTORE);
                    }
                    // Bring the game window to the front
                    WindowHelper.SetForegroundWindow(proc.MainWindowHandle);
                }
            }
        }

        public static async void CloseGame(NowPlayingData gameData, CloseBehavior behavior = CloseBehavior.CloseAndEnd)
        {
            if (gameData == null)
            {
                return;
            }
            var proc = FindProcessById(gameData.ProcessId);
            if (proc == null)
            {
                return;
            }
            var success = proc.CloseMainWindow();
            switch (behavior)
            {
                case CloseBehavior.CloseAndEnd:
                    if (!success)
                    {
                        proc.Kill(); // Forcefully kill the process if closing the main window fails
                    }
                    for (int i = 0; i < 30 && !proc.HasExited; i++)
                    {
                        await Task.Delay(100); // Wait for up to 3 seconds for the process to exit gracefully
                    }
                    if (!proc.HasExited)
                    {
                        Debug.WriteLine($"Process {proc.ProcessName} did not exit gracefully after 3 seconds, killing it forcefully.");
                        proc.Kill(); // Forcefully kill the process if it hasn't exited
                    }
                    proc.Close();
                    break;
                case CloseBehavior.CloseWindow:
                    if (!success)
                    {
                        proc.Kill(); // Forcefully kill the process if closing the main window fails
                    }
                    proc.Close();
                    break;
                case CloseBehavior.EndTask:
                    proc.Kill(); // Forcefully kill the process regardless
                    proc.Close();
                    break;
                default:
                    Debug.WriteLine($"Unknown close behavior: {behavior}");
                    break;
            }


        }
    }


    public static class WindowHelper
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public const int SW_RESTORE = 9;
        public const int SW_MAXIMIZE = 3;

        public static void ForceFocusWindow(IntPtr hwnd)
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundThread = GetWindowThreadProcessId(foregroundWindow, out _);
            uint currentThread = GetCurrentThreadId();

            AttachThreadInput(currentThread, foregroundThread, true);
            SetForegroundWindow(hwnd);
            AttachThreadInput(currentThread, foregroundThread, false);
        }
    }

    public class NowPlayingData
    {
        public string GameName { get; set; }
        public int ProcessId { get; set; }
        public string IconPath { get; set; }
        public string CoverPath { get; set; }
        public string Id { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
    }

}