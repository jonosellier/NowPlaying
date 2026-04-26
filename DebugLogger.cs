using System;
using System.Diagnostics;
using Playnite.SDK;

namespace NowPlaying
{
    /// <summary>
    /// A wrapper around ILogger that optionally writes output to the terminal when debugging.
    /// </summary>
    public class DebugLogger : ILogger
    {
        private readonly ILogger _innerLogger;
        private bool _writeToTerminal;

        /// <summary>
        /// Gets or sets a value indicating whether to write log output to the terminal.
        /// </summary>
        public bool WriteToTerminal
        {
            get => _writeToTerminal;
            set => _writeToTerminal = value;
        }

        /// <summary>
        /// Initializes a new instance of the DebugLogger class.
        /// </summary>
        /// <param name="innerLogger">The underlying logger to wrap.</param>
        /// <param name="writeToTerminal">Whether to write output to the terminal. Default is true when debugging.</param>
        public DebugLogger(ILogger innerLogger, bool? writeToTerminal = null)
        {
            _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
            _writeToTerminal = writeToTerminal ?? IsDebugMode();
        }

        /// <summary>
        /// Determines if the application is running in debug mode.
        /// </summary>
        private static bool IsDebugMode()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Writes a debug message to the logger and optionally to the terminal.
        /// </summary>
        public void Debug(string message)
        {
            _innerLogger.Debug(message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
            }
        }

        /// <summary>
        /// Writes a debug message with exception details to the logger and optionally to the terminal.
        /// </summary>
        public void Debug(Exception exception, string message)
        {
            _innerLogger.Debug(exception, message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}\n{exception}");
            }
        }

        /// <summary>
        /// Writes an info message to the logger and optionally to the terminal.
        /// </summary>
        public void Info(string message)
        {
            _innerLogger.Info(message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
            }
        }

        /// <summary>
        /// Writes an info message with exception details to the logger and optionally to the terminal.
        /// </summary>
        public void Info(Exception exception, string message)
        {
            _innerLogger.Info(exception, message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[INFO] {message}\n{exception}");
            }
        }

        /// <summary>
        /// Writes a warning message to the logger and optionally to the terminal.
        /// </summary>
        public void Warn(string message)
        {
            _innerLogger.Warn(message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] {message}");
            }
        }

        /// <summary>
        /// Writes a warning message with exception details to the logger and optionally to the terminal.
        /// </summary>
        public void Warn(Exception exception, string message)
        {
            _innerLogger.Warn(exception, message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] {message}\n{exception}");
            }
        }

        /// <summary>
        /// Writes an error message to the logger and optionally to the terminal.
        /// </summary>
        public void Error(string message)
        {
            _innerLogger.Error(message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
            }
        }

        /// <summary>
        /// Writes an error message with exception details to the logger and optionally to the terminal.
        /// </summary>
        public void Error(Exception exception, string message)
        {
            _innerLogger.Error(exception, message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] {message}\n{exception}");
            }
        }

        /// <summary>
        /// Writes a trace message to the logger and optionally to the terminal.
        /// </summary>
        public void Trace(string message)
        {
            _innerLogger.Trace(message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[TRACE] {message}");
            }
        }

        /// <summary>
        /// Writes a trace message with exception details to the logger and optionally to the terminal.
        /// </summary>
        public void Trace(Exception exception, string message)
        {
            _innerLogger.Trace(exception, message);
            if (_writeToTerminal)
            {
                System.Diagnostics.Debug.WriteLine($"[TRACE] {message}\n{exception}");
            }
        }
    }
}
