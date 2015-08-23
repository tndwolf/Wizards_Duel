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
		bool HasEnded { get; set; }

		/// <summary>
		/// Gets or sets the current initiative.
		/// </summary>
		/// <value>The initiative.</value>
		int Initiative { get; set; }

		bool IsAnimating { get; }

		/// <summary>
		/// Runs the event. At the end the initiative count must always be updated
		/// </summary>
		/// <param name="sim">Sim.</param>
		/// <param name="ed">Ed.</param>
		void Run (Simulator sim, EventManager ed);

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

		public bool AcceptEvent { get; set; }

		public void AppendEvent(Event evt) {
			if (this.eventQueueLocked == false) {
				this.eventQueue.Add (evt);
			}
			// else throw?
		}

		public void ClearUserEvent() {
			//Logger.Debug ("EventManager", "ClearUserEvent", "Clearing user events");
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
				Logger.Debug ("EventManager", "Dispatch", "Object " + actor.ToString() + " at initiative " + this.Initiative.ToString());
				/*var e = actor as Entity;
				if (e!= null && e.ID != Simulator.PLAYER_ID && actor.HasEnded == false) {
					//Logger.Debug ("EventManager", "Dispatch", "Object " + e.ID + " is waiting");
					return;
				} else {
					//Logger.Debug ("EventManager", "Dispatch", "Object to process " + actor.GetHashCode() + " at initiative " + this.Initiative.ToString());
					this.Initiative = actor.Initiative;
					actor.Run (this.simulator, this);
					if (actor.HasEnded) {
						this.actorQueue.Sort ();
						//this.Replan (actor);
					}
				}*/
				if (!actor.IsAnimating) {
					this.Initiative = actor.Initiative;
					actor.Run (this.simulator, this);
					if (this.WaitingForUser) {
						return;
					} else {
						actor.HasEnded = true;
						this.actorQueue.Sort ();
					}
				}
				//if (actor.HasEnded) {
					//this.actorQueue.Sort ();
					//Logger.Debug ("EventManager", "Dispatch", "Current Queue " + actorQueue.Count.ToString());
				//}
			}
		}

		public void DispatchAll() {
			// first run events
			this.eventQueueLocked = true;
			foreach (var evt in this.eventQueue) {
				Logger.Debug ("EventManager", "Dispatch", "Processing event " + evt.GetType().ToString());
				if (evt.Run () == false) {
					this.eventQueueLocked = false;
				} else {
					evt.DeleteMe = true;
				}
			}
			this.eventQueue.RemoveAll (x => x.DeleteMe == true);
			this.eventQueueLocked = false;
			// then run actors
			foreach (var actor in this.actorQueue) {
				if (actor as World == null) {
					Logger.Debug ("EventManager", "DispatchAll", "Object " + actor.ToString () + " at initiative " + this.Initiative.ToString ());
					this.Initiative = actor.Initiative;
					actor.Run (this.simulator, this);
				}
			}
			this.actorQueue.Sort ();
		}

		/// <summary>
		/// Gets or sets the absolute initiative count.
		/// </summary>
		/// <value>The initiative.</value>
		public int Initiative {
			get;
			internal set;
		}

		public void QueueObject(EventObject obj, int initiative) {
			//Logger.Debug ("EventManager", "QueueObject", "Adding object " + obj.GetHashCode());
			obj.Initiative = initiative;
			obj.HasEnded = false;
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

		public bool RunUserEvent() {
			if (this.userEvent != null && simulator.GetPlayer().OutObject.IsInIdle) {
			//if (this.userEvent != null) {
				//Logger.Debug ("EventManager", "RunUserEvent", "Running user event " + this.userEvent.ToString());
				Logger.Debug ("EventManager", "RunUserEvent", "RUNNING EVENT AT INIT " + this.Initiative.ToString ());
				var player = simulator.GetPlayer();
				this.userEvent.Run ();
				this.userEvent = null;
				simulator.world.CalculateFoV (player.X, player.Y, 6);
				return true;
			} else {
				this.ClearUserEvent ();
				return false;
			}
		}

		public void SetUserEvent(Event userEvent) {
			if (AcceptEvent) {
				Logger.Info ("EventManager", "SetUserEvent", "Got new user event " + userEvent.ToString ());
				this.userEvent = userEvent;
			}
		}

		public void WaitAndRun(int millis, Event evt) {
			this.AppendEvent (new WaitEvent (millis));
			this.AppendEvent (evt);
		}

		public void WaitFor(int millis) {
			//this.waitUntil = IoManager.Time + millis;
			this.AppendEvent (new WaitEvent (millis));
		}

		public bool WaitingForUser { get; set; }
	}
}
