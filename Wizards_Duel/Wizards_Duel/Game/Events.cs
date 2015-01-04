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

namespace WizardsDuel.Game
{
	public class Event
	{
		public Event (long deltaTime = 0)
		{
			this.DeltaTime = deltaTime;
			this.StartTime = 0;
		}

		/// <summary>
		/// Executes this event, return true if the event has ended, false otherwise
		/// </summary>
		virtual public bool Run(Simulator sim) {
			return true;
		}

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
		public ActorEvent (string oid, long deltaTime = 0): base (deltaTime)
		{
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

	public class AiEvent: ActorEvent {
		public AiEvent (string oid, long deltaTime = 0): base (oid, deltaTime) {}

		override public bool Run(Simulator sim) {
			//Logger.Debug ("AiEvent", "Run", "Running time " + this.StartTime.ToString());
			this.StartTime = 0;
			this.DeltaTime = 15; // XXX this should come from the speed of the player actor!
			var rnd = new Random ();
			sim.CanShift(this.Actor, rnd.Next(-1,2), rnd.Next(-1,2), true);
			sim.events.AppendEvent (this);
			return true;
		}
	}

	public class AttackEvent: TargetedEvent {
		public AttackEvent (Entity attacker, Entity target, long deltaTime = 0): 
		base (attacker, target, deltaTime) {}

		override public bool Run(Simulator sim) {
			Logger.Debug ("AttackEvent", "Run", this.Actor + " attacks " + this.Target);
			var rnd = new Random ();
			this.Actor.OutObject.SetAnimation ("ATTACK");
			return true;
		}
	}

	public class ShiftEvent: ActorEvent {
		public ShiftEvent(string oid, int dx, int dy, long deltaTime = 0):
		base (oid, deltaTime) {
			this.DX = dx;
			this.DY = dy;
		}

		public int DX {
			get;
			set;
		}

		public int DY {
			get;
			set;
		}

		override public bool Run(Simulator sim) {
			sim.CanShift(this.Actor, this.DX, this.DY, true);
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

	public class UserEvent : Event {
		EventDispatcher eventDispatcher;
		public UserEvent(EventDispatcher ed, long deltaTime = 0): base (deltaTime) {
			this.eventDispatcher = ed;
		}

		override public bool Run(Simulator sim) {
			//Logger.Debug ("UserEvent", "Run", "Running time " + this.StartTime.ToString());
			var acted = this.eventDispatcher.RunUserEvent ();
			this.StartTime = 0;
			this.DeltaTime = (acted == false) ? 0 : 10; // XXX this should come from the speed of the player actor!
			this.eventDispatcher.AppendEvent (this);
			return true;
		}
	}
}

