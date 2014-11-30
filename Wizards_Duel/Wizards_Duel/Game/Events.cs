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

	public class ActorEvent : Event
	{
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

	public class ShiftEvent : ActorEvent {
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

	public class UserAiEvent : Event {
		EventDispatcher eventDispatcher;
		public UserAiEvent(EventDispatcher ed, long deltaTime = 0): base (deltaTime) {
			this.eventDispatcher = ed;
		}

		override public bool Run(Simulator sim) {
			this.eventDispatcher.RunUserEvent ();
			this.StartTime = 0;
			this.DeltaTime = 10; // XXX this should come from the speed of the player actor!
			this.eventDispatcher.AppendEvent (this);
			return true;
		}
	}
}

