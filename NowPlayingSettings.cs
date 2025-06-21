using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Windows.Input;

namespace NowPlaying
{
    public class NowPlayingSettings : ObservableObject, ISettings
    {
        private readonly NowPlaying plugin;

        // Only these properties will be serialized and saved
        private CloseBehavior _closeBehavior = CloseBehavior.CloseAndEnd;
        public CloseBehavior CloseBehavior
        {
            get => _closeBehavior;
            set => SetValue(ref _closeBehavior, value);
        }

        private bool _confirmClose = false;
        public bool ConfirmClose
        {
            get => _confirmClose;
            set => SetValue(ref _confirmClose, value);
        }

        // Backups for cancel functionality
        private CloseBehavior _closeBehaviorBackup;
        private bool _confirmCloseBackup;

        // Parameterless constructor required for LoadPluginSettings
        public NowPlayingSettings()
        {
            InitializeRuntimeProperties();
        }

        public NowPlayingSettings(NowPlaying plugin)
        {
            this.plugin = plugin;
            InitializeRuntimeProperties();

            try
            {
                // Load saved settings
                var savedSettings = plugin.LoadPluginSettings<NowPlayingSettings>();
                if (savedSettings != null)
                {
                    CloseBehavior = savedSettings.CloseBehavior;
                    if(savedSettings.ConfirmClose != null)
                    {
                        ConfirmClose = savedSettings.ConfirmClose;
                    }
                }
            }
            catch (Exception ex)
            {
                // If loading fails (e.g., due to old incompatible settings), use defaults
                CloseBehavior = CloseBehavior.CloseAndEnd;
                ConfirmClose = false;
            }
        }

        private void InitializeRuntimeProperties()
        {
            // Initialize all runtime properties that shouldn't be serialized
            _timer = new Timer(10000);
            _timer.Elapsed += (sender, e) =>
            {
                if (RunningGame != null)
                {
                    var duration = DateTime.Now - RunningGame.StartTime;
                    SessionLength = duration.ToString(@"h\:mm");
                }
                else
                {
                    _timer.Stop();
                    SessionLength = "0:00";
                }
            };
        }

        #region Runtime Properties (Not Serialized)

        private Timer _timer;
        private NowPlayingData _runningGame;
        private bool _isGameRunning = false;
        private string _sessionLength = "0:00";

        [DontSerialize]
        public RelayCommand OpenDialog { get; set; }

        [DontSerialize]
        public RelayCommand CloseGame { get; set; }

        [DontSerialize]
        public RelayCommand ReturnToGame { get; set; }

        [DontSerialize]
        public NowPlayingData RunningGame
        {
            get => _runningGame;
            set
            {
                if (value != null)
                {
                    _timer?.Start();
                }
                else
                {
                    _timer?.Stop();
                }

                if (_runningGame != value)
                {
                    _runningGame = value;
                    IsGameRunning = _runningGame != null;
                    OnPropertyChanged();
                }
            }
        }

        [DontSerialize]
        public bool IsGameRunning
        {
            get => _isGameRunning;
            set
            {
                _isGameRunning = value;
                OnPropertyChanged();
            }
        }

        [DontSerialize]
        public string SessionLength
        {
            get => _sessionLength;
            set
            {
                _sessionLength = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public void BeginEdit()
        {
            _closeBehaviorBackup = CloseBehavior;
        }

        public void CancelEdit()
        {
            CloseBehavior = _closeBehaviorBackup;
        }

        public void EndEdit()
        {
            plugin?.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }

    public enum CloseBehavior
    {
        [Description("Close Window (asking)")]
        CloseWindow,

        [Description("End Task (telling)")]
        EndTask,

        [Description("Close then End Task")]
        CloseAndEnd
    }
}