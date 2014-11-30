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

namespace WizardsDuel.Game
{
	public class Simulator
	{
		public const string PLAYER_ID = "Player";
		protected int moveSpeed = 500; //millis
		protected int cellHeight = 1;
		protected int cellWidth = 1;

		World world = new World();
		EventDispatcher events;

		public Simulator(out WorldView worldView, bool initUserEvents = true) {
			//*
			this.world = GameFactory.LoadGame("Data/Test.xml");
			worldView = this.world.worldView;
			this.cellWidth = worldView.CellWidth;
			this.cellHeight = worldView.CellHeight;
			// by default always create the player object, which is indestructible
			this.CreateObject (PLAYER_ID, "bp_ezekiel");
			this.Move (PLAYER_ID, 10, 4);
			worldView.ReferenceObject = this.world.entities [PLAYER_ID].OutObject;
			//*/

			this.events = new EventDispatcher(this);
			if (initUserEvents) {
				this.events.AppendEvent (new UserAiEvent (this.events));
			}

			foreach (var r in worldView.dungeon) {
				Logger.Info ("Simulator", "Simulator", "r: " + r + " (" + r.Length.ToString() + ")");
			}
		}

		public void AddEvent(Event evt) {
			this.events.AppendEvent (evt);
		}

		public void AddLight(float x, float y, float radius, Color color) {
			this.world.worldView.LightLayer.AddLight (x, y, radius, color);
		}

		public bool CanMove(string oid, int x, int y, bool moveIfPossible = false) {
			Entity en;
			bool res = false;
			if (this.world.entities.TryGetValue (oid, out en)) {
				var tile = this.world.GetTile(x, y);
				res = tile.template.IsWalkable;
				if (moveIfPossible && res) {
					Move (oid, x, y);
				}
			}
			return res;
		}

		public bool CanShift(string oid, int dx, int dy, bool shiftIfPossible = false) {
			Entity en;
			bool res = false;
			try {
				if (this.world.entities.TryGetValue (oid, out en) && this.world.IsValid(en.X + dx, en.Y + dy)) {
					var tile = this.world.GetTile(en.X + dx, en.Y + dy);
					res = tile.template.IsWalkable;
					if (shiftIfPossible && res) {
						Shift (oid, dx, dy);
					}
				}
			} catch {
			}
			return res;
		}

		public void Cast(string oid) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				if (res.OutObject.IsAnimating) {
					return;
				};
				res.OutObject.SetAnimation ("CAST1");
				CreateParticle (res.X, res.Y);
			}
		}

		public void CreateObject(string oid, string templateId) {
			var newEntity = GameFactory.LoadFromTemplate (templateId);
			this.world.worldView.ObjectsLayer.AddObject (newEntity.OutObject);
			this.world.entities.Add (oid, newEntity);
		}

		public void CreateParticle(int gx, int gy) {
			var ps = new ParticleSystem ();
			var pos = new Vector2f ((gx+1) * this.cellWidth, gy * this.cellHeight - this.cellHeight/4);
			ps.TTL = 3000;
			ps.CreateParticleEffect (this.world.worldView.ObjectsLayer, pos);
			IO.AddWidget (ps);
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

		public void Move(string oid, int gx, int gy) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				res.X = gx;
				res.Y = gy;
				// XXX The X is shifted by 1 on the right to offset the padding
				// of the "Layers" of the WorldView
				res.OutObject.SetPosition ((res.X+1) * this.cellWidth, res.Y * this.cellHeight - this.cellHeight/4);
			}
		}

		public void SetUserEvent(Event evt) {
			this.events.SetUserEvent (evt);
		}

		public void Shift(string oid, int dx, int dy) {
			Entity res;
			if (this.world.entities.TryGetValue (oid, out res)) {
				if (res.OutObject.IsAnimating) {
					return;
				};
				res.X += dx;
				res.Y += dy;
				var ta = new TranslateAnimation (res.OutObject.GetAnimationLength("SHIFT"), dx * this.cellWidth, dy * this.cellHeight);
				res.OutObject.AddAnimator (ta);

				Logger.Debug ("Simulator", "Shift", "Moving to " + res.X.ToString() + "," + res.Y.ToString());

				if (dx < 0) {
					res.OutObject.Facing = Io.Facing.LEFT;
				} else if (dx > 0) {
					res.OutObject.Facing = Io.Facing.RIGHT;
				}
				res.OutObject.SetAnimation ("SHIFT");
			}
		}
	}
}

