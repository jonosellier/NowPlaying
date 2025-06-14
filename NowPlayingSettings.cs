﻿using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Input;

namespace NowPlaying
{
    public class NowPlayingSettings : ObservableObject
    {
        public ICommand OpenDialog { get; set; }
        public ICommand CloseGame { get; set; }
        public ICommand ReturnToGame { get; set; }
        private NowPlayingData _runningGame;

        private Timer _timer = new Timer(10000);

        // Property with notification
        public NowPlayingData RunningGame
        {
            get => _runningGame;
            set
            {
                if (value != null)
                {
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }

                if (_runningGame != value)
                {
                    _runningGame = value;
                    IsGameRunning = _runningGame != null;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isGameRunning = false;
        public bool IsGameRunning
        {
            get => _isGameRunning; set
            {
                _isGameRunning = value;
                OnPropertyChanged();
            }
        }

        private string _sessionLength = "0:00";
        public string SessionLength
        {
            get => _sessionLength; set
            {
                _sessionLength = value;
                OnPropertyChanged();
            }
        }

        public NowPlayingSettings() {
            _timer.Elapsed += (sender, e) =>
            {
                if (RunningGame != null)
                {
                    var duration = DateTime.Now - RunningGame.StartTime;
                    SessionLength = duration.ToString(@"h\:mm");
                } else
                {
                    _timer.Stop();
                    SessionLength = "0:00";
                }
            };
        }
    }

    public class NowPlayingSettingsViewModel : ObservableObject, ISettings
    {
        private readonly NowPlaying plugin;
        private NowPlayingSettings editingClone { get; set; }

        private NowPlayingSettings settings;
        public NowPlayingSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public NowPlayingSettingsViewModel(NowPlaying plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<NowPlayingSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new NowPlayingSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}