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
		private int createdEntityCount = 0;
		public EventDispatcher events;
		private static Simulator instance;
		private Random rng = new Random ();
		private World world = new World();

		public const string BLOOD_PARTICLE = "P_BLEED";
		public const string DEFAULT_DATA = "Data/Test.xml";
		public const string DEFAULT_LEVEL = "Data/TestLevel.xml";
		public const string PLAYER_ID = "Player";

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

			this.events = new EventDispatcher(this);

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
			this.events.AppendEvent(new AttackEvent(GetObject(attackerId), GetObject(targetId)));
		}

		public void Bleed(Entity target) {
			CreateParticleOn (BLOOD_PARTICLE, target);
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
						CreateParticleOn ("p_truce", PLAYER_ID);
					} else {
						CreateParticleOn ("p_hurt", targetId);
					}
				} else {
					CreateObject (this.createdEntityCount.ToString(), "bp_firefly", gx, gy);
					//CreateParticleAt ("p_lava", gx, gy);
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

		public void CreateObject(string oid, string templateId, int gx=0, int gy=0) {
			var newEntity = GameFactory.LoadFromTemplate (templateId);
			if (newEntity != null) {
				this.world.worldView.ObjectsLayer.AddObject (newEntity.OutObject);
				this.world.entities.Add (oid, newEntity);
				Move (oid, gx, gy);
				if (oid == PLAYER_ID) {
					this.events.AppendEvent (new UserEvent (this.events));
				} else {
					CreateParticleOn ("P_SPAWN", newEntity);
					//this.events.AppendEvent (new AiEvent (oid, 10));
				}
				createdEntityCount++;
			} else {
				Logger.Warning ("Simulator", "CreateObject", "cannot create " + oid + " " + templateId);
			}
		}

		public void CreateParticleAt(string pid, int gx, int gy) {
			var ps = GameFactory.LoadParticleFromTemplate (pid, gx * this.CellWidth, gy * this.CellHeight, this.world.worldView.ObjectsLayer);
			ps.LightsLayer = this.world.worldView.LightLayer;
			IoManager.AddWidget (ps);
			Logger.Debug ("Simulator", "CreateParticle", ps.ToString());
		}

		public void CreateParticleOn(string pid, Entity target) {
			var flip = (target.OutObject.Facing == Facing.RIGHT) ? false : true;
			var ps = GameFactory.LoadParticleFromTemplate (pid, 0f, 0f, this.world.worldView.ObjectsLayer, flip);
			ps.LightsLayer = this.world.worldView.LightLayer;
			target.OutObject.AddParticleSystem (ps);
			IoManager.AddWidget (ps);
		}

		public void CreateParticleOn(string pid, string oid) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				var ps = GameFactory.LoadParticleFromTemplate (pid, 0f, 0f, this.world.worldView.ObjectsLayer);
				ps.LightsLayer = this.world.worldView.LightLayer;
				res.OutObject.AddParticleSystem (ps);
				IoManager.AddWidget (ps);
			}
		}

		public void DestroyObject(string oid) {
			Entity res;
			if (oid != PLAYER_ID && this.world.entities.TryGetValue (oid, out res)) {
				this.world.worldView.ObjectsLayer.DeleteObject (res.OutObject);
				this.world.entities.Remove (oid);
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

		public void LoadArea() {
			// by default always create the player object, which is indestructible
			this.CreateObject (PLAYER_ID, "bp_ezekiel");
			this.Move (PLAYER_ID, 10, 3);//10,4
			this.world.worldView.ReferenceObject = this.world.entities [PLAYER_ID].OutObject;

			//this.CreateObject ("monster1", "bp_firefly");
			//this.Move ("monster1", 2, 2);//12,4

			var wf = new WorldFactory ();
			wf.Initialize (DEFAULT_LEVEL);
			//wf.Generate ();
			this.world.SetMap (wf.Generate ());

			this.events.AppendEvent (new AreaAiEvent (10));

			var done = false;
			do {
				var x = Random(world.GridWidth);
				var y = Random(world.GridHeight);
				if (world.GetTile(x,y).Template.IsWalkable == true) {
					done = true;
					this.Move (PLAYER_ID, x, y);
				}
			} while(done != true);

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
				res.OutObject.Position = new Vector2f ((res.X + 0.5f) * this.CellWidth, (res.Y + 0.5f) * this.CellHeight + this.CellObjectOffset);
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

		public void SetUserEvent(Event evt) {
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
				var bufferId = this.world.GetObjectAt (endX, endY);
				if (bufferId == null) {
					// nothing in the way, move around
					res.X = endX;
					res.Y = endY;

					var ta = new TranslateAnimation (res.OutObject.GetAnimationLength ("SHIFT"), dx * this.CellWidth, dy * this.CellHeight);
					res.OutObject.AddAnimator (ta);

					Logger.Debug ("Simulator", "Shift", "Shifting 3 " + ta.deltaX.ToString () + "," + ta.deltaY.ToString ());
					Logger.Debug ("Simulator", "Shift", "Moving to " + endX.ToString () + "," + endY.ToString ());

					if (dx < 0) {
						res.OutObject.Facing = Io.Facing.LEFT;
					} else if (dx > 0) {
						res.OutObject.Facing = Io.Facing.RIGHT;
					}
					res.OutObject.SetAnimation ("SHIFT");
				} else {
					// something on my path, attack it
					Logger.Debug ("Simulator", "Shift", "Found entity " + res.ToString() + " at " + endX.ToString () + "," + endY.ToString ());
					//events.WaitFor (500); // TODO movement animation end
					this.Attack (oid, bufferId);
				}
				//Logger.Debug ("Simulator", "Shift", "Shifting 4");
			}
		}
	}
}

