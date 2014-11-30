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

namespace WizardsDuel.States
{
	public enum GameStates {
		NO_CHANGE,
		MENU,
		PLAY,
		TEST,
		QUIT
	}

	public class GameState
	{
		public GameState () {
			this.NextState = GameStates.NO_CHANGE;
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
			return;
		}
	}
}

