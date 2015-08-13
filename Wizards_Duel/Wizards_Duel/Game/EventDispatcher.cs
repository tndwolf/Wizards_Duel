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
		List<EventObject> actorQueue = new List<EventObject> ();
		List<Event> eventQueue = new List<Event> ();
		bool eventQueueLocked = false;
		Simulator simulator;
		public Event userEvent;
		public long userWaitUntil = 0;

		public EventManager (Simulator sim) {
			this.simulator = sim;
			this.userEvent = null;
		}

		public void AppendEvent(Event evt) {
			if (this.eventQueueLocked == false) {
				this.eventQueue.Add (evt);
			}
			// else throw?
		}

		public void ClearUserEvent() {
			Logger.Debug ("EventManager", "ClearUserEvent", "Clearing user events");
			this.userEvent = null;
		}

		public void Dispatch() {
			// first run events
			this.eventQueueLocked = true;
			foreach (var evt in this.eventQueue) {
				Logger.Debug ("EventManager", "Dispatch", "Processing event " + evt.GetType().ToString());
				if (evt.Run () == false) {
					this.eventQueueLocked = false;
					return;
				} else {
					evt.DeleteMe = true;
				}
			}
			this.eventQueue.RemoveAll (x => x.DeleteMe == true);
			this.eventQueueLocked = false;
			// then run actors
			if (this.actorQueue.Count > 0) {
				var actor = this.actorQueue [0];
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
				if (actor.IsWaiting /*|| actor.HasEnded == false*/) {
					return;
				//} else if (actor.HasEnded == false) {
				//	actor.Run (this.simulator, this);
				} else {
					//Logger.Debug ("EventManager", "Dispatch", "Object to process " + actor.GetHashCode() + " at initiative " + this.Initiative.ToString());
					this.Initiative = actor.Initiative;
					actor.Run (this.simulator, this);
					this.Replan (actor);
				}
			}
		}

		public int Initiative {
			get;
			private set;
		}

		public void QueueObject(EventObject obj, int initiative) {
			//Logger.Debug ("EventManager", "QueueObject", "Adding object " + obj.GetHashCode());
			obj.Initiative = initiative;
			obj.HasEnded = false;
			obj.HasStarted = false;
			//obj.IsWaiting = false;
			if (this.actorQueue.Count < 0) {
				this.actorQueue.Add (obj);
			} else {
				for (int i = 0; i < actorQueue.Count; i++) {
					if (this.actorQueue [i].Initiative > initiative) {
						this.actorQueue.Insert (i, obj);
						return;
					}
				}
				this.actorQueue.Add (obj);
			}
		}

		public void Replan(EventObject obj) {
			var newInitiative = obj.UpdateInitiative ();
			if (newInitiative < 0) {
				this.actorQueue.Remove (obj);
				//Logger.Debug ("EventManager", "Replan", "Removing object " + obj.GetHashCode ());
			} else {
				obj.HasStarted = false;
				obj.HasEnded = false;
				this.actorQueue.Sort ();
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
			//this.waitUntil = IoManager.Time + millis;
			this.AppendEvent (new WaitEvent (millis));
			//this.userEvent = null;
		}

		public void WaitAndRun(int millis, Event evt) {
			this.AppendEvent (new WaitEvent (millis));
			this.AppendEvent (evt);
			//this.userEvent = null;
		}
	}
}
