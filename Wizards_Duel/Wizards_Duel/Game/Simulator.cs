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
using SFML.Graphics;
using SFML.Window;
using WizardsDuel.Io;
using WizardsDuel.Utils;
using System.Collections.Generic;

namespace WizardsDuel.Game
{
	public class Simulator
	{
		internal int createdEntityCount = 0;
		public EventManager events;
		private static Simulator instance;
		private Random rng = new Random ();
		internal World world = new World();

		public const string BLOOD_PARTICLE = "P_BLEED";
		public const string DEFAULT_DATA = "Data/Test.xml";
		public const string DEFAULT_LEVEL = "Data/TestLevel.xml";
		public const string HEALTH_VARIABLE = "HEALTH";
		public const string PLAYER_ID = "Player";
		public const string SPAWN_PARTICLE = "P_SPAWN";

		private Simulator() {}

		public static Simulator Instance {
			get {
				if (instance == null) {
					instance = new Simulator();
				}
				return instance;
			}
		}

		public void Initialize(out WorldView worldView, bool initUserEvents = true) {
			this.world = GameFactory.LoadGame(DEFAULT_DATA);
			worldView = this.world.worldView;
			this.events = new EventManager (this);
			this.LoadArea ();
		}

		public void AddEvent(Event evt) {
			this.events.AppendEvent (evt);
		}

		public void AddLight(float x, float y, float radius, Color color) {
			this.world.worldView.LightLayer.AddLight (x, y, radius, color);
		}

		public void AddLight(string oid, float radius, Color color) {
			try {
				Entity en;
				if (this.world.entities.TryGetValue (oid, out en)) {
					var light = this.world.worldView.LightLayer.AddLight (0f, 0f, radius, color);
					light.Parent = en.OutObject;
				}
			} catch {
			}
		}

		public void AddLight(Particle particle, float radius, Color color) {
			var light = this.world.worldView.LightLayer.AddLight (0f, 0f, radius, color);
			light.Parent = particle;
		}

		public void Attack(string attackerId, string targetId) {
			//this.events.AppendEvent(new AttackEvent(GetObject(attackerId), GetObject(targetId)));
			var Actor = GetObject(attackerId);
			var Target = GetObject (targetId);
			Logger.Debug ("Simulator", "Attack", Actor.ID + " attacks " + Target.ID);
			SetAnimation (Actor, "ATTACK");
			if (Actor.X != Target.X) {
				Actor.OutObject.Facing = (Actor.X < Target.X) ? Facing.RIGHT : Facing.LEFT;
				Target.OutObject.Facing = (Actor.X < Target.X) ? Facing.LEFT : Facing.RIGHT;
			}
			var newHealth = Target.GetVar (Simulator.HEALTH_VARIABLE, 1) - 1;
			Target.SetVar (Simulator.HEALTH_VARIABLE, newHealth);
			if (newHealth > 0) {
				if (Target.GetVar ("armor") < 1) {
					Simulator.Instance.Bleed (Target);
				}
			} else {
				Simulator.Instance.Kill (Target);
			}
		}

		public void Bleed(Entity target) {
			CreateParticleOn (BLOOD_PARTICLE, target);
			this.events.WaitFor (500);
		}

		public bool CanMove(string oid, int x, int y, bool moveIfPossible = false) {
			Entity en;
			bool res = false;
			if (this.world.entities.TryGetValue (oid, out en)) {
				var tile = this.world.GetTile(x, y);
				res = tile.Template.IsWalkable;
				if (moveIfPossible && res) {
					Move (oid, x, y);
				}
			}
			return res;
		}

		/// <summary>
		/// Determines whether the object can attempt to move the specific delta.
		/// If the final tile is occupied by something an action different than shifting may happen
		/// for example attacking something of a different faction
		/// </summary>
		/// <returns><c>true</c> if this object can shift.</returns>
		/// <param name="oid">Object ID.</param>
		/// <param name="dx">Delta X.</param>
		/// <param name="dy">Delta Y.</param>
		/// <param name="shiftIfPossible">If set to <c>true</c> run Shift().</param>
		public bool CanShift(string oid, int dx, int dy, bool shiftIfPossible = false) {
			Entity en;
			bool res = false;
			try {
				if (this.world.entities.TryGetValue (oid, out en) && this.world.IsValid(en.X + dx, en.Y + dy)) {
					var tile = this.world.GetTile(en.X + dx, en.Y + dy);
					res = tile.Template.IsWalkable;
					if (shiftIfPossible && res) {
						//Logger.Debug ("Simulator", "CanShift", "shifting " + oid);
						Shift (oid, dx, dy);
					}
					else {
						Logger.Debug ("Simulator", "CanShift", "does not shift " + oid);
						//Shift (oid, dx, dy);
					}
				}
			} catch (Exception ex) {
				Logger.Debug ("Simulator", "CanShift", ex.ToString());
			}
			return res;
		}

		public void Cast(string oid, int gx, int gy) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				if (res.OutObject.IsAnimating) {
					return;
				};
				res.OutObject.SetAnimation ("CAST1");
				Entity target;
				var targetId = GetObjectAt (gx, gy);
				if (targetId != null && this.world.entities.TryGetValue (targetId, out target)) {
					if (target == res) {
						if (res.GetVar ("armor") == 0) {
							CreateParticleOn ("p_truce", PLAYER_ID);
							res.SetVar ("armor", 1);
						} else {
							RemoveParticle (PLAYER_ID, "p_truce");
							res.SetVar ("armor", 0);
						}
					} else {
						CreateParticleOn ("p_hurt", targetId);
						//this.events2.WaitFor (900);
						//this.AddEvent (new KillEvent (target.ID));
						Kill (target);
					}
				} else {
					//if (gx % 2 == 1) {
						//CreateObject (this.createdEntityCount.ToString (), "bp_firefly", gx, gy);
						/*var id = "lava_" + createdEntityCount.ToString ();
						CreateObject (id, "bp_fire_lava", gx, gy);
						var lava = GetObject (id);
						SetAnimation (lava, "CREATE");*/
					//System.Threading.Thread.Sleep(200);
					/*for (int i = 0; i < 4; i++) {
						var id = "lava_" + createdEntityCount.ToString ();
						CreateObject (id, "bp_fire_lava", gx, gy);
						var lava = GetObject (id);
						lava.OutObject.X += (Random (6) - 3)*6;
						lava.OutObject.Y += (Random (6) - 3)*6;
						lava.OutObject.ZIndex -= Random (10);
						SetAnimation (lava, "CREATE");
						//System.Threading.Thread.Sleep(200);
					}*/
					var r = Random (100);
					if (r < 30) {
						CreateObject (createdEntityCount.ToString (), "bp_fire_thug1", gx, gy);
					} else if (r < 60) {
						CreateObject (createdEntityCount.ToString (), "bp_fire_salamander1", gx, gy);
					} else {
						CreateObject (createdEntityCount.ToString (), "bp_fire_bronze_thug1", gx, gy);
					}
					//CreateParticleOn ("p_lava", lava);
					//CreateParticleAt ("p_lava", gx, gy);
					//} else {*/
						//CreateObject (this.createdEntityCount.ToString (), "bp_fire_entrance", gx, gy);
						//CreateParticleAt ("p_lava", gx, gy);
					//}
				}
			}
		}

		protected int CellHeight {
			get { return this.world.worldView.CellHeight; }
		}

		protected int CellObjectOffset {
			get { return this.world.worldView.CellObjectOffset; }
		}

		protected int CellWidth {
			get { return this.world.worldView.CellWidth; }
		}

		public void ClearUserEvent() {
			this.events.ClearUserEvent ();
		}

		public void CreateObject(string oid, string templateId, int gx=0, int gy=0) {
			var newEntity = GameFactory.LoadFromTemplate (templateId, oid);
			if (newEntity != null) {
				this.world.worldView.ObjectsLayer.AddObject (newEntity.OutObject);
				this.world.entities.Add (oid, newEntity);
				Move (oid, gx, gy);
				if (templateId == "bp_fire_garg1") {
					AddLight (oid, 196, new Color(255, 102, 0, 128));
					//this.events2.AppendEvent (new UserEvent (this.events2));
				} else {
					//CreateParticleOn (SPAWN_PARTICLE, newEntity);
					//this.events.AppendEvent (new AiEvent (oid, 10));
				}
				this.events.QueueObject (newEntity, createdEntityCount + 10);
				createdEntityCount++;
			} else {
				Logger.Warning ("Simulator", "CreateObject", "cannot create " + oid + " " + templateId);
			}
		}

		public void CreateParticleAt(string pid, int gx, int gy) {
			var ps = GameFactory.LoadParticleFromTemplate (pid, gx * this.CellWidth, gy * this.CellHeight, this.world.worldView.ObjectsLayer);
			IoManager.AddWidget (ps);
			Logger.Debug ("Simulator", "CreateParticle", ps.ToString());
		}

		public void CreateParticleOn(string pid, Entity target) {
			var flip = (target.OutObject.Facing == Facing.RIGHT) ? false : true;
			var ps = GameFactory.LoadParticleFromTemplate (pid, 0f, 0f, this.world.worldView.ObjectsLayer, flip);
			target.OutObject.AddParticleSystem (ps);
			IoManager.AddWidget (ps);
		}

		public void CreateParticleOn(string pid, string oid) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				var ps = GameFactory.LoadParticleFromTemplate (pid, 0f, 0f, this.world.worldView.ObjectsLayer);
				res.OutObject.AddParticleSystem (ps);
				IoManager.AddWidget (ps);
			}
		}

		public void DestroyObject(string oid) {
			Logger.Debug ("Simulator", "DestroyObject", "Trying to destroy " + oid);
			Entity res;
			if (oid != PLAYER_ID && this.world.entities.TryGetValue (oid, out res)) {
				if (res.DeathAnimation == String.Empty) {
					this.world.worldView.ObjectsLayer.DeleteObject (res.OutObject);
				}
				this.world.entities.Remove (oid);
				Logger.Debug ("Simulator", "DestroyObject", "Destroyed " + oid);
			}
		}

		public void DoLogic() {
			this.events.Dispatch ();
		}

		public string GetObjectAt(int x, int y) {
			return this.world.GetObjectAt (x, y);
		}

		public Entity GetObject(string oid) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				return res;
			} else {
				return null;
			}
		}

		public bool IsUserEventInQueue() {
			return this.events.UserEventInQueue;
		}

		public void Kill(Entity target) {
			Logger.Debug ("Simulator", "Kill", "Trying to kill " + target.ID);
			if (target.DeathAnimation == String.Empty) {
				//CreateParticleAt (SPAWN_PARTICLE, target.X, target.Y);
				var ps = new ParticleSystem ("DEATH");
				//ps.AddParticle (new Particle ("FX01.png", new IntRect (0, 0, 1, 1)));
				ps.TTL = 2000;
				ps.Layer = this.world.worldView.ObjectsLayer;
				ps.Position = new Vector2f (target.X * this.CellWidth + target.DeathRect.Left, target.Y * this.CellHeight + target.DeathRect.Top);

				var emitter = new Emitter (ps, 0);
				//emitter.Offset = new Vector2f (target.DeathRect.Top, target.DeathRect.Left);
				emitter.ParticleTTL = 1000;
				emitter.SpawnCount = 64;
				emitter.SpawnDeltaTime = 50;
				emitter.TTL = 90;
				emitter.AddParticleTemplate ("FX01.png", 0, 0, 1, 1, 2);
				emitter.AddAnimator (new GravityAnimation (new Vector2f (0f, 0.0002f)));
				emitter.AddAnimator (new FadeAnimation (0, 0, 1000));
				emitter.AddVariator (new GridSpawner (target.DeathRect.Width, target.DeathRect.Height, 2, 2));
				emitter.AddVariator (new BurstSpawner (0.05f));
				var cps = new ColorPickerSpawner ();
				cps.AddColor (target.DeathMain);
				cps.AddColor (target.DeathMain);
				cps.AddColor (target.DeathSecundary);
				emitter.AddVariator (cps);
				ps.AddEmitter (emitter);

				//var ps = GameFactory.LoadParticleFromTemplate (pid, target.X * this.CellWidth, target.Y * this.CellHeight, this.world.worldView.ObjectsLayer);
				IoManager.AddWidget (ps);
				target.OutObject.AddAnimator (new FadeAnimation (0, 0, 400));
			} else {
				SetAnimation (target, target.DeathAnimation);
			}

			Logger.Debug ("Simulator", "Cast", "Command to to kill " + target.ID);
			this.events.WaitAndRun(500, new DestroyEvent (target.ID));
		}

		public void LoadArea() {
			this.world = GameFactory.LoadGame(DEFAULT_DATA);
			this.events = new EventManager (this);
			this.events.QueueObject (this.world, 15);

			// by default always create the player object, which is indestructible
			if (!this.world.entities.ContainsKey(PLAYER_ID))
				this.CreateObject (PLAYER_ID, "bp_ezekiel");
			this.world.worldView.ReferenceObject = GetObject (PLAYER_ID).OutObject;
			this.AddLight (PLAYER_ID, 300, new Color(254, 250, 235));

			var wf = new WorldFactory ();
			wf.Initialize (DEFAULT_LEVEL);
			wf.Generate (this.world);
			this.world.worldView.EnableGrid (false);

			if (world.StartCell.X != 0 && world.StartCell.Y != 0) {
				Move(PLAYER_ID, world.StartCell.X, world.StartCell.Y);
			} else {
				var done = false;
				do {
					var x = Random (world.GridWidth - 1);
					var y = Random (world.GridHeight - 1);
					if (world.GetTile (x, y).Template.IsWalkable == true) {
						done = true;
						this.Move (PLAYER_ID, x, y);
					}
				} while(done != true);
			}

			foreach (var r in this.world.worldView.dungeon) {
				//Logger.Info ("Simulator", "LoadArea", "r: " + r + " (" + r.Length.ToString() + ")");
			}
		}

		public Dictionary<string, Entity> ListEnemies() {
			var entities = this.world.entities;
			var res = new Dictionary<string, Entity> (entities);
			res.Remove (PLAYER_ID);
			return res;
		}

		public void Move(string oid, int gx, int gy) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				res.X = gx;
				res.Y = gy;
				// XXX The X is shifted by 1 on the right to offset the padding
				// of the "Layers" of the WorldView
				//res.OutObject.Position = new Vector2f ((res.X+1) * this.cellWidth, res.Y * this.cellHeight - this.cellHeight/4);
				res.OutObject.Position = new Vector2f ((res.X) * this.CellWidth, (res.Y) * this.CellHeight + this.CellObjectOffset);
			}
		}

		public void RemoveParticle(string oid, string particleId) {
			var o = this.GetObject (oid);
			if (o != null) {
				o.OutObject.RemoveParticleSystem (particleId);
			}
		}

		/// <summary>
		/// Returns a random float between 0.0 (included) and 1.0 (excluded)
		/// </summary>
		public float Random() {
			return (float)this.rng.NextDouble ();
		}

		/// <summary>
		/// Returns a random int between min (included) and max (excluded)
		/// </summary>
		/// <param name="max">Maximum value, not included.</param>
		/// <param name="min">Minimum value, included.</param>
		public int Random(int max, int min = 0) {
			return this.rng.Next (min, max);
		}

		public void SetAnimation(Entity obj, string animation) {
			if (obj != null) {
				obj.OutObject.SetAnimation (animation);
			}
		}

		public void SetAnimation(string oid, string animation) {
			var o = this.GetObject (oid);
			if (o != null) {
				o.OutObject.SetAnimation (animation);
			}
		}

		public void SetUserEvent(Event evt) {
			//this.events.SetUserEvent (evt);
			this.events.SetUserEvent (evt);
		}

		public void Shift(string oid, int dx, int dy) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				//Logger.Debug ("Simulator", "Shift", "Shifting 1");
				if (res.OutObject.IsAnimating) {
					return;
				};
				//Logger.Debug ("Simulator", "Shift", "Shifting 2");
				var endX = res.X + dx;
				var endY = res.Y + dy;
				var bufferId = GetObjectAt (endX, endY);
				if (bufferId == null || GetObject (bufferId).Dressing == true) {
					// nothing in the way, move around
					res.X = endX;
					res.Y = endY;

					var ta = new TranslateAnimation (res.OutObject.GetAnimationLength ("SHIFT"), dx * this.CellWidth, dy * this.CellHeight);
					res.OutObject.AddAnimator (ta);

					//Logger.Debug ("Simulator", "Shift", "Shifting 3 " + ta.deltaX.ToString () + "," + ta.deltaY.ToString ());
					//Logger.Debug ("Simulator", "Shift", "Moving to " + endX.ToString () + "," + endY.ToString ());

					if (dx < 0) {
						res.OutObject.Facing = Io.Facing.LEFT;
					} else if (dx > 0) {
						res.OutObject.Facing = Io.Facing.RIGHT;
					}
					SetAnimation (res, "SHIFT");
					// goto new area
					Logger.Debug ("Simulator", "Shift", "Moving to " + endX.ToString () + "," + endY.ToString () +  " vs " + world.EndCell.ToString());
					if (oid == PLAYER_ID && world.EndCell.X == endX && world.EndCell.Y == endY) {
						LoadArea ();
					}
				} else if (res.Faction != GetObject (bufferId).Faction) {
					// something on my path, attack it
					Logger.Debug ("Simulator", "Shift", "Found entity " + res.ToString () + " at " + endX.ToString () + "," + endY.ToString ());
					//events.WaitFor (500); // TODO movement animation end
					this.Attack (oid, bufferId);
				}
				//Logger.Debug ("Simulator", "Shift", "Shifting 4");
			}
		}

		public void ShowGrid(bool show) {
			world.worldView.EnableGrid (show);
		}

		public void ToggleGrid() {
			world.worldView.ToggleGrid ();
		}
	}
}

