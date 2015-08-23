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
		internal Skill currentSkill = null;
		internal EventManager events;
		private static Simulator instance;
		private Random rng = new Random ();
		internal World world = new World();

		public const string BLOOD_PARTICLE = "P_BLEED";
		public const string DEFAULT_DATA = "Data/Test_priv.xml";
		public const string DEFAULT_LEVEL = "Data/TestLevel.xml";
		public const string HEALTH_VARIABLE = "HEALTH";
		public const string PLAYER_ID = "Player";
		public const int ROUND_LENGTH = 100;
		public const string SPAWN_PARTICLE = "P_SPAWN";

		public readonly Color COOLDOWN_BAR_COLOR = new Color(0, 0, 0, 127);
		public readonly Color DAMAGE_BAR_COLOR = new Color(255, 0, 0, 127);
		public readonly Color SELECTED_SKILL_COLOR = Color.Cyan;
		public readonly Color UNSELECTED_SKILL_COLOR = new Color(127, 127, 127);
		public const string TARGET_ICON_ID = "TARGET_ICON_ID";
		public const int UI_VERTICAL_START = 16;
		public const int UI_VERTICAL_SPACE = 96;
		public const int UI_SCALE = 2;

		public const string DAMAGE_TYPE_COLD = "COLD";
		public const string DAMAGE_TYPE_FIRE = "FIRE";
		public const string DAMAGE_TYPE_HOLY = "HOLY";
		public const string DAMAGE_TYPE_NECRO = "NECRO";
		public const string DAMAGE_TYPE_PHYSICAL = "PHYSICAL";
		public const string DAMAGE_TYPE_UNTYPED = "";

		private Simulator() {}

		public static Simulator Instance {
			get {
				if (instance == null) {
					instance = new Simulator();
				}
				return instance;
			}
		}

		public void Initialize(out WorldView worldView) {
			this.world = GameFactory.LoadGame(DEFAULT_DATA);
			worldView = this.world.worldView;
			this.events = new EventManager (this);
			this.LoadArea ();
		}

		public void InitializeUserInterface() {
			var targetPortrait = new Icon("", new IntRect(0,0,0,0));
			IoManager.AddWidget (targetPortrait, TARGET_ICON_ID);
			var y = UI_VERTICAL_START;
			var player = GetPlayer ();
			player.OutIcon.ScaleX = UI_SCALE;
			player.OutIcon.ScaleY = UI_SCALE;
			player.Border = new SolidBorder (this.UNSELECTED_SKILL_COLOR, 2f);
			player.OutIcon.AddDecorator (player.Border);
			player.DamageBar = new DamageBarDecorator (DAMAGE_BAR_COLOR);
			player.DamageBar.InvertAxis = true;
			player.OutIcon.AddDecorator (player.DamageBar);
			player.OutIcon.Position = new Vector2f (UI_VERTICAL_START, y);
			IoManager.AddWidget (player.OutIcon);
			foreach (var s in player.skills) {
				if (s.Show) {
					y += UI_VERTICAL_SPACE;
					s.OutIcon = new Icon (s.IconTexture, s.IconRect);
					s.OutIcon.ScaleX = UI_SCALE;
					s.OutIcon.ScaleY = UI_SCALE;
					s.Border = new SolidBorder (this.UNSELECTED_SKILL_COLOR, 2f);
					s.OutIcon.AddDecorator (s.Border);
					s.DamageBar = new DamageBarDecorator (COOLDOWN_BAR_COLOR);
					s.DamageBar.InvertAxis = true;
					s.OutIcon.AddDecorator (s.DamageBar);
					s.OutIcon.Position = new Vector2f (UI_VERTICAL_START, y);
					IoManager.AddWidget (s.OutIcon);
				}
			}
			foreach (var s in player.skills) {
				if (s.Show) {
					s.Border.Color = SELECTED_SKILL_COLOR;
					break;
				}
			}
		}

		public void AddEffect (string oid, Effect effect) {
			var target = GetObject (oid);
			target.AddEffect (effect);
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

		public void Attack(string attackerId, string targetId, int damage, string damageType) {
			var actor = GetObject(attackerId);
			var target = GetObject (targetId);
			Logger.Debug ("Simulator", "Attack", actor.ID + " attacks " + target.ID);
			if (actor.X != target.X && target.Static == false) {
				actor.OutObject.Facing = (actor.X < target.X) ? Facing.RIGHT : Facing.LEFT;
				target.OutObject.Facing = (actor.X < target.X) ? Facing.LEFT : Facing.RIGHT;
			}
			target.Damage (damage, damageType);
			if (target.ID != PLAYER_ID && attackerId == PLAYER_ID) {
				IoManager.AddWidget (target.OutIcon, TARGET_ICON_ID);
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
			bool res = false;
			try {
				var en = GetObject(oid);
				var objectsAtEnd = this.GetObjectsAt(en.X + dx, en.Y + dy);
				if (this.world.IsValid(en.X + dx, en.Y + dy) && objectsAtEnd.Find(x => x.Static) == null) {
					//var tile = this.world.GetTile(en.X + dx, en.Y + dy);
					//res = tile.Template.IsWalkable;
					res = this.IsSafeToWalk(en, en.X + dx, en.Y + dy);
					if (shiftIfPossible && res) {
						//Logger.Debug ("Simulator", "CanShift", "shifting " + oid);
						Shift (oid, dx, dy);
					} else if (oid == PLAYER_ID && objectsAtEnd.Find(x => x.Faction == "MONSTERS") != null) {
						Shift (oid, dx, dy);
					} else {
						Logger.Debug ("Simulator", "CanShift", "does not shift " + oid);
						//Shift (oid, dx, dy);
					}
				}
			} catch (Exception ex) {
				Logger.Debug ("Simulator", "CanShift", ex.ToString());
			}
			return res;
		}

		public void Click(string oid, int gx, int gy) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				if (res.OutObject.IsAnimating) {
					return;
				};
				//res.OutObject.SetAnimation ("CAST1");
				var targets = GetObjectsAt(gx, gy);
				if (targets.Count > 0) {
					var target = targets.Find (x => x.Faction == "MONSTERS");
					//if (target == res) {
					if (targets.Find (x => x == res) != null) {
						TrySkill (currentSkill, res);
						//currentSkill.OnSelf (res);
					} else if (target.Dressing == false) {
						TrySkill (currentSkill, res, target);
						/*currentSkill.OnTarget (res, target);
						target.DamageBar.Level = 1f - (float)target.Health / (float)target.MaxHealth;
						IoManager.AddWidget (target.OutIcon, TARGET_ICON_ID);*/
					}
				} else {
					//CreateObject (createdEntityCount.ToString (), "bp_fire_thug1", gx, gy);
					/*
					var r = Random (100);
					if (r < 30) {
						CreateObject (createdEntityCount.ToString (), "bp_firefly", gx, gy);
					//*
					} else if (r < 50) {
						CreateObject (createdEntityCount.ToString (), "bp_fire_thug1", gx, gy);
					} else if (r < 80) {
						CreateObject (createdEntityCount.ToString (), "bp_fire_salamander1", gx, gy);
					} else {
						CreateObject (createdEntityCount.ToString (), "bp_fire_bronze_thug1", gx, gy);
					}//*/
				}
			}
		}

		public int CellHeight {
			get { return this.world.worldView.CellHeight; }
		}

		protected int CellObjectOffset {
			get { return this.world.worldView.CellObjectOffset; }
		}

		public int CellWidth {
			get { return this.world.worldView.CellWidth; }
		}

		public void ClearUserEvent() {
			this.events.ClearUserEvent ();
		}

		public string CreateObject(string templateId, int gx=0, int gy=0) {
			var id = templateId + createdEntityCount.ToString ();
			CreateObject (id, templateId, gx, gy);
			return id;
		}

		public Entity CreateObject(string oid, string templateId, int gx=0, int gy=0) {
			var newEntity = GameFactory.LoadFromTemplate (templateId, oid);
			if (newEntity != null) {
				this.world.worldView.ObjectsLayer.AddObject (newEntity.OutObject);
				this.world.entities.Add (oid, newEntity);
				Move (oid, gx, gy);
				if (newEntity.OutObject.LightRadius > 1) {
					AddLight (oid, newEntity.OutObject.LightRadius, newEntity.OutObject.LightColor);
				} else {
					//CreateParticleOn (SPAWN_PARTICLE, newEntity);
					//this.events.AppendEvent (new AiEvent (oid, 10));
				}
				this.events.Initiative++;
				this.events.QueueObject (newEntity, InitiativeCount/*createdEntityCount + 10*/);
				createdEntityCount++;
				newEntity.AI.OnCreate ();
				newEntity.Visible = false;
				newEntity.OutObject.Color = Color.Transparent;

				if (oid != PLAYER_ID) {
					newEntity.OutIcon.ScaleX = UI_SCALE;
					newEntity.OutIcon.ScaleY = UI_SCALE;
					newEntity.OutIcon.Position = new Vector2f(IoManager.Width - UI_VERTICAL_START - newEntity.OutIcon.Width, UI_VERTICAL_START);
					newEntity.Border = new SolidBorder (this.UNSELECTED_SKILL_COLOR, 2f);
					newEntity.OutIcon.AddDecorator (newEntity.Border);
					newEntity.DamageBar = new DamageBarDecorator (DAMAGE_BAR_COLOR);
					newEntity.DamageBar.InvertAxis = true;
					newEntity.OutIcon.AddDecorator (newEntity.DamageBar);
				}

				return newEntity;
			} else {
				Logger.Warning ("Simulator", "CreateObject", "cannot create " + oid + " " + templateId);
				return null;
			}
		}

		public void CreateParticleAt(string pid, int gx, int gy) {
			var ps = GameFactory.LoadParticleFromTemplate (pid, (gx + 0.5f) * this.CellWidth, (gy + 0.5f) * this.CellHeight, this.world.worldView.ObjectsLayer);
			if (ps != null) {
				IoManager.AddWidget (ps);
				Logger.Debug ("Simulator", "CreateParticle", ps.ToString ());
			}
		}

		public void CreateParticleOn(string pid, Entity target) {
			var flip = (target.OutObject.Facing == Facing.RIGHT) ? false : true;
			var ps = GameFactory.LoadParticleFromTemplate (pid, 0f, 0f, this.world.worldView.ObjectsLayer, flip);
			if (ps != null) {
				target.OutObject.AddParticleSystem (ps);
				IoManager.AddWidget (ps);
			}
		}

		public void CreateParticleOn(string pid, string oid) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				var ps = GameFactory.LoadParticleFromTemplate (pid, 0f, 0f, this.world.worldView.ObjectsLayer);
				if (ps != null) {
					res.OutObject.AddParticleSystem (ps);
					IoManager.AddWidget (ps);
				}
			}
		}

		public void DestroyObject(string oid) {
			Logger.Debug ("Simulator", "DestroyObject", "Trying to destroy " + oid);
			Entity res;
			if (oid != PLAYER_ID && this.world.entities.TryGetValue (oid, out res)) {
				res.OutObject.RemoveAllParticleSystems ();
				this.world.worldView.ObjectsLayer.DeleteObject (res.OutObject);
				this.world.entities.Remove (oid);
				Logger.Debug ("Simulator", "DestroyObject", "Destroyed " + oid);
			}
		}

		public void DoLogic() {
			this.events.Dispatch ();
		}

		public List<Entity> GetEnemiesAt(string enemyFaction, int x, int y, int radius = 0) {
			var res = new List<Entity>();
			foreach (var e in this.world.entities.Values) {
				if (
					e.X >= x - radius && e.X <= x + radius && e.Y >= y - radius && e.Y <= y + radius 
					&& e.Health > 0 
					&& e.Faction == enemyFaction
				) {
					res.Add (e);
				}
			}
			return res;
		}

		public string GetObjectAt(int x, int y) {
			return this.world.GetObjectAt (x, y);
		}

		public Entity GetObject(string oid) {
			Entity res;
			if (oid != null && this.world.entities.TryGetValue (oid, out res)) {
				return res;
			} else {
				return null;
			}
		}

		public List<Entity> GetObjectsAt(int x, int y, int radius = 0) {
			var res = new List<Entity>();
			foreach (var e in this.world.entities.Values) {
				if (e.X >= x - radius && e.X <= x + radius && e.Y >= y - radius && e.Y <= y + radius) {
					res.Add (e);
				}
			}
			return res;
		}

		public Entity GetPlayer() {
			Entity res;
			if (this.world.entities.TryGetValue (PLAYER_ID, out res)) {
				return res;
			} else {
				return null;
			}
		}

		public int InitiativeCount {
			get { return events.Initiative; }
		}

		public bool IsSafeToWalk(Entity walker, int ex, int ey) {
			var res = this.world.IsWalkable (ex, ey);
			if (res == true) {
				var entities = this.world.GetObjectsAt (ex, ey);
				foreach (var entity in entities) {
					if (entity.Faction != "NEUTRAL") {
						return false;
					} else if (entity.HasTag("TRAP") && entity.HasTag("ACTIVE")) { 
						if (entity.HasTag ("GROUND") && !walker.HasTag ("FLYING")) {
							return false;
						}
					}
				}
			}
			return res;
		}

		public void Kill(Entity target, int delayMillis = 0) {
			Logger.Debug ("Simulator", "Kill", "Trying to kill " + target.ID);
			target.Kill ();
			if (target.DeathAnimation == String.Empty) {
				//CreateParticleAt (SPAWN_PARTICLE, target.X, target.Y);
				var ps = new ParticleSystem ("DEATH");
				//ps.AddParticle (new Particle ("FX01.png", new IntRect (0, 0, 1, 1)));
				ps.TTL = 2000;
				ps.Layer = this.world.worldView.ObjectsLayer;
				ps.Position = new Vector2f (target.X * this.CellWidth + target.DeathRect.Left, target.Y * this.CellHeight + target.DeathRect.Top);

				var emitter = new Emitter (ps, 0);
				//emitter.Offset = new Vector2f (target.DeathRect.Top, target.DeathRect.Left);
				emitter.ParticleTTL = 800;
				emitter.SpawnCount = 64;
				emitter.SpawnDeltaTime = 50;
				emitter.StartDelay = 250;
				emitter.TTL = 90;
				emitter.AddParticleTemplate ("FX01.png", 0, 0, 1, 1, 2);
				emitter.AddParticleTemplate ("FX01.png", 0, 0, 1, 1, 2);
				emitter.AddParticleTemplate ("FX01.png", 0, 0, 1, 1, 4);
				emitter.AddAnimator (new GravityAnimation (new Vector2f (0f, 0.0002f)));
				emitter.AddAnimator (new FadeAnimation (0, 0, 800));
				emitter.AddVariator (new GridSpawner (target.DeathRect.Width, target.DeathRect.Height, 4, 4));
				emitter.AddVariator (new BurstSpawner (0.05f));
				var cps = new ColorPickerSpawner ();
				cps.AddColor (target.DeathMain);
				cps.AddColor (target.DeathMain);
				cps.AddColor (target.DeathSecundary);
				emitter.AddVariator (cps);
				ps.AddEmitter (emitter);

				//var ps = GameFactory.LoadParticleFromTemplate (pid, target.X * this.CellWidth, target.Y * this.CellHeight, this.world.worldView.ObjectsLayer);
				IoManager.AddWidget (ps);
				target.OutObject.AddAnimator (new FadeAnimation (0, delayMillis, 500));
			} else {
				SetAnimation (target, target.DeathAnimation);
			}

			Logger.Debug ("Simulator", "Cast", "Command to to kill " + target.ID);
			this.events.WaitAndRun(delayMillis + 500, new DestroyEvent (target.ID));
		}

		public void LoadArea() {
			this.world = GameFactory.LoadGame(DEFAULT_DATA);
			this.events = new EventManager (this);
			this.events.QueueObject (this.world, 15);

			// by default always create the player object, which is indestructible
			if (!this.world.entities.ContainsKey (PLAYER_ID)) {
				if (Random (100) < 50) {
					this.CreateObject (PLAYER_ID, "bp_exekiel");
				} else {
					this.CreateObject (PLAYER_ID, "bp_exekiel");
				}
			}
			var playerObject = GetObject (PLAYER_ID);
			playerObject.AI = new UserAI ();
			playerObject.HasEnded = true;
			playerObject.Visible = true;
			playerObject.OutObject.Color = Color.White;
			this.world.worldView.ReferenceObject = GetObject (PLAYER_ID).OutObject;
			//this.AddLight (PLAYER_ID, 300, new Color(254, 250, 235));

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
			this.world.CalculateFoV (playerObject.X, playerObject.Y, 6);
			this.events.DispatchAll ();
			this.events.AcceptEvent = true;
			this.InitializeUserInterface ();
			// XXX After the user interface is loaded!
			this.SelectedSkill = 1;
			Logger.Debug ("Simulator", "LoadArea", "Starting fade at " + IoManager.Time.ToString());
			IoManager.FadeTo (Color.Transparent, 1500);
			IoManager.PlayMusic ("intro");
			IoManager.SetNextMusicLoop ("intro");
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

		public void Select(Entity entity) {
			if (entity != null && entity.ID != PLAYER_ID) {
				entity.DamageBar.Level = 1f - (float)entity.Health / (float)entity.MaxHealth;
				IoManager.AddWidget (entity.OutIcon, TARGET_ICON_ID);
			}
		}

		public void Select(int gx, int gy) {
			Select (GetObject(GetObjectAt (gx, gy)));
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

		private int selectedSkill = 0;
		public int SelectedSkill { 
			get { return this.selectedSkill; } 
			set {
				var i = 0;
				var selected = false;
				foreach (var s in GetPlayer().skills) {
					if (s.Show) {
						if (++i == value) {
							s.Border.Color = SELECTED_SKILL_COLOR;
							this.currentSkill = s;
							selected = true;
						} else {
							s.Border.Color = UNSELECTED_SKILL_COLOR;
						}
					}
				}
				// if nothing selected just go to the default skill
				if (selected == false) {
					foreach (var s in GetPlayer().skills) {
						if (s.Show) {
							s.Border.Color = SELECTED_SKILL_COLOR;
							this.currentSkill = s;
							return;
						}
					}
				}
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

					if (res.Visible == true) {
						var ta = new TranslateAnimation (res.OutObject.GetAnimationLength ("SHIFT"), dx * this.CellWidth, dy * this.CellHeight);
						res.OutObject.AddAnimator (ta);
					} else {
						var ta = new TranslateAnimation (50, dx * this.CellWidth, dy * this.CellHeight);
						res.OutObject.AddAnimator (ta);
					}

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
						Logger.Debug ("Simulator", "Shift", "Starting fade out at " + IoManager.Time.ToString());
						IoManager.FadeTo (Color.Black, 500);
						events.WaitAndRun (500, new MethodEvent (LoadArea));
						//LoadArea ();
					}
				} else if (res.Faction != GetObject (bufferId).Faction) {
					// something on my path, attack it
					Logger.Debug ("Simulator", "Shift", "Found entity " + res.ToString () + " at " + endX.ToString () + "," + endY.ToString ());
					//events.WaitFor (500); // TODO movement animation end
					res.skills[0].OnTarget(res, GetObjectsAt(endX, endY).Find(x => x.Faction != "NUTRAL"));
					//this.Attack (oid, bufferId, 1, Simulator.DAMAGE_TYPE_PHYSICAL);
				}
				//Logger.Debug ("Simulator", "Shift", "Shifting 4");
			}
		}

		public void ShowGrid(bool show) {
			world.worldView.EnableGrid (show);
		}

		/// <summary>
		/// Tries to apply the skill to the target.
		/// </summary>
		/// <returns><c>true</c>, if skill was applied, <c>false</c> otherwise.</returns>
		/// <param name="skill">Skill.</param>
		/// <param name="actor">Actor.</param>
		/// <param name="target">Target, if null the target will be actor</param>
		/// <param name="gx">Grid x, if < 0 the target will be target</param>
		/// <param name="gy">Grid y, if < 0 the target will be target</param>
		public bool TrySkill(Skill skill, Entity actor, Entity target = null, int gx = -1, int gy = -1) {
			if (skill.RoundsToGo < 1) {
				if (gx >= 0 && gy >= 0) {
					skill.OnEmpty (actor, gx, gy);
				} else if (target != null) {
					Logger.Debug ("Simulator", "TrySkill", "Trying skill " + skill.Name + " on " + target.ID);
					skill.OnTarget (actor, target);
					target.DamageBar.Level = 1f - (float)target.Health / (float)target.MaxHealth;
					IoManager.AddWidget (target.OutIcon, TARGET_ICON_ID);
				} else {
					skill.OnSelf (actor);
				}
				skill.RoundsToGo = skill.CoolDown;
				if (skill.DamageBar != null)
					skill.DamageBar.Level = (float)skill.RoundsToGo / (float)skill.CoolDown;
				return true;
			} else {
				return false;
			}
		}

		public void ToggleGrid() {
			world.worldView.ToggleGrid ();
		}
	}
}

