using System;
using WizardsDuel.Game;
using System.Collections.Generic;
using SFML.Window;
using WizardsDuel.Utils;

namespace WizardsDuel.Game
{
	public class AreaAI: ArtificialIntelligence {
		new public World Parent { get; set; }

		override public void onRound () {
			var MAX_ENTITIES = 10;
			var sim = Simulator.Instance;
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
					var r = sim.Random (100);
					if (r < 60) {
						sim.CreateObject (sim.createdEntityCount.ToString (), "bp_firefly", position.X, position.Y);
					} else if (r < 80) {
						sim.CreateObject (sim.createdEntityCount.ToString (), "bp_fire_thug1", position.X, position.Y);
					} else if (r < 90) {
						sim.CreateObject (sim.createdEntityCount.ToString (), "bp_fire_salamander1", position.X, position.Y);
					} else {
						sim.CreateObject (sim.createdEntityCount.ToString (), "bp_fire_bronze_thug1", position.X, position.Y);
					}
						Logger.Info ("AreaAI", "onRound", "Created object at " + position.ToString());
				}
			}
		}
	}

	public class ArtificialIntelligence {
		public const string LAVA = "LAVA";
		public const string LAVA_EMITTER = "LAVA_EMITTER";
		public const string MELEE = "MELEE";
		public const string RANGED = "RANGED";

		public Entity Parent { get; set; }

		virtual public void onCreate () {
			return;
		}

		virtual public void onDamage (int howMuch, string[] type) {
			return;
		}

		virtual public void onDestroy () {
			return;
		}

		virtual public void onKill () {
			return;
		}

		virtual public void onRound () {
			return;
		}
	}

	public class MeleeAI: ArtificialIntelligence {
		override public void onRound () {
			var player = Simulator.Instance.GetPlayer();
			var dx = Math.Sign(player.X - this.Parent.X);
			var dy = Math.Sign(player.Y - this.Parent.Y);
			var ex = this.Parent.X + dx;
			var ey = this.Parent.Y + dy;
			var world = Simulator.Instance.world;
			if (world.IsWalkable (ex, ey)) {
				Simulator.Instance.Shift (this.Parent.ID, dx, dy);
			} else if (dx == 0) {
				ex -= 1;
				if (world.IsWalkable (ex, ey)) {
					Simulator.Instance.Shift (this.Parent.ID, dx-1, dy);
					return;
				} else if (world.IsWalkable (ex + 2, ey)) {
					Simulator.Instance.Shift (this.Parent.ID, dx+1, dy);
					return;
				}
			} else if (dy == 0) {
				ey -= 1;
				if (world.IsWalkable (ex, ey)) {
					Simulator.Instance.Shift (this.Parent.ID, dx, dy-1);
					return;
				} else if (world.IsWalkable (ex, ey + 2)) {
					Simulator.Instance.Shift (this.Parent.ID, dx, dy+1);
					return;
				}
			} else {
				dx = Simulator.Instance.Random(3)-1;
				dy = Simulator.Instance.Random(3)-1;
				Simulator.Instance.CanShift(this.Parent.ID, dx, dy, true);
			}
		}
	}

	public class LavaAI: ArtificialIntelligence {
		public static int HARDEN_TIME = Simulator.ROUND_LENGTH * 3;
		public static int MAX_GENERATIONS = 3;
		public static int MAX_SPAWN_COUNT = 3;

		private bool firstRound = true;
		private bool hasSpawned = false;
		private int initiative = 0;
		private int hardenInitiative = 0;
		private int oldInitiative = 0;
		internal int status = 0; // 0 lava, 1 basalt

		/// <summary>
		/// Lava cells has a "generation", zero generation (the default) do not spawn 
		/// other lava cells, first generation to MAX_GENERATIONS spawn lava cells if active
		/// </summary>
		/// <value>The generation.</value>
		public int Generation {
			get;
			set;
		}

		public int Initiative {
			set {
				this.initiative = value;
				this.oldInitiative = value;
				this.hardenInitiative = value + HARDEN_TIME;
			}
		}

		override public void onCreate() {
			var sim = Simulator.Instance;
			var objects = sim.world.GetObjectsAt (Parent.X, Parent.Y);
			//Logger.Debug ("LavaAI", "onCreate", "Deleting existing items");
			foreach (var o in objects) {
				if (o.ID == this.Parent.ID) {
					// skip myself
					continue;
				}
				if (o.TemplateID == Parent.TemplateID) {
					// Found lava already present, destroy old patch and update
					//Logger.Debug ("LavaAI", "onCreate", "Deleting " + o.TemplateID);
					this.Parent.OutObject.SetAnimation ("IDLE");
					sim.DestroyObject(o.ID);
				} else {
					// other objects, hurt them on creation!
					Logger.Debug ("LavaAI", "onCreate", "Deleting " + o.TemplateID);
					sim.DestroyObject(o.ID);
				}
			}
		}

		override public void onRound () {
			this.initiative += Parent.Initiative - this.oldInitiative;
			this.oldInitiative = Parent.Initiative;
			//Logger.Info ("LavaEmitterAI", "onRound", "Current initiative " + this.startInitiative.ToString() + " parent " + this.Parent.GetHashCode().ToString());
			if (this.status == 0 && this.Generation > 0 && this.Generation < MAX_GENERATIONS && this.firstRound == false && this.hasSpawned == false) {
				Spawn ();
			}
			if (this.initiative > this.hardenInitiative && this.status == 0) {
				this.status = 1;
				Parent.OutObject.ZIndex -= 1;
				Parent.OutObject.IdleAnimation = "BASALT";
				Parent.OutObject.SetAnimation ("SOLIDIFY");
				Parent.Static = true;
				Parent.AI = new ArtificialIntelligence ();
			}
			this.firstRound = false;
		}

		private void Spawn() {
			this.hasSpawned = true;
			var sim = Simulator.Instance;
			for (int i = 0; i < MAX_SPAWN_COUNT; i++) {
				var x = Parent.X + sim.Random (3) - 1;
				var y = Parent.Y + sim.Random (3) - 1;
				//var alreadyRefreshed = false;
				var objects = sim.world.GetObjectsAt (x, y);
				var containsLava = objects.Exists(o => o.TemplateID == this.Parent.TemplateID && o.OutObject.IdleAnimation == "IDLE");
				if (sim.world.IsWalkable (x, y) && containsLava == false) {
					var lava = sim.GetObject (sim.CreateObject (Parent.TemplateID, x, y));
					if (lava != null) {
						var ai = lava.AI as LavaAI;
						ai.Initiative = this.oldInitiative + Simulator.ROUND_LENGTH;
						ai.Generation = this.Generation + 1;
						//lava.OutObject.Color = new SFML.Graphics.Color (255, (byte)(255 - ai.Generation * 50), 255);
						sim.SetAnimation (lava, "CREATE");
					}
				}
			}
		}
	}

	public class LavaEmitterAI: ArtificialIntelligence {
		public static int EMIT_START = Simulator.ROUND_LENGTH * 6;
		public static int EMIT_END = Simulator.ROUND_LENGTH * 9;

		private int initiative = 0;
		private int emitInitiative = EMIT_START;
		private int oldInitiative = 0;
		private int stopInitiative = EMIT_END;
		private int status = 0; // 0 idle, 1 active

		public int Initiative {
			set {
				this.initiative = value;
				this.oldInitiative = value;
				this.emitInitiative = value + EMIT_START;
				this.stopInitiative = value + EMIT_END;
			}
		}

		override public void onRound () {
			this.initiative += Parent.Initiative - this.oldInitiative;
			this.oldInitiative = Parent.Initiative;
			if (this.initiative > stopInitiative && this.status == 1) {
				//Logger.Debug ("LavaEmitterAI", "onRound", "Closing");
				this.status = 0;
				Parent.OutObject.IdleAnimation = "IDLE";
				Parent.OutObject.SetAnimation ("CLOSE");
				this.Initiative = this.oldInitiative;
			} else if (this.initiative > emitInitiative && this.status == 0) {
				//Logger.Info ("LavaEmitterAI", "onRound", "Opening");
				var sim = Simulator.Instance;
				this.status = 1;
				Parent.OutObject.SetAnimation ("OPEN");
				Parent.OutObject.IdleAnimation = "ACTIVE";
				var lava = sim.GetObject (sim.CreateObject ("bp_fire_lavax2", Parent.X, Parent.Y + 1));
				var ai = lava.AI as LavaAI;//new LavaAI ();
				ai.Initiative = this.oldInitiative + Simulator.ROUND_LENGTH;
				ai.Generation = 1;
				sim.SetAnimation (lava, "CREATE");
			}
		}
	}

	public class UserAI: ArtificialIntelligence {
		override public void onRound () {
			Simulator.Instance.events.WaitingForUser = !Simulator.Instance.events.RunUserEvent ();
		}
	}

}

