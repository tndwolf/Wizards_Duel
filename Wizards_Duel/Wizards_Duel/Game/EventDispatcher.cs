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
	public interface EventObject: IComparable {
		bool HasEnded {
			get;
			set;
		}

		bool HasStarted {
			get;
			set;
		}

		int Initiative {
			get;
			set;
		}

		bool IsWaiting {
			get;
		}

		/// <summary>
		/// Returns true if the action has finished, false otherwise.
		/// </summary>
		/// <param name="sim">Sim.</param>
		/// <param name="ed">Ed.</param>
		void Run (Simulator sim, EventManager ed);

		/// <summary>
		/// Returns the next initiative count after ending the current action.
		/// If less than zero the object can be deleted
		/// </summary>
		int UpdateInitiative ();
	}

	public class EventManager {
		List<EventObject> queue = new List<EventObject> ();
		Simulator simulator;
		public Event userEvent;
		long waitUntil = 0;

		public EventManager (Simulator sim) {
			this.simulator = sim;
			this.userEvent = null;
		}

		public void Dispatch() {
			if (this.waitUntil > IoManager.Time) {
				// wait a certain real-time amount
				return;
			}
			//Logger.Debug ("EventManager", "Dispatch", "Run dispatcher");
			if (this.queue.Count > 0) {
				var actor = this.queue [0];
				Logger.Debug ("EventManager", "Dispatch", "Object to process " + actor.GetHashCode());
				/*if (actor.HasStarted == false) {
					Logger.Debug ("EventManager", "Dispatch", "Running object " + actor.GetHashCode ());
					actor.Run (this.simulator, this);
					actor.HasStarted = true;
				} else if (actor.IsWaiting == true) {
					Logger.Debug ("EventManager", "Dispatch", "Waiting for " + actor.GetHashCode());
					return;
				} else if (actor.HasEnded) {
					Logger.Debug ("EventManager", "Dispatch", "Replanning object " + actor.GetHashCode());
					this.Replan (actor);
					//this.Dispatch ();
					//return;
				}*/
				if (actor.IsWaiting) {
					return;
				//} else if (actor.HasEnded == false) {
				//	actor.Run (this.simulator, this);
				} else {
					actor.Run (this.simulator, this);
					this.Replan (actor);
				}
			}
		}

		public void QueueObject(EventObject obj, int initiative) {
			Logger.Debug ("EventManager", "QueueObject", "Adding object " + obj.GetHashCode());
			obj.Initiative = initiative;
			obj.HasEnded = false;
			obj.HasStarted = false;
			//obj.IsWaiting = false;
			if (this.queue.Count < 0) {
				this.queue.Add (obj);
			} else {
				for (int i = 0; i < queue.Count; i++) {
					if (this.queue [i].Initiative > initiative) {
						this.queue.Insert (i, obj);
						return;
					}
				}
				this.queue.Add (obj);
			}
		}

		public void Replan(EventObject obj) {
			var newInitiative = obj.UpdateInitiative ();
			if (newInitiative < 0) {
				this.queue.Remove (obj);
				Logger.Debug ("EventManager", "Replan", "Removing object " + obj.GetHashCode ());
			} else {
				obj.HasStarted = false;
				obj.HasEnded = false;
				this.queue.Sort ();
			}
		}

		public bool RunUserEvent() {
			if (this.userEvent != null) {
				Logger.Debug ("EventManager", "RunUserEvent", "Running user event " + this.userEvent.ToString());
				this.userEvent.Run ();
				this.userEvent = null;
				return true;
			} else {
				return false;
			}
		}

		public void SetUserEvent(Event userEvent) {
			Logger.Debug ("EventManager", "SetUserEvent", "Got new user event " + userEvent.ToString());
			this.userEvent = userEvent;
		}

		public bool UserEventInQueue {
			get { return this.userEvent != null; }
		}

		public void WaitFor(int millis) {
			this.waitUntil = IoManager.Time + millis;
		}
	}

	public class EventDispatcher
	{
		Simulator simulator;

		bool canAddEvents = true;
		List<Event> events = new List<Event>(); // TODO Maybe better as a LinkedList?
		List<Event> eventsToBeAdded = new List<Event>();
		Event nextUserEvent = null;
		long time = 0; // this is the absolute simulation time
		public long turnCount = 0; // number of players turns
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
			if (this.waitUntil > IoManager.Time) {
				// wait a certain real-time amount
				return;
			}
			this.UpdateEventQueue ();
			var newEvents = new List<Event>();
			this.canAddEvents = false;
			foreach (var evt in this.events) {
				if (evt == this.events [0]) {
					this.time = evt.StartTime;
					if (evt.Run () == false) {
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
				if (this.nextUserEvent.Run () == false) {
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
			this.waitUntil = IoManager.Time + millis;
		}
	}
}

