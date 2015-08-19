// Wizard's Duel, a procedural tactical RPG
// Copyright (C) 2015  Luca Carbone
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
using WizardsDuel.Utils;

namespace WizardsDuel.Game
{
	public class Effect {
		protected int lastInitiative = 0;

		public int Duration { get; set; }

		/// <summary>
		/// Initialize the effect, setting the initiative counter
		/// It should be called by all its derivates
		/// </summary>
		virtual public void OnAdded() {
			this.lastInitiative = Simulator.Instance.InitiativeCount;
		}

		virtual public void OnRemoved() {
			return;
		}

		/// <summary>
		/// It should be called by all its derivates to update the duration and clear the
		/// effect if the duration is expired
		/// </summary>
		virtual public void OnRound() {
			var deltaInitiative = Simulator.Instance.InitiativeCount - this.lastInitiative;
			this.Duration -= deltaInitiative;
			if (this.Duration < 1) {
				this.RemoveMe = true;
			}
		}

		public Entity Parent { get; internal set; }

		public bool RemoveMe { get; protected set; }
	}

	public class BurningEffect: Effect {
		override public void OnAdded() {
			base.OnAdded ();
			this.Parent.OutObject.Color = new SFML.Graphics.Color(255, 255, 127);
			Simulator.Instance.CreateParticleOn ("p_burning", this.Parent.ID);
			this.Duration = Simulator.ROUND_LENGTH * 9 + 1;
		}

		override public void OnRemoved() {
			this.Parent.OutObject.Color = SFML.Graphics.Color.White;
			Simulator.Instance.RemoveParticle (this.Parent.ID, "p_freeze");
			this.Parent.Static = false;
		}
	}

	public class FreezeEffect: Effect {
		override public void OnAdded() {
			base.OnAdded ();
			this.Parent.OutObject.Color = new SFML.Graphics.Color(127, 255, 255);
			//this.Parent.Static = true;
			Simulator.Instance.CreateParticleOn ("p_freeze", this.Parent.ID);
			this.Duration = Simulator.ROUND_LENGTH * 9 + 1;
		}

		override public void OnRemoved() {
			this.Parent.OutObject.Color = SFML.Graphics.Color.White;
			Simulator.Instance.RemoveParticle (this.Parent.ID, "p_freeze");
			this.Parent.Static = false;
		}
	}
}

