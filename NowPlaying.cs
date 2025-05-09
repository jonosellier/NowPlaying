using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        public NowPlayingSettingsViewModel settings { get; set; }

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
                    settings.Settings.RunningGame = value;
                }
            }
        }

        public ICommand LaunchCommand;

        private static void ExecuteShowDialog(IPlayniteAPI api)
        {
            ShowNowPlayingDialog(api);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NowPlaying(IPlayniteAPI api) : base(api)
        {
            Api = api;
            settings = new NowPlayingSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            LaunchCommand = new RelayCommand(() => ExecuteShowDialog(api));
            settings.Settings.OpenDialog = LaunchCommand;
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "NowPlaying",
                SettingsRoot = $"settings.Settings"
            });

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
            GameData = null;
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings; 
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new NowPlayingSettingsView();
        }

        public static void ShowNowPlayingDialog(IPlayniteAPI api)
        {
            var runningGame = api.Database.Games.FirstOrDefault(g => g.IsRunning);
            var GameData = CreateNowPlayingData(api, runningGame, null);
            if (GameData == null)
            {
                return;
            }
            var returnToGameButton = new MessageBoxOption("Back to Game", true, false);
            var closeGameButton = new MessageBoxOption("Close Game", false, false);
            var cancelButton = new MessageBoxOption("Cancel", false, true);

            var response = api.Dialogs.ShowMessage("", GameData.GameName, MessageBoxImage.None, new List<MessageBoxOption>
            {
                returnToGameButton, closeGameButton, cancelButton
            });

            if (response.Title == "Back to Game") ReturnToGame(GameData);
            if (response.Title == "Close Game") CloseGame(GameData);
        }

        private static NowPlayingData CreateNowPlayingData(IPlayniteAPI api, Game game, int? processId)
        {
            if (game == null) return null;
            return new NowPlayingData
            {
                GameName = game.Name,
                ProcessId = FindRunningGameProcess(game)?.Id ?? processId ?? -1,
                IconPath = GetFullIconPath(api, game),
            };
        }

        private static string GetFullIconPath(IPlayniteAPI api, Game game)
        {
            if (string.IsNullOrEmpty(game.CoverImage))
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
        private static Process FindRunningGameProcess(Game game)
        {
            if (game == null) return null;

            try
            {
                // Get all executable files in the game installation directory and subdirectories
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
                        // Add variations like "game-win64", "game_launcher", etc.
                        additionalNames.Add(exe.Replace("-", ""));
                        additionalNames.Add(exe.Replace("_", ""));
                        additionalNames.Add(exe.Replace(" ", ""));
                        additionalNames.Add(exe.Replace("-", " "));
                        additionalNames.Add(exe.Replace("_", " "));
                        additionalNames.Add(exe.Replace(" ", " "));
                    }
                    gameExecutables.AddRange(additionalNames);

                    logger.Debug($"Found {gameExecutables.Count} potential game executables in {game.InstallDirectory}");
                }
                catch (Exception ex)
                {
                    logger.Error($"Error scanning game directory: {ex.Message}");
                }

                // Get the game name words for title comparison
                string gameName = game.Name;
                string[] gameNameWords = gameName.ToLower().Split(new char[] { ' ', '-', '_', ':', '.', '(', ')', '[', ']' },
                    StringSplitOptions.RemoveEmptyEntries);

                logger.Debug($"Game name for matching: {gameName}, split into {gameNameWords.Length} words");

                // Get all processes with main window
                Process[] allProcesses = Process.GetProcesses()
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
                    .ToArray();

                var candidates = new List<Process>();
                var nameMatchCandidates = new List<Process>();
                var titleMatchCandidates = new List<(Process Process, int MatchCount)>();
                var inaccessibleCandidates = new List<Process>();

                DateTime gameStartTime = DateTime.Now; // Use current time as fallback

                foreach (var p in allProcesses)
                {
                    try
                    {
                        // Check memory usage first
                        if (!p.HasExited && p.WorkingSet64 > 100 * 1024 * 1024)
                        {
                            // Check if process name matches any executable in the game folder
                            bool nameMatches = gameExecutables.Any(exe =>
                                string.Equals(exe, p.ProcessName, StringComparison.OrdinalIgnoreCase));

                            // Check window title for matches with game name
                            int titleMatchScore = 0;
                            if (p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(p.MainWindowTitle))
                            {
                                string[] windowTitleWords = p.MainWindowTitle.ToLower().Split(new char[] { ' ', '-', '_', ':', '.', '(', ')', '[', ']' },
                                    StringSplitOptions.RemoveEmptyEntries);

                                // Count how many words from the game name appear in the window title
                                titleMatchScore = gameNameWords.Count(gameWord =>
                                    windowTitleWords.Any(titleWord => titleWord.Contains(gameWord) || gameWord.Contains(titleWord)));
                            }

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
                                    // Path doesn't match but name does - this is a good candidate
                                    nameMatchCandidates.Add(p);
                                }
                                else if (titleMatchScore > 0)
                                {
                                    // Window title matches game name
                                    titleMatchCandidates.Add((p, titleMatchScore));
                                }
                            }
                            catch
                            {
                                // Can't access module info (likely due to 32/64 bit mismatch)
                                if (nameMatches)
                                {
                                    // Process name matches executable - higher priority
                                    nameMatchCandidates.Add(p);
                                }
                                else if (titleMatchScore > 0)
                                {
                                    // Window title matches game name
                                    titleMatchCandidates.Add((p, titleMatchScore));
                                }
                                else
                                {
                                    // No name match, but memory usage matches
                                    inaccessibleCandidates.Add(p);
                                }
                            }
                        }
                    }
                    catch { /* Skip processes we can't access at all */ }
                }

                // Priority order: direct path match, window title match, process name match, then best guess
                if (candidates.Count > 0)
                {
                    var bestMatch = candidates.OrderByDescending(p => p.WorkingSet64).First();
                    logger.Debug($"Found process with matching path: {bestMatch.ProcessName} (ID: {bestMatch.Id})");
                    return bestMatch;
                }

                if (titleMatchCandidates.Count > 0)
                {
                    // Get the process with the highest title match score
                    var bestMatch = titleMatchCandidates.OrderByDescending(t => t.MatchCount)
                                                        .ThenByDescending(t => t.Process.WorkingSet64)
                                                        .First().Process;
                    logger.Debug($"Found process with matching window title: {bestMatch.ProcessName} (ID: {bestMatch.Id}, Title: {bestMatch.MainWindowTitle})");
                    return bestMatch;
                }

                if (nameMatchCandidates.Count > 0)
                {
                    var bestMatch = nameMatchCandidates.OrderByDescending(p => p.WorkingSet64).First();
                    logger.Debug($"Found process with matching name: {bestMatch.ProcessName} (ID: {bestMatch.Id})");
                    return bestMatch;
                }

                if (inaccessibleCandidates.Count > 0)
                {
                    var bestGuess = inaccessibleCandidates.OrderByDescending(p => p.WorkingSet64).First();
                    logger.Debug($"Using best guess process: {bestGuess.ProcessName} (ID: {bestGuess.Id})");
                    return bestGuess;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error finding game process: {ex.Message}");
            }

            return null;
        }

        private static void ReturnToGame(NowPlayingData data)
        {
            GameStateManager.ReturnToGame(data);
        }

        private static void CloseGame(NowPlayingData data)
        {
            GameStateManager.CloseGame(data);
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

        public static void CloseGame(NowPlayingData gameData)
        {
            if (gameData != null)
            {
                var proc = FindProcessById(gameData.ProcessId);
                if (proc != null)
                {
                    proc.CloseMainWindow();
                    proc.Close();
                }
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
    }
}