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
using WizardsDuel.Io;
using WizardsDuel.States;
using WizardsDuel.Utils;

namespace WizardsDuel {
	class MainClass {
		public static void Main (string[] args) {
			try {
				Logger.Initialize (LogLevel.ALL, true);
				Logger.SetOutFile ();

				Logger.Blacklist ("AddLayer");
				Logger.Blacklist ("AddRule");
				Logger.Blacklist ("AreaAI");
				Logger.Blacklist ("BackgroundMusic");
				Logger.Blacklist ("CalculateLoS");
				Logger.Blacklist ("CanShift");
				Logger.Blacklist ("Effect");
				Logger.Blacklist ("Entity");
				Logger.Blacklist ("EventManager");
				Logger.Blacklist ("GameFactory");
				Logger.Blacklist ("IoManager");
				Logger.Blacklist ("LavaAI");
				Logger.Blacklist ("LoadTilemask");
				Logger.Blacklist ("LoadWorldView");
				Logger.Blacklist ("Main");
				Logger.Blacklist ("MeleeAI");
				Logger.Blacklist ("OnRound");
				Logger.Blacklist ("OutObject");
				Logger.Blacklist ("Run");
				Logger.Blacklist ("SetDungeon");
				Logger.Blacklist ("SetUserEvent");
				Logger.Blacklist ("Skill");
				//Logger.Blacklist ("Simulator");
				Logger.Blacklist ("TestLevel");
				Logger.Blacklist ("TranslateAnimation");
				Logger.Blacklist ("WorldFactory");
				Logger.Blacklist ("XmlUtilities");

				IoManager.Initialize ("Wizards of Unica", 1280, 720);
			}
			catch (Exception ex) {
				Console.WriteLine ("Configuration Error, Aborting: " + ex.ToString ());
				return;
			}

			GameState state = new TitleState ();
			while (true) {
				var inputs = IoManager.GetInputs ();
				if (inputs.Command == InputCommands.QUIT) {
					return;
				}
				state.Logic (inputs);
				GameState.ChangeState (ref state);
				state.Render ();
			}
		}
	}
}
