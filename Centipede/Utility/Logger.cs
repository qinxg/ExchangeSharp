﻿#region Imports
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;using NLog;
using NLog.Config;

#endregion Imports

namespace Centipede
{
    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Trace / Diagnostic
        /// </summary>
        Trace,

        /// <summary>
        /// Trace / Diagnostic
        /// </summary>
        Diagnostic = Trace,

        /// <summary>
        /// Debug
        /// </summary>
        Debug,

        /// <summary>
        /// Information / Info
        /// </summary>
        Information,

        /// <summary>
        /// Information / Info
        /// </summary>
        Info = Information,

        /// <summary>
        /// Warning / Warn
        /// </summary>
        Warning,

        /// <summary>
        /// Warning / Warn
        /// </summary>
        Warn = Warning,

        /// <summary>
        /// Error / Exception
        /// </summary>
        Error,

        /// <summary>
        /// Error / Exception
        /// </summary>
        Exception = Error,

        /// <summary>
        /// Critical / Fatal
        /// </summary>
        Critical,

        /// <summary>
        /// Critical / Fatal
        /// </summary>
        Fatal = Critical,

        /// <summary>
        /// Off / None
        /// </summary>
        Off,

        /// <summary>
        /// Off / None
        /// </summary>
        None = Off
    }

    /// <summary>
    /// Centipede logger. Will never throw exceptions.
    /// Currently the Centipede logger uses NLog internally, so make sure it is setup in your app.config file or nlog.config file.
    /// </summary>
    public static class Logger
    {
        private static readonly NLog.Logger logger;

        static Logger()
        {
            try
            {
                LogFactory factory = LogManager.LoadConfiguration(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath);
                if (factory.Configuration.AllTargets.Count == 0)
                {
                    if (File.Exists("nlog.config"))
                    {
                        factory = LogManager.LoadConfiguration("nlog.config");
                    }
                    else
                    {
                        //System.IO.StringReader sr = new System.IO.StringReader(CentipedeResources.NLog_config);
                        //System.Xml.XmlReader xr = System.Xml.XmlReader.Create(sr);
                        //LogManager.Configuration = new XmlLoggingConfiguration(xr, Directory.GetCurrentDirectory());
                        factory = LogManager.LogFactory;
                    }
                }
                logger = factory.GetCurrentClassLogger();
            }
            catch (Exception ex)
            {
                // log to console as no other logger is available
                Console.WriteLine("Failed to initialize logger: {0}", ex);
            }
        }

        /// <summary>
        /// Map IPBan log level to NLog log level
        /// </summary>
        /// <param name="logLevel">IPBan log level</param>
        /// <returns>NLog log level</returns>
        public static NLog.LogLevel GetNLogLevel(Centipede.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case Centipede.LogLevel.Critical: return NLog.LogLevel.Fatal;
                case Centipede.LogLevel.Debug: return NLog.LogLevel.Debug;
                case Centipede.LogLevel.Error: return NLog.LogLevel.Error;
                case Centipede.LogLevel.Information: return NLog.LogLevel.Info;
                case Centipede.LogLevel.Trace: return NLog.LogLevel.Trace;
                case Centipede.LogLevel.Warning: return NLog.LogLevel.Warn;
                default: return NLog.LogLevel.Off;
            }
        }

        /*
        /// <summary>
        /// Map Microsoft log level to NLog log level
        /// </summary>
        /// <param name="logLevel">Microsoft log level</param>
        /// <returns>NLog log level</returns>
        public static NLog.LogLevel GetNLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Critical: return NLog.LogLevel.Fatal;
                case Microsoft.Extensions.Logging.LogLevel.Debug: return NLog.LogLevel.Debug;
                case Microsoft.Extensions.Logging.LogLevel.Error: return NLog.LogLevel.Error;
                case Microsoft.Extensions.Logging.LogLevel.Information: return NLog.LogLevel.Info;
                case Microsoft.Extensions.Logging.LogLevel.Trace: return NLog.LogLevel.Trace;
                case Microsoft.Extensions.Logging.LogLevel.Warning: return NLog.LogLevel.Warn;
                default: return NLog.LogLevel.Off;
            }
        }
        */

        /// <summary>
        /// Log an error
        /// </summary>
        /// <param name="ex">Error</param>
        public static void Error(Exception ex)
        {
            Write(Centipede.LogLevel.Error, "Exception: " + ex.ToString());
        }

        /// <summary>
        /// Log an error
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="args">Format arguments</param>
        public static void Error(string text, params object[] args)
        {
            Write(Centipede.LogLevel.Error, text, args);
        }

        /// <summary>
        /// Log an error
        /// </summary>
        /// <param name="ex">Error</param>
        /// <param name="text">Text with format</param>
        /// <param name="args">Format args</param>
        public static void Error(Exception ex, string text, params object[] args)
        {
            Write(Centipede.LogLevel.Error, string.Format(text, args) + ": " + ex.ToString());
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="text">Text with format</param>
        /// <param name="args">Format args</param>
        public static void Warn(string text, params object[] args)
        {
            Write(Centipede.LogLevel.Warning, text, args);
        }
        /// <summary>
        /// Log an info message
        /// </summary>
        /// <param name="text">Text with format</param>
        /// <param name="args">Format args</param>
        public static void Info(string text, params object[] args)
        {
            Write(Centipede.LogLevel.Info, text, args);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        /// <param name="text">Text with format</param>
        /// <param name="args">Format args</param>
        public static void Debug(string text, params object[] args)
        {
            Write(Centipede.LogLevel.Debug, text, args);
        }

        /// <summary>
        /// Write to the log
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="text">Text with format</param>
        /// <param name="args">Format args</param>
        public static void Write(Centipede.LogLevel level, string text, params object[] args)
        {
            try
            {
                if (args != null && args.Length != 0)
                {
                    text = string.Format(text, args);
                }
                logger?.Log(GetNLogLevel(level), text);
            }
            catch
            {
                // oh well...
            }
        }
    }
}