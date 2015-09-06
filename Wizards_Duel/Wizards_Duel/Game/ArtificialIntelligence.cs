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
using WizardsDuel.Game;
using System.Collections.Generic;
using SFML.Window;
using WizardsDuel.Utils;
using WizardsDuel.Io;

namespace WizardsDuel.Game
{
	public class AreaAI: ArtificialIntelligence {
		new public World Parent { get; set; }

		internal int[] progression = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 3, 0, 0, 0, 0, 5, 0, 0, 0, 0 };
		private int progressionIndex = 0;
		public int CurrentThreatLevel { get; set; }
		public int ThreatLevel { get; set; }
		public int MaxThreatLevel { get; set; }

		override public void OnRound () {
			this.ThreatLevel += this.progression[this.progressionIndex];
			//this.ThreatLevel = Math.Max(this.ThreatLevel, this.MaxThreatLevel);
			this.progressionIndex++;
			this.progressionIndex %= this.progression.Length;
			var deltaThreat = this.ThreatLevel - this.CurrentThreatLevel;
			if (deltaThreat > 0) {
				Logger.Info ("AreaAI", "OnRound", "Threat " + deltaThreat.ToString() + " spawning");
				var sim = Simulator.Instance;
				// let's find a group of enemies at this threat level
				var encounter = new List<EnemyBlueprint>();
				var encounterLevel = 0;
				var i = 0;
				var MAX_ITER = 5;
				while (encounterLevel < deltaThreat) {
					var e = Parent.enemyBlueprints [sim.Random (Parent.enemyBlueprints.Count)];
					if (encounterLevel + e.ThreatLevel <= deltaThreat) {
						encounterLevel += e.ThreatLevel;
						encounter.Add (e);
						continue;
					}
					if (i++ >= MAX_ITER) {
						Logger.Info ("AreaAI", "OnRound", "Unable to generate correct group");
						return;
					}
				}

				if (encounter.Count > 0) {
					// find a place where to spawn them
					var p = sim.GetPlayer();
					var minX = p.X - World.FOV_UPDATE_RADIUS;
					var minY = p.Y - World.FOV_UPDATE_RADIUS;
					var maxX = p.X + World.FOV_UPDATE_RADIUS;
					var maxY = p.Y + World.FOV_UPDATE_RADIUS;
					var sminX = p.X - Parent.lastSightRadius;
					var sminY = p.Y - Parent.lastSightRadius;
					var smaxX = p.X + Parent.lastSightRadius;
					var smaxY = p.Y + Parent.lastSightRadius;
					var possibleCells = new List<Vector2i> ();
					//Logger.Info ("AreaAI", "OnRound", "Spawn center " + new {p.X, p.Y}.ToString());
					for(int y = minY; y < maxY; y++) {
						for(int x = minX; x < maxX; x++) {
							// must spawn outside sight
							// TODO if not specified differently?
							if (x > sminX && x < smaxX && y > sminY && y < smaxY) {
								continue;
							}
							if (sim.world.IsWalkable (x, y) && sim.GetObjectAt(x, y) == null) {
								//Logger.Info ("AreaAI", "OnRound", "Adding possible cell " + new {x, y}.ToString());
								possibleCells.Add (new Vector2i (x, y));
							}
						}
					}
					// finally spawn
					foreach (var e in encounter) {
						if (possibleCells.Count > 0) {
							var position = possibleCells [sim.Random (possibleCells.Count)];
							possibleCells.Remove (position);
							sim.CreateObject (e.TemplateID, position.X, position.Y);
							this.CurrentThreatLevel += e.ThreatLevel;
							//Logger.Info ("AreaAI", "OnRound", "Created object " + e.TemplateID + " at " + position.ToString());
						}
					}
				}
			}

			this.UpdateMusic ();
		}

		virtual public void UpdateMusic() {
			var threat = 0;
			foreach (var e in Simulator.Instance.world.entities.Values) {
				threat += (e.Visible == true && e.Dressing == false && e.Static == false) ? 1 : 0;
			}
			if (threat == 1) {
				Logger.Debug ("AreaAI", "UpdateMusic", "threath level low = " + threat.ToString());
				IoManager.SetNextMusicLoop ("combat1");
			} else if (threat > 1) {
				Logger.Debug ("AreaAI", "UpdateMusic", "threath level high = " + threat.ToString());
				IoManager.SetNextMusicLoop ("combat2");
			} else {
				Logger.Debug ("AreaAI", "UpdateMusic", "threath level none = " + threat.ToString());
				IoManager.SetNextMusicLoop ("intro");
			}
		}
	}

	public class ArtificialIntelligence {
		public const string ICE = "ICE";
		public const string LAVA = "LAVA";
		public const string LAVA_EMITTER = "LAVA_EMITTER";
		public const string MELEE = "MELEE";
		public const string RANGED = "RANGED";

		public Entity Parent { get; set; }

		virtual public void OnCreate () {
			return;
		}

		virtual public void OnDamage (ref int howMuch, string type) {
			return;
		}

		virtual public void OnDeath () {
			return;
		}

		virtual public void OnRound () {
			return;
		}
	}

	public class DestructibleAI: ArtificialIntelligence {
		public DestructibleAI(int damage = 1, int damageRadius = 1, string damageType = Simulator.DAMAGE_TYPE_UNTYPED) {
			Damage = damage;
			DamageRadius = damageRadius;
			DamageType = damageType;
			DestructParticle = string.Empty;
		}

		public int Damage { get; set; }

		public int DamageRadius { get; set; }

		public String DamageType { get; set; }

		public string DestructParticle { get; set; }

		override public void OnDeath () {
			return;
			// XXX this first step is important to avoid death chain reactions
			// with myself
			Parent.AI = new ArtificialIntelligence ();
			Logger.Debug ("DestructibleAI", "OnDestroy", "Damaging all around " + Parent.ID);
			if (DestructParticle != String.Empty) {
				Simulator.Instance.CreateParticleAt (this.DestructParticle, Parent.X, Parent.Y);
			}
			var objInRadius = Simulator.Instance.GetObjectsAt (Parent.X, Parent.Y, DamageRadius);
			foreach (var e in objInRadius) {
				if (e != this.Parent) {
					Simulator.Instance.events.WaitAndRun (200, new MethodEvent(() => e.Damage (this.Damage, this.DamageType)));
				}
			}
		}
	}

	public class MeleeAI: ArtificialIntelligence {
		override public void OnRound () {
			var range = this.Parent.CurrentActiveRange;
			var skill = this.Parent.GetPrioritySkillInRange(range);
			//Logger.Debug ("MeleeAI", "OnRound", "Using skill " + skill.Name + " searching in range " + range.ToString());
			var enemiesInRange = Simulator.Instance.GetEnemiesAt ("PLAYER", this.Parent.X, this.Parent.Y, range);
			if (enemiesInRange.Count > 0 && skill != null) {
				Logger.Debug ("MeleeAI", "OnRound", "Trying skill " + skill.Name + " on " + enemiesInRange [0].ID);
				Simulator.Instance.TrySkill (skill, this.Parent, enemiesInRange[0]);
			} else {
				var player = Simulator.Instance.GetPlayer ();
				if (player.Health > 0) {
					var dx = Math.Sign (player.X - this.Parent.X);
					var dy = Math.Sign (player.Y - this.Parent.Y);
					var ex = this.Parent.X + dx;
					var ey = this.Parent.Y + dy;
					var world = Simulator.Instance.world;
					if (Simulator.Instance.IsSafeToWalk (this.Parent,ex, ey)) {
						Logger.Debug ("MeleeAI", "OnRound", "Moving " + Parent.ID.ToString());
						Simulator.Instance.Shift (this.Parent.ID, dx, dy);
					} else if (dx == 0) {
						ex -= 1;
						if (Simulator.Instance.IsSafeToWalk (this.Parent, ex, ey)) {
							Simulator.Instance.Shift (this.Parent.ID, dx - 1, dy);
							return;
						} else if (Simulator.Instance.IsSafeToWalk (this.Parent, ex + 2, ey)) {
							Simulator.Instance.Shift (this.Parent.ID, dx + 1, dy);
							return;
						}
					} else if (dy == 0) {
						ey -= 1;
						if (Simulator.Instance.IsSafeToWalk (this.Parent, ex, ey)) {
							Simulator.Instance.Shift (this.Parent.ID, dx, dy - 1);
							return;
						} else if (Simulator.Instance.IsSafeToWalk (this.Parent, ex, ey + 2)) {
							Simulator.Instance.Shift (this.Parent.ID, dx, dy + 1);
							return;
						}
					} else {
						dx = Simulator.Instance.Random (3) - 1;
						dy = Simulator.Instance.Random (3) - 1;
						Simulator.Instance.CanShift (this.Parent.ID, dx, dy, true);
					}
				}
			}
		}
	}

	public class IceAI: ArtificialIntelligence {
		override public void OnCreate() {
			var entitiesOverMe = Simulator.Instance.GetObjectsAt (this.Parent.X, this.Parent.Y);
			foreach (var entity in entitiesOverMe) {
				if (entity.HasTag ("HAZARD") && entity.HasTag ("GROUND")) {
					entity.Damage (5, Simulator.DAMAGE_TYPE_COLD);
					/*if (entity.HasTag ("ACTIVE") && entity.HasTag ("FIRE")) {
						
					} else {
						Simulator.Instance.Kill (entity);
					}*/
				}
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

		override public void OnCreate() {
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
					Logger.Debug ("LavaAI", "onCreate", "Deleting " + o.ID);
					this.Parent.OutObject.SetAnimation ("IDLE");
					sim.DestroyObject(o.ID);
				}
			}
		}

		override public void OnDamage(ref int howMuch, string type) {
			if (type == Simulator.DAMAGE_TYPE_COLD) {
				Parent.OutObject.ZIndex -= 1;
				Parent.OutObject.IdleAnimation = "BASALT";
				Parent.OutObject.SetAnimation ("SOLIDIFY");
				Parent.Dressing = true;
				Parent.Health = 1;
				Parent.RemoveTag ("ACTIVE");
				Parent.AddTag ("DISABLED");
				var ai = new DestructibleAI (1, 1, Simulator.DAMAGE_TYPE_PHYSICAL);
				ai.DestructParticle = "p_explosion_basalt";
				Parent.AI = ai;
			}
		}

		override public void OnRound () {
			foreach(var entity in Simulator.Instance.GetObjectsAt(Parent.X, Parent.Y)) {
				if (entity != Parent && entity.HasTag("FLYING") == false)
					entity.AddEffect (new BurningEffect ());
			}
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
				Parent.Dressing = true;
				Parent.Health = 1;
				Parent.RemoveTag ("ACTIVE");
				Parent.AddTag ("DISABLED");
				var ai = new DestructibleAI (1, 1, Simulator.DAMAGE_TYPE_PHYSICAL);
				ai.DestructParticle = "p_explosion_basalt";
				Parent.AI = ai;
			}
			this.firstRound = false;
		}

		private void Spawn() {
			this.hasSpawned = true;
			var sim = Simulator.Instance;
			for (int i = 0; i < MAX_SPAWN_COUNT; i++) {
				var x = Parent.X;
				var y = Parent.Y;
				var r = sim.Random ();
				if (r < 0.25f)
					x++;
				else if (r < 0.50f)
					x--;
				else if (r < 0.75f)
					y++;
				else
					y--;
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

		override public void OnRound () {
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
				var lava = sim.GetObject (sim.CreateObject ("bp_fire_lava", Parent.X, Parent.Y + 1));
				var ai = lava.AI as LavaAI;//new LavaAI ();
				ai.Initiative = this.oldInitiative + Simulator.ROUND_LENGTH;
				ai.Generation = 1;
				sim.SetAnimation (lava, "CREATE");
			}
		}
	}

	public class UserAI: ArtificialIntelligence {
		override public void OnRound () {
			Simulator.Instance.events.WaitingForUser = !Simulator.Instance.events.RunUserEvent ();
		}

		override public void OnDeath () {
			var position = new Vector2f (IoManager.Width / 2, IoManager.Height / 2 - 100);
			Logger.Info ("UserAI", "OnDeath", "PLAYER is Dead");
			var label = new Label ("YOU HAVE FALLEN", 48);
			label.AlignCenter = true;
			label.Color = SFML.Graphics.Color.Red;
			label.Position = position;
			IoManager.AddWidget (label);

			label = new Label ("Thank you for playing this Alpha release", 32);
			label.AlignCenter = true;
			label.Color = SFML.Graphics.Color.White;
			position.Y += 200;
			label.Position = Parent.OutObject.Position;
			IoManager.AddWidget (label);

			label = new Label ("Lots of things will change for the final release", 32);
			label.AlignCenter = true;
			label.Color = SFML.Graphics.Color.White;
			position.Y += 40;
			label.Position = position;
			IoManager.AddWidget (label);

			label = new Label ("But we would gladly accept your comments and critiques!", 32);
			label.AlignCenter = true;
			label.Color = SFML.Graphics.Color.White;
			position.Y += 40;
			label.Position = position;
			IoManager.AddWidget (label);

			label = new Label ("Contact us on http://wizardsduelgame.wordpress.com!", 32);
			label.AlignCenter = true;
			label.Color = SFML.Graphics.Color.White;
			position.Y += 40;
			label.Position = position;
			IoManager.AddWidget (label);
		}
	}

}

