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
using WizardsDuel.Utils;
using WizardsDuel.Io;
using System.Collections.Generic;
using SFML.Window;

namespace WizardsDuel.Game
{
	public class Event {
		public Event (long deltaTime = 0) {
			this.DeltaTime = deltaTime;
			this.StartTime = 0;
		}

		/// <summary>
		/// Executes this event, return true if the event has ended, false otherwise
		/// </summary>
		virtual public bool Run() {
			return true;
		}

		public bool DeleteMe { get; set; }

		public long DeltaTime {
			get;
			set;
		}

		public long StartTime {
			get;
			set;
		}
	}

	public class ActorEvent: Event {
		public ActorEvent (string oid, long deltaTime = 0): base (deltaTime) {
			this.Actor = oid;
		}

		/// <summary>
		/// Gets or Sets the oid of the event's Actor
		/// </summary>
		virtual public string Actor {
			get;
			set;
		}
	}

	public class AttackEvent: TargetedEvent {
		public AttackEvent (Entity attacker, Entity target, long deltaTime = 0): 
		base (attacker, target, deltaTime) {}

		override public bool Run() {
			Logger.Debug ("AttackEvent", "Run", this.Actor.ID + " attacks " + this.Target.ID);
			this.Actor.OutObject.SetAnimation ("ATTACK");
			if (this.Actor.X != this.Target.X) {
				this.Actor.OutObject.Facing = (this.Actor.X < this.Target.X) ? Facing.RIGHT : Facing.LEFT;
				this.Target.OutObject.Facing = (this.Actor.X < this.Target.X) ? Facing.LEFT : Facing.RIGHT;
			}
			var newHealth = this.Target.GetVar (Simulator.HEALTH_VARIABLE, 1) - 1;
			this.Target.SetVar (Simulator.HEALTH_VARIABLE, newHealth);
			if (newHealth > 0) {
				if (this.Target.GetVar ("armor") < 1) {
					Simulator.Instance.Bleed (Target);
				}
			} else {
				Simulator.Instance.Kill (Target);
			}
			return true;
		}
	}

	public class KillEvent: ActorEvent {
		public KillEvent (string oid, long deltaTime = 0): base (oid, deltaTime) {}

		override public bool Run() {
			Simulator.Instance.Kill (Simulator.Instance.GetObject(this.Actor));
			return true;
		}
	}

	public class DestroyEvent: ActorEvent {
		public DestroyEvent (string oid, long deltaTime = 0): base (oid, deltaTime) {}

		override public bool Run() {
			Logger.Debug ("DestroyEvent", "Run", "Trying to kill " + this.Actor);
			Simulator.Instance.DestroyObject (this.Actor);
			return true;
		}
	}

	public class CastEvent: ActorEvent {
		public CastEvent(string oid, int targetX, int targetY, long deltaTime = 0):
		base (oid, deltaTime) {
			this.TargetX = targetX;
			this.TargetY = targetY;
			//Logger.Debug ("ShiftEvent", "ShiftEvent", "Added to " + oid);
		}

		public int TargetX {
			get;
			set;
		}

		public int TargetY {
			get;
			set;
		}

		override public bool Run() {
			//Logger.Debug ("ShiftEvent", "Run", "Shifting " + this.Actor);
			Simulator.Instance.Cast(this.Actor, this.TargetX, this.TargetY);
			return true;
		}
	}

	public class MethodEvent: Event {
		private Action function;

		public MethodEvent (Action function, long deltaTime = 0): base(deltaTime) {
			this.function = function;
		}

		/// <summary>
		/// Executes this event, return true if the event has ended, false otherwise
		/// </summary>
		override public bool Run() {
			this.function ();
			return true;
		}
	}

	public class ShiftEvent: ActorEvent {
		public ShiftEvent(string oid, int dx, int dy, long deltaTime = 0):
		base (oid, deltaTime) {
			this.DX = dx;
			this.DY = dy;
			//Logger.Debug ("ShiftEvent", "ShiftEvent", "Added to " + oid);
		}

		public int DX {
			get;
			set;
		}

		public int DY {
			get;
			set;
		}

		override public bool Run() {
			//Logger.Debug ("ShiftEvent", "Run", "Shifting " + this.Actor);
			if (Simulator.Instance.CanShift (this.Actor, this.DX, this.DY, true) == false) {
				if (Simulator.Instance.CanShift (this.Actor, this.DX, 0, true) == false) {
					Simulator.Instance.CanShift (this.Actor, 0, this.DY, true);
				}
			}
			return true;
		}
	}

	public class TargetedEvent: Event {
		public TargetedEvent (Entity actor, Entity target, long deltaTime = 0): base (deltaTime) {
			this.Actor = actor;
			this.Target = target;
		}

		/// <summary>
		/// Gets or Sets the oid of the event's Actor
		/// </summary>
		virtual public Entity Actor {
			get;
			set;
		}

		/// <summary>
		/// Gets or Sets the oid of the event's Target
		/// </summary>
		virtual public Entity Target {
			get;
			set;
		}
	}

	public class WaitEvent: Event {
		public WaitEvent (long deltaTime = 0) {
			this.EndTime = IoManager.Time + deltaTime;
		}

		/// <summary>
		/// Executes this event, return true if the event has ended, false otherwise
		/// </summary>
		override public bool Run() {
			Simulator.Instance.ClearUserEvent ();
			return IoManager.Time > this.EndTime;
		}

		public long EndTime {
			get;
			set;
		}
	}
}
