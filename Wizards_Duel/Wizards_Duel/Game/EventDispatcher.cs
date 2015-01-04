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
using WizardsDuel.Utils;
using WizardsDuel.Io;

namespace WizardsDuel.Game
{
	public class EventDispatcher
	{
		Simulator simulator;

		bool canAddEvents = true;
		List<Event> events = new List<Event>(); // TODO Maybe better as a LinkedList?
		List<Event> eventsToBeAdded = new List<Event>();
		Event nextUserEvent = null;
		long time = 0; // this is the absolute simulation time
		long waitUntil = 0;


		public EventDispatcher (Simulator sim) {
			this.simulator = sim;
		}

		public void AppendEvent(Event evt) {
			if (this.canAddEvents == false) {
				this.eventsToBeAdded.Add (evt);
				return;
			}
			evt.StartTime = evt.DeltaTime + this.time;
			var newEvents = new List<Event>();
			bool added = false;
			foreach (var refEvt in this.events) {
				// the '<' guarantees that the event is queued after other
				// events happening at the same timeref
				if (evt.StartTime < refEvt.StartTime) {
					newEvents.Add(evt);
					added = true;
				}
				newEvents.Add (refEvt);
			}
			if (added == false) {
				newEvents.Add (evt);
			}
			this.events = newEvents;
		}

		public void Dispatch() {
			if (this.waitUntil > IO.GetTime ()) {
				// wait a certain real-time amount
				return;
			}
			this.UpdateEventQueue ();
			var newEvents = new List<Event>();
			this.canAddEvents = false;
			foreach (var evt in this.events) {
				if (evt == this.events [0]) {
					this.time = evt.StartTime;
					if (evt.Run (this.simulator) == false) {
						newEvents.Add (evt);
					}
				} else {
					newEvents.Add (evt);
				}
			}
			this.events = newEvents;
			this.canAddEvents = true;
		}

		public bool RunUserEvent() {
			if (this.nextUserEvent == null) {
				return false;
			} else {
				if (this.nextUserEvent.Run (this.simulator) == false) {
					this.AppendEvent (this.nextUserEvent);
				}
				this.nextUserEvent = null;
				return true;
			}
		}

		public void SetUserEvent(Event evt) {
			this.nextUserEvent = evt;
		}

		public void UpdateEventQueue() {
			if (this.eventsToBeAdded.Count > 0) {
				this.canAddEvents = true;
				foreach (var evt in this.eventsToBeAdded) {
					this.AppendEvent (evt);
				}
				this.eventsToBeAdded.Clear ();
			}
		}

		public void WaitFor(int millis) {
			this.waitUntil = IO.GetTime () + millis;
		}
	}
}

