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
		public readonly string ID = "NULL_EFFECT";
		public const int INFINITE_DURATION = 999999999;
		protected int lastInitiative = 0;

		virtual public Effect Clone {
			get {
				var res = new Effect ();
				res.lastInitiative = this.lastInitiative;
				res.Duration = this.Duration;
				return res;
			}
		}

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
			Logger.Debug ("Effect", "OnRound", "Old duration " + this.Duration.ToString());
			this.Duration -= Simulator.Instance.InitiativeCount - this.lastInitiative;
			this.lastInitiative = Simulator.Instance.InitiativeCount;
			Logger.Debug ("Effect", "OnRound", "Updated duration " + this.Duration.ToString());
			if (this.Duration < 1) {
				Logger.Debug ("Effect", "OnRound", "Set to be removed from " + this.Parent.ID);
				this.RemoveMe = true;
			}
		}

		public Entity Parent { get; internal set; }

		virtual public int ProcessDamage(int howMuch, string type) {
			return howMuch;
		}

		public bool RemoveMe { get; protected set; }

		public Entity Target { get; set; }
	}

	public class BurningEffect: Effect {
		new public readonly string ID = "BURN_EFFECT";

		public BurningEffect(int duration = Simulator.ROUND_LENGTH * 4, int strength = 1) {
			this.Duration = duration + 1;
			this.Strength = strength;
		}

		override public Effect Clone {
			get {
				var res = new BurningEffect ();
				res.lastInitiative = this.lastInitiative;
				res.Duration = this.Duration;
				res.Strength = this.Strength;
				return res;
			}
		}

		override public void OnAdded() {
			base.OnAdded ();
			this.Parent.OutObject.Color = new SFML.Graphics.Color(255, 127, 127);
			Simulator.Instance.CreateParticleOn ("p_burning", this.Parent.ID);
		}

		override public void OnRemoved() {
			this.Parent.OutObject.Color = SFML.Graphics.Color.White;
			Simulator.Instance.RemoveParticle (this.Parent.ID, "p_burning");
		}

		override public void OnRound() {
			base.OnRound ();
			this.Parent.Damage(this.Strength, Simulator.DAMAGE_TYPE_FIRE);
		}

		public int Strength { get; set; }
	}

	public class FreezeEffect: Effect {
		new public readonly string ID = "FREEZE_EFFECT";

		public FreezeEffect(int duration = Simulator.ROUND_LENGTH * 4) {
			this.Duration = duration + 1;
		}

		override public Effect Clone {
			get {
				var res = new FreezeEffect ();
				res.lastInitiative = this.lastInitiative;
				res.Duration = this.Duration;
				return res;
			}
		}

		override public void OnAdded() {
			base.OnAdded ();
			this.Parent.Frozen = true;
			this.Parent.OutObject.StopAnimation = true;
			this.Parent.OutObject.Color = new SFML.Graphics.Color(127, 255, 255);
			Simulator.Instance.CreateParticleOn ("p_freeze", this.Parent.ID);
		}

		override public void OnRemoved() {
			this.Parent.Frozen = false;
			this.Parent.OutObject.StopAnimation = false;
			this.Parent.OutObject.Color = SFML.Graphics.Color.White;
			Simulator.Instance.RemoveParticle (this.Parent.ID, "p_freeze");
		}
	}

	public class GuardEffect: Effect {
		new public string ID = "GUARD_EFFECT";

		public GuardEffect() {
			this.Duration = Simulator.ROUND_LENGTH * 3 + 1;
			this.Strength = 1;
			this.Type = Simulator.DAMAGE_TYPE_UNTYPED;
		}

		override public Effect Clone {
			get {
				var res = new GuardEffect ();
				res.lastInitiative = lastInitiative;
				res.Duration = Duration;
				res.Strength = this.Strength;
				res.Type = this.Type;
				return res;
			}
		}

		override public void OnAdded() {
			base.OnAdded ();
			Logger.Debug ("GuardEffect", "OnAdded", "Adding effect to " + this.Parent.ID);
			this.Parent.OutObject.SetAnimation ("CAST1");
			Simulator.Instance.CreateParticleOn ("p_truce", this.Parent.ID);
			//this.Parent.SetVar ("armor", 1);
		}

		override public void OnRemoved() {
			//this.Parent.SetVar ("armor", 0);
			Logger.Debug ("GuardEffect", "OnRemoved", "Removing effect to " + this.Parent.ID);
			Simulator.Instance.RemoveParticle (this.Parent.ID, "p_truce");
		}

		override public int ProcessDamage(int howMuch, string type) {
			if (this.Type == Simulator.DAMAGE_TYPE_UNTYPED || type == this.Type) {
				howMuch -= this.Strength;
			}
			Logger.Debug ("GuardEffect", "ProcessDamage", "Reducing damage to " + this.Parent.ID + " to " + howMuch.ToString());
			return (howMuch < 0) ? 0 : howMuch;
		}

		public int Strength { get; set; }

		private string type;
		public string Type { 
			get { return this.type; } 
			set {
				this.type = value;
				this.ID = "GUARD_EFFECT_" + type;
			}
		}
	}

	public class VulnerableEffect: Effect {
		new public string ID = "VULNERABLE_EFFECT";

		public VulnerableEffect(float multiplier, string toDamageType) {
			this.Type = toDamageType;
			this.Multiplier = multiplier;
			this.Duration = Effect.INFINITE_DURATION;
		}

		override public Effect Clone {
			get {
				var res = new VulnerableEffect (this.Multiplier, this.Type);
				res.lastInitiative = this.lastInitiative;
				res.Duration = this.Duration;
				return res;
			}
		}

		public float Multiplier { get; set; }

		override public int ProcessDamage(int howMuch, string type) {
			if (type == this.Type || this.Type == Simulator.DAMAGE_TYPE_UNTYPED) {
				return (int)(howMuch * this.Multiplier);
			} else {
				return howMuch;
			}
		}

		public string Type { get; set; }
	}
}

