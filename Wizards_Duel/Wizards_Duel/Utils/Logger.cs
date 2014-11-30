// Wizard's Duel, a procedural tactical RPG
// Copyright (C) 2014  Luca Carbone
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;

namespace WizardsDuel.Utils
{
	public enum LogLevel {
		NO_LOG = 0x0000,
		DEBUG = 0x1000,
		INFO = 0x0100,
		WARNING = 0x0010,
		ERROR = 0x0001,
		ALL = 0x1111
	}

	public static class Logger
	{
		private static LogLevel logLevel = LogLevel.NO_LOG;
		private static bool isToDisplay = false;
		private static List<string> blacklist = new List<string>();
		private static StreamWriter logFile;

		private static string LOGS_DIRECTORY = "Logs" + Path.DirectorySeparatorChar;

		// XXX This is just a trick to call a Deconstructor for a static class
		static readonly Finalizer finalizer = new Finalizer();
		sealed class Finalizer {
			~Finalizer() {
				Logger.Close();
			}
		}

		/// <summary>
		/// No logs coming from the specified module/function will be displayed.
		/// </summary>
		/// <param name="module">Module.</param>
		public static void Blacklist(string module) {
			Logger.blacklist.Add(module);
		}

		/// <summary>
		/// Clear the logger and finalize the log file.
		/// </summary>
		public static void Close() {
			try {
				Logger.logFile.WriteLine("</log>");
				Logger.logFile.Close();
			} catch {}
		}

		/// <summary>
		/// Log a debug string.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		/// <param name="functionName">Function name.</param>
		/// <param name="msg">Accompanying message.</param>
		public static void Debug(string moduleName, string functionName, string msg) {
			Logger.Log (moduleName, functionName, LogLevel.DEBUG, msg);
		}

		/// <summary>
		/// Log a error string.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		/// <param name="functionName">Function name.</param>
		/// <param name="msg">Accompanying message.</param>
		public static void Error(string moduleName, string functionName, string msg) {
			Logger.Log (moduleName, functionName, LogLevel.ERROR, msg);
		}

		/// <summary>
		/// Log a info string.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		/// <param name="functionName">Function name.</param>
		/// <param name="msg">Accompanying message.</param>
		public static void Info(string moduleName, string functionName, string msg) {
			Logger.Log (moduleName, functionName, LogLevel.INFO, msg);
		}

		/// <summary>
		/// Initialize the logger.
		/// </summary>
		/// <param name="level">Level.</param>
		/// <param name="printToScreen">If set to <c>true</c> print to screen.</param>
		public static void Initialize (LogLevel level, bool printToScreen = false) {
			Logger.logLevel = level;
			Logger.isToDisplay = printToScreen;

			// If "NO_LOG" then do nothing, the log function will never execute
			if (level != LogLevel.NO_LOG) {
				// Print a message to inform the user that the logger is fine
				var currTime = DateTime.Now.ToLongTimeString();
				Console.WriteLine(
					currTime +
					" | logger.init [" + LogLevel.INFO.ToString() +
					"]: Running at log level " + Logger.logLevel.ToString()
				);
			}
		}

		public static bool IsToDisplay {
			get { return Logger.isToDisplay; }
		}

		/// <summary>
		/// Determines if is to log the specified level.
		/// </summary>
		/// <returns><c>true</c> if is to log the specified level; otherwise, <c>false</c>.</returns>
		/// <param name="level">Level.</param>
		public static bool IsToLog (LogLevel level) {
			return (((ushort)level & (ushort)Logger.logLevel) != 0);
			/*var a = (ushort)level;
			var b = (ushort)Logger.logLevel;
			var c = a & b;
			Console.WriteLine("check " + a.ToString() + " vs " + b.ToString() + " res: " + c.ToString());
			return true;*/
		}

		/// <summary>
		/// Determines if is to log the specified module.
		/// </summary>
		/// <returns><c>true</c> if is to log the specified module; otherwise, <c>false</c>.</returns>
		/// <param name="module">Module.</param>
		public static bool IsToLog(string module) {
			return !Logger.blacklist.Contains (module);
		}

		private static bool IsToWrite {
			get;
			set;
		}

		/// <summary>
		/// Logs a message. In general it is better to use one of the helper functions:
		/// <see cref="Wizardsule.Utils.Logger.Debug"/>, <see cref="Wizardsule.Utils.Logger.Error"/>,
		/// <see cref="Wizardsule.Utils.Logger.Info"/>, <see cref="Wizardsule.Utils.Logger.Warning"/>
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		/// <param name="functionName">Function name.</param>
		/// <param name="level">Level.</param>
		/// <param name="msg">Message.</param>
		public static void Log(string moduleName, string functionName, LogLevel level, string msg) {
			if (Logger.IsToLog(level) && Logger.IsToLog(moduleName) && Logger.IsToLog(functionName)) {
				var currTime = DateTime.Now.ToLongTimeString();
				if (Logger.IsToWrite) {
					Logger.logFile.WriteLine(
						"<msg ts='" + currTime + 
						"' type='" + level.ToString() + 
						"' module='" + moduleName + 
						"' func='" + functionName + 
						"'>" + msg + "</msg>"
					);
				}
				if (Logger.IsToDisplay) {
					Console.WriteLine(
						currTime + " | " + moduleName + "." + functionName + " [" + level.ToString() + "]: " + msg
					);
				}
			}
		}

		/// <summary>
		/// Log a warning string.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		/// <param name="functionName">Function name.</param>
		/// <param name="msg">Accompanying message.</param>
		public static void Warning(string moduleName, string functionName, string msg) {
			Logger.Log (moduleName, functionName, LogLevel.WARNING, msg);
		}

		/// <summary>
		/// Sets the out file.
		/// </summary>
		/// <param name="outFile">the name of the outputfile, without the extension, a timestamp will be automatically added.</param>
		/// <param name="appendTimestampToName">If set to <c>true</c> append timestamp to name, to keep logs separated.</param>
		public static void SetOutFile(string outFile = "log", bool appendTimestampToName = false) {
			try {
				// Create the logs dir if necessary
				//var logDir = System.Reflection.Assembly.GetExecutingAssembly().CodeBase + Logger.LOGS_DIRECTORY;
				Directory.CreateDirectory(Logger.LOGS_DIRECTORY);
				// Open the log file
				string currTime = "";
				if (appendTimestampToName) {
					currTime = "_" + DateTime.Now.ToLongTimeString();
				}
				string fileName = Logger.LOGS_DIRECTORY + outFile + currTime + ".xml";

				Logger.logFile = new StreamWriter(fileName);
				Logger.logFile.AutoFlush = true;
				Logger.logFile.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
				Logger.logFile.WriteLine("<log timestamp='" + currTime + "' loglevel='" + Logger.logLevel.ToString() + "'>");
				Logger.IsToWrite = true;
				Logger.Info("Logger", "SetOutFile", "Logger successfully initiated");
			}
			catch {
				Logger.Error("Logger", "SetOutFile", "Cannot open log file " + Logger.LOGS_DIRECTORY + outFile + ".xml");
			}
		}
	}
}

