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
		virtual public bool Run() {
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

		override public bool Run() {
			var sim = Simulator.Instance;
			//Logger.Debug ("AiEvent", "Run", "Running time " + this.StartTime.ToString());
			this.StartTime = 0;
			this.DeltaTime = 15; // XXX this should come from the speed of the player actor!
			var rnd = new Random ();

			var player = sim.GetObject (Simulator.PLAYER_ID);
			var actore = sim.GetObject (this.Actor);
			var dx = Math.Sign(player.X - actore.X);
			var dy = Math.Sign(player.Y - actore.Y);

			//sim.CanShift(this.Actor, rnd.Next(-1,2), rnd.Next(-1,2), true);
			sim.CanShift(this.Actor, dx, dy, true);
			sim.events.AppendEvent (this);
			return true;
		}
	}

	public class AreaAiEvent: Event {
		private long areaDeltaTime;

		public AreaAiEvent (long deltaTime = 0): base (deltaTime) {
			this.areaDeltaTime = deltaTime;
		}

		override public bool Run() {
			var sim = Simulator.Instance;
			Logger.Info ("AreaAiEvent", "Run", "Running area events");

			// XXX Test only, run the enemies
			//if (acted == true)
			{
				//var sim = Simulator.Instance;
				var player = sim.GetObject (Simulator.PLAYER_ID);
				foreach (var enemy in sim.ListEnemies()) {
					if (enemy.Value.Static == false) {
						var dx = Math.Sign (player.X - enemy.Value.X);
						var dy = Math.Sign (player.Y - enemy.Value.Y);
						sim.CanShift (enemy.Key, dx, dy, true);
					}
				}
			}
			// XXX end of test

			//5-5 7-7
			/*var MAX_ENTITIES = 10;
			var spawn = sim.Random (100);
			if (spawn > 90 && sim.world.entities.Count < MAX_ENTITIES) {
				var p = sim.GetObject (Simulator.PLAYER_ID);
				var minX = p.X - 7;
				var minY = p.Y - 5;
				var maxX = p.X + 8;
				var maxY = p.Y + 6;
				var possibleCells = new List<Vector2i> ();
				for(int y = minY; y < maxY; y++) {
					for(int x = minX; x < maxX; x++) {
						if (
							!(y > minY && y < maxY - 1 && x > minX && x < maxX - 1) &&
							sim.world.IsWalkable (x, y) &&
							sim.GetObjectAt(x, y) == null
						) {
							possibleCells.Add (new Vector2i (x, y));
						}
					}
				}
				if (possibleCells.Count > 0) {
					var position = possibleCells [sim.Random (possibleCells.Count)];
					sim.CreateObject(sim.createdEntityCount.ToString (), "bp_firefly", position.X, position.Y);
					Logger.Info ("AreaAiEvent", "Run", "Created object at " + position.ToString());
				}
			}*/

			this.DeltaTime = this.areaDeltaTime;
			sim.events.AppendEvent (this);
			return true;
		}
	}

	public class AttackEvent: TargetedEvent {
		public AttackEvent (Entity attacker, Entity target, long deltaTime = 0): 
		base (attacker, target, deltaTime) {}

		override public bool Run() {
			Logger.Debug ("AttackEvent", "Run", this.Actor + " attacks " + this.Target);
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

	public class UserEvent : Event {
		EventDispatcher eventDispatcher;
		public UserEvent(EventDispatcher ed, long deltaTime = 0): base (deltaTime) {
			this.eventDispatcher = ed;
		}

		override public bool Run() {
			//Logger.Debug ("UserEvent", "Run", "Running time " + this.StartTime.ToString());
			var acted = this.eventDispatcher.RunUserEvent ();
			this.StartTime = 0;
			this.DeltaTime = (acted == false) ? 0 : 10; // XXX this should come from the speed of the player actor!
			this.eventDispatcher.AppendEvent (this);

			/*if (acted == true) {
				var turn = ++this.eventDispatcher.turnCount;
				if (turn % 10 == 9) {
					var sim = Simulator.Instance;
					var pc = sim.GetObject (Simulator.PLAYER_ID);
					var buff = sim.GetObject(sim.GetObjectAt (pc.X, pc.Y));
					if (buff == null || buff.TemplateID != "bp_fire_lava") {
						var id = "lava_" + turn.ToString ();
						sim.CreateObject (id, "bp_fire_lava", pc.X, pc.Y);
						var lava = sim.GetObject (id);
						lava.OutObject.SetAnimation ("CREATE");
						//sim.CreateParticleAt ("p_lava", lava.X, lava.Y);
					}
				}
			}//*/

			/*if (acted == true) {
				//5-5 7-7
				var MAX_ENTITIES = 10;
				var sim = Simulator.Instance;
					var spawn = sim.Random (100);
					if (spawn > 10 && sim.world.entities.Count < MAX_ENTITIES) {
						var p = sim.GetObject (Simulator.PLAYER_ID);
						var minX = p.X - 7;
						var minY = p.Y - 5;
						var maxX = p.X + 8;
						var maxY = p.Y + 6;
						var possibleCells = new List<Vector2i> ();
						for (int y = minY; y < maxY; y++) {
							for (int x = minX; x < maxX; x++) {
								if (
									!(y > minY && y < maxY - 1 && x > minX && x < maxX - 1) &&
									sim.world.IsWalkable (x, y) &&
									sim.GetObjectAt (x, y) == null) {
									possibleCells.Add (new Vector2i (x, y));
								}
							}
						}
						if (possibleCells.Count > 0) {
							var position = possibleCells [sim.Random (possibleCells.Count)];
							sim.CreateObject (sim.createdEntityCount.ToString (), "bp_firefly", position.X, position.Y);
							Logger.Info ("AreaAiEvent", "Run", "Created object at " + position.ToString ());
						}
					}
			}//*/

			return true;
		}
	}
}

