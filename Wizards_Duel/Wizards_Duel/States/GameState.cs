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
using WizardsDuel.Game;
using WizardsDuel.Utils;
using SFML.Graphics;

namespace WizardsDuel.States
{
	public enum GameStates {
		NO_CHANGE,
		MENU,
		PLAY,
		TEST,
		TITLE,
		QUIT
	}

	/// <summary>
	/// Base GameState class
	/// </summary>
	public class GameState {
		public GameState () {
			this.NextState = GameStates.NO_CHANGE;
		}

		public static void ChangeState(ref GameState state) {
			switch (state.NextState) {
			case GameStates.TEST:
				state = new TestState ();
				break;
			
			case GameStates.TITLE:
				state = new TitleState ();
				break;

			default:
				break;
			}
		}

		public virtual void Logic(Inputs inputs) {
			if (inputs.Command == InputCommands.QUIT) {
				this.NextState = GameStates.QUIT;
			}
		}

		public GameStates NextState {
			get;
			set;
		}

		public virtual void Render() {
			IoManager.Draw ();
		}
	}

	/// <summary>
	/// Test state, for development purposes (not unit testing).
	/// </summary>
	public class TestState: GameState {
		public TestState(): base() {
			IoManager.Clear ();
			WorldView tm;
			Simulator.Instance.Initialize (out tm);
		}

		override public void Logic(Inputs inputs) {
			//var changeTo = "bp_exekiel";
			if (inputs.Command == InputCommands.QUIT) {
				this.NextState = GameStates.QUIT;
			} else if (inputs.Command == InputCommands.UP) {
				Simulator.Instance.SetUserEvent (new ShiftEvent (Simulator.PLAYER_ID, 0, -1));
			} else if (inputs.Command == InputCommands.DOWN) {
				Simulator.Instance.SetUserEvent (new ShiftEvent (Simulator.PLAYER_ID, 0, 1));
			} else if (inputs.Command == InputCommands.LEFT) {
				Simulator.Instance.SetUserEvent (new ShiftEvent (Simulator.PLAYER_ID, -1, 0));
			} else if (inputs.Command == InputCommands.RIGHT) {
				Simulator.Instance.SetUserEvent (new ShiftEvent (Simulator.PLAYER_ID, 1, 0));
			} else if (inputs.Command == InputCommands.UP_RIGHT) {
				Simulator.Instance.SetUserEvent (new ShiftEvent (Simulator.PLAYER_ID, 1, -1));
			} else if (inputs.Command == InputCommands.DOWN_RIGHT) {
				Simulator.Instance.SetUserEvent (new ShiftEvent (Simulator.PLAYER_ID, 1, 1));
			} else if (inputs.Command == InputCommands.UP_LEFT) {
				Simulator.Instance.SetUserEvent (new ShiftEvent (Simulator.PLAYER_ID, -1, -1));
			} else if (inputs.Command == InputCommands.DOWN_LEFT) {
				Simulator.Instance.SetUserEvent (new ShiftEvent (Simulator.PLAYER_ID, -1, 1));
			} else if (inputs.Command == InputCommands.SKIP) {
				Simulator.Instance.SetUserEvent (new SkipEvent (Simulator.PLAYER_ID));
			} else if (inputs.Command == InputCommands.TOGGLE_GRID) {
				Simulator.Instance.ToggleGrid ();
			} else {
				//Logger.Debug ("TestState", "Logic", "Inputs: " + inputs.Command.ToString ());
			}

			//Logger.Debug ("TestState", "Logic", "Current Unicode " + inputs.Unicode);
			switch (inputs.Unicode) {
			/*case "0":
				Logger.Debug ("Main", "main", "Changing player");
				var player = Simulator.Instance.GetPlayer ();
				changeTo = (changeTo == "bp_rake") ? "bp_exekiel" : "bp_rake";
				var tmp = Simulator.Instance.CreateObject ("tmp", changeTo, player.X, player.Y);
				var oo = player.OutObject;
				player.OutObject = tmp.OutObject;
				tmp.OutObject = oo;
				Simulator.Instance.world.worldView.ReferenceObject = player.OutObject;
				Simulator.Instance.DestroyObject ("tmp");
				break;*/

			case "1":
				Simulator.Instance.SelectedSkill = 1;
				break;

			case "2":
				Simulator.Instance.SelectedSkill = 2;
				break;

			case "3":
				Simulator.Instance.SelectedSkill = 3;
				break;

			case "4":
				Simulator.Instance.SelectedSkill = 4;
				break;

			default:
				break;
			}

			Simulator.Instance.DoLogic ();
		}
	}

	public class TitleState: GameState {
		private int page = 0;

		public TitleState(): base() {
			IoManager.Clear ();
			IoManager.AddWidget(new Icon("0startscreen01_big.jpg", new IntRect(0,0,1280,720)));
			var label = new Label ("Click to start", 32, "munro.ttf");
			label.AlignCenter = true;
			label.Position = new SFML.Window.Vector2f (IoManager.Width/2, IoManager.Height - 64);
			IoManager.AddWidget (label);
		}

		override public void Logic(Inputs inputs) {
			if (inputs.Command == InputCommands.QUIT) {
				this.NextState = GameStates.QUIT;
			} else if (inputs.Command != InputCommands.NONE) {
				switch (this.page) {
				case 0:
					this.page++;
					IoManager.Clear ();
					IoManager.AddWidget (new Icon ("00_tutorial.png", new IntRect (0, 0, 1280, 720)));
					break;

				default:
					IoManager.Clear ();
					var label = new Label ("Loading", 32, "munro.ttf");
					label.AlignCenter = true;
					label.Position = new SFML.Window.Vector2f (IoManager.Width / 2, IoManager.Height / 2);
					IoManager.AddWidget (label);
					IoManager.ForceDraw ();
					this.NextState = GameStates.TEST;
					break;
				}
			} else {
				//Logger.Debug ("TestState", "Logic", "Inputs: " + inputs.Command.ToString ());
			}
		}
	}
}

