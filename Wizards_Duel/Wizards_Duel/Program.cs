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
using System.Configuration;
using SFML.Graphics;
using WizardsDuel.Game;
using WizardsDuel.Io;
using WizardsDuel.Utils;

namespace WizardsDuel
{
	class MainClass {
		public static void Main (string[] args) {
			try {
				/*var reader = new AppSettingsReader ();
				var width = (int)reader.GetValue("width", typeof(Int32));
				var height = (int)reader.GetValue("height", typeof(Int32));
				var logLevel = (int)reader.GetValue("logLevel", typeof(Int32));
				var logPrintToScreen = (bool)reader.GetValue("logPrint", typeof(bool));
				var logPrefix = (string)reader.GetValue("logFileName_prefix", typeof(string));
				var logUseTimestamp = (bool)reader.GetValue("logFileName_timestamped", typeof(bool));*/

				Logger.Initialize (LogLevel.ALL, true);
				Logger.SetOutFile ();
				Logger.Blacklist("XmlUtilities");
				Logger.Blacklist("LoadWorldView");
				Logger.Blacklist("LoadTilemask");
				Logger.Blacklist("AddRule");
				Logger.Blacklist("AddLayer");
				//Logger.Blacklist("BackgroundMusic");
				IO.Initialize ("Wizard's Duel", 800, 480);
				//Logger.Initialize (LogLevel.ALL, logPrintToScreen);
				//Logger.SetOutFile (logPrefix, logUseTimestamp);
				//IO.Initialize ("Wizard's Duel", width, height);
			} catch (Exception ex) {
				Console.WriteLine ("Configuration Error, Aborting: " + ex.ToString());
				return;
			}

			WorldView tm;
			var showingGrid = false;
			var simulator = new Simulator (out tm);
			tm.EnableGrid(showingGrid);

			simulator.AddLight (400, 240, 200, new Color(254, 250, 235));
			IO.PlayMusic ("test1");
			while (true) {
				var inputs = IO.GetInputs ();
				if (inputs.Command == InputCommands.QUIT) {
					return;
				} else if (inputs.Command == InputCommands.UP) {
					simulator.SetUserEvent(new ShiftEvent(Simulator.PLAYER_ID, 0, -1));
				} else if (inputs.Command == InputCommands.DOWN) {
					simulator.SetUserEvent(new ShiftEvent(Simulator.PLAYER_ID, 0, 1));
				} else if (inputs.Command == InputCommands.LEFT) {
					simulator.SetUserEvent(new ShiftEvent(Simulator.PLAYER_ID, -1, 0));
				} else if (inputs.Command == InputCommands.RIGHT) {
					simulator.SetUserEvent(new ShiftEvent(Simulator.PLAYER_ID, 1, 0));
				} else if (inputs.Command == InputCommands.UP_RIGHT) {
					simulator.SetUserEvent(new ShiftEvent(Simulator.PLAYER_ID, 1, -1));
				} else if (inputs.Command == InputCommands.DOWN_RIGHT) {
					simulator.SetUserEvent(new ShiftEvent(Simulator.PLAYER_ID, 1, 1));
				} else if (inputs.Command == InputCommands.UP_LEFT) {
					simulator.SetUserEvent(new ShiftEvent(Simulator.PLAYER_ID, -1, -1));
				} else if (inputs.Command == InputCommands.DOWN_LEFT) {
					simulator.SetUserEvent(new ShiftEvent(Simulator.PLAYER_ID, -1, 1));
				} else if (inputs.Command == InputCommands.MOUSE_RIGHT) {
					/*Random rnd = new Random();
					var colmin = 100;//100
					var colmax = 155;//255
					var col = new Color ((byte)rnd.Next(colmin, colmax), (byte)rnd.Next(colmin, colmax), (byte)rnd.Next(colmin, colmax));
					var rad = (float)rnd.Next (50, 150);
					simulator.AddLight (inputs.MouseX, inputs.MouseY, rad, col);*/
					simulator.Cast (Simulator.PLAYER_ID);
				} else if (inputs.Command == InputCommands.TOGGLE_GRID) {
					showingGrid = !showingGrid;
					tm.EnableGrid(showingGrid);
				} else {
					//Console.WriteLine (inputs.Command.ToString ());
				}

				switch (inputs.Unicode) {
				case "1":
					Logger.Debug ("Main", "main", "Set next loop to 1");
					IO.SetNextMusicLoop ("test1");
					break;
				case "2":
					Logger.Debug ("Main", "main", "Set next loop to 2");
					IO.SetNextMusicLoop ("test2");
					break;
				case "3":
					Logger.Debug ("Main", "main", "Set next loop to 3");
					IO.SetNextMusicLoop ("test3");
					break;
				case "4":
					Logger.Debug ("Main", "main", "Set next loop to 4");
					IO.SetNextMusicLoop ("test4");
					break;
				case "5":
					Logger.Debug ("Main", "main", "Set next loop to 5");
					IO.SetNextMusicLoop ("test5");
					break;
				case "6":
					Logger.Debug ("Main", "main", "Set next loop to 6");
					IO.SetNextMusicLoop ("test6");
					break;
				default:
					break;
				}

				simulator.DoLogic ();
				IO.Draw ();
			}
		}
	}
}
