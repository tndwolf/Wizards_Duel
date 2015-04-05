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
using SFML.Graphics;
using SFML.Window;

namespace WizardsDuel.Game
{
	public class Entity: EventObject {
		public string DeathAnimation = String.Empty;
		public Color DeathMain = Color.Red;
		public IntRect DeathRect = new IntRect (0, 0, 1, 1);
		public Color DeathSecundary = Color.Black;
		public WizardsDuel.Io.OutObject OutObject = null;
		public Dictionary<string, int> Vars = new Dictionary<string, int>();
		public int X = 0;
		public int Y = 0;

		public Entity(string id, string templateId = "") {
			this.ID = id;
			this.TemplateID = templateId;
		}

		public bool Dressing {
			get;
			set;
		}

		public string Faction {
			get;
			set;
		}

		public int GetVar(string name, int def=0) {
			int res;
			if (this.Vars.TryGetValue (name, out res) == true) {
				return res;
			} else {
				return def;
			}
		}

		public string ID {
			get;
			protected set;
		}

		public void SetVar(string name, int value) {
			this.Vars[name] = value;
		}

		public bool Static {
			get;
			set;
		}

		public string TemplateID {
			get;
			protected set;
		}

		#region EventObject implementation
		public void Run (Simulator sim, EventManager ed) {
			if (this.Dressing || this.Static) {
				this.HasStarted = true;
				this.HasEnded = true;
			} else if (this.ID == Simulator.PLAYER_ID) {
				Logger.Debug ("Entity", "Run", "Running PLAYER event");
				if (ed.RunUserEvent () == true) {
					this.HasStarted = true;
					this.HasEnded = true;
				} else {
					this.HasStarted = false;
					this.HasEnded = false;
				}
			} else {
				//Logger.Debug ("Entity", "Run", "Running event");
				//sim.CanShift (this.ID, sim.Random (3) - 1, sim.Random (3) - 1, true);
				var player = sim.GetObject (Simulator.PLAYER_ID);
				var dx = Math.Sign(player.X - this.X);
				var dy = Math.Sign(player.Y - this.Y);
				sim.CanShift(this.ID, dx, dy, true);

				this.HasStarted = true;
				this.HasEnded = true;
			}
		}

		bool hasEnded = false;
		public bool HasEnded {
			get { return this.hasEnded && this.OutObject.IsInIdle; }
			set { this.hasEnded = value; }
		}

		public bool HasStarted { get; set; }

		public int Initiative { get; set; }

		public bool IsWaiting { 
			get { 
				if (this.ID == Simulator.PLAYER_ID) {
					return !Simulator.Instance.IsUserEventInQueue ();
				} else {
					return false;
				}
			}
		}

		public int UpdateInitiative () {
			this.Initiative += 10;
			return this.Initiative;
		}

		public int CompareTo (object obj) {
			try {
				var comp = (EventObject) obj;
				return this.Initiative.CompareTo (comp.Initiative);
			} catch (Exception ex) {
				Logger.Debug ("Entity", "CompareTo", "Trying to compare a wrong object" + ex.ToString());
				return 0;
			}
		}
		#endregion
	}

	public class TileTemplate {
		public bool IsSolid { get; set; }
		public bool IsWalkable { get; set; }
	}

	public class Tile {
		public TileTemplate Template;
		public bool InLos { get; set; }
		public bool IsExplored { get; set; }
	}

	public class World : EventObject {
		public const string DEFAULT_TILE_ID = "DEFAULT";

		public Dictionary<string, Entity> entities = new Dictionary<string, Entity>();
		public Tile[,] map;
		public Dictionary<string, TileTemplate> tiles = new Dictionary<string, TileTemplate>();
		public WizardsDuel.Io.WorldView worldView = null;

		public World () {
			var tt = new TileTemplate ();
			this.SetTileTemplate(DEFAULT_TILE_ID, tt);
			tt = new TileTemplate ();
			tt.IsWalkable = true;
			tt.IsSolid = false;
			this.SetTileTemplate(".", tt);
			tt = new TileTemplate ();
			tt.IsWalkable = false;
			tt.IsSolid = true;
			this.SetTileTemplate("#", tt);
		}

		public Vector2i EndCell { get; set; }

		/// <summary>
		/// Returns the ID of the object at x, y if any, null otherwise
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		public string GetObjectAt(int x, int y) {
			foreach (var entity in this.entities) {
				if (entity.Value.X == x && entity.Value.Y == y) {
					return entity.Key;
				}
			}
			return null;
		}

		public string GetObjectAt(int x, int y, out Entity result) {
			foreach (var entity in this.entities) {
				if (entity.Value.X == x && entity.Value.Y == y) {
					result = entity.Value;
					return entity.Key;
				}
			}
			result = null;
			return null;
		}

		public Tile GetTile(int x, int y) {
			return this.map [y, x];
		}

		public int GridHeight { get; set; }

		public int GridWidth { get; set; }

		public bool InLos(int x, int y) {
			return (IsValid (x, y) && this.GetTile (x, y).InLos);
		}

		public bool IsExplored(int x, int y) {
			return (IsValid (x, y) && this.GetTile (x, y).IsExplored);
		}

		public bool IsValid(int x, int y) {
			return (x >= 0 && x < this.GridWidth-1 && y >= 0 && y < this.GridHeight-1);
		}

		public bool IsWalkable(int x, int y) {
			return (IsValid (x, y) && this.GetTile (x, y).Template.IsWalkable);
		}

		public void SetMap (string[] dungeon) {
			try {
				// Setup new grid
				this.GridHeight = dungeon.Length;
				this.GridWidth = dungeon[0].Length;
				this.map = new Tile[this.GridHeight, this.GridWidth];
				for (int y = 0; y < this.GridHeight-1; y++) {
					for (int x = 0; x < this.GridWidth-1; x++) {
						var tile = new Tile();
						tile.InLos = false;
						tile.IsExplored = false;
						if (this.tiles.TryGetValue(dungeon[y][x].ToString(), out tile.Template) == false) {
							tile.Template = this.tiles[DEFAULT_TILE_ID];
						}
						this.SetTile(x, y, tile);
					}
				}
				this.worldView.SetDungeon (dungeon);
			}
			catch (Exception ex) {
				Logger.Warning ("World", "SetMap", ex.ToString());
			}
		}

		public void SetMap (string[,] dungeon) {
			try {
				// Setup new grid
				this.GridHeight = dungeon.GetLength(1);
				this.GridWidth = dungeon.GetLength(0);
				this.map = new Tile[this.GridHeight, this.GridWidth];
				var linearDungeon = new string[this.GridHeight];
				for (int y = 0; y < this.GridHeight-1; y++) {
					for (int x = 0; x < this.GridWidth-1; x++) {
						var tile = new Tile();
						tile.InLos = false;
						tile.IsExplored = false;
						if (this.tiles.TryGetValue(dungeon[x,y], out tile.Template) == false) {
							tile.Template = this.tiles[DEFAULT_TILE_ID];
						}
						linearDungeon[y] += dungeon[x,y];
						this.SetTile(x, y, tile);
					}
				}
				this.worldView.SetDungeon (linearDungeon);
			}
			catch (Exception ex) {
				Logger.Warning ("World", "SetMap", ex.ToString());
			}
		}

		public void SetTile(int x, int y, Tile tile) {
			this.map [y, x] = tile;
		}

		public void SetTileTemplate(string glyph, TileTemplate tt) {
			this.tiles [glyph] = tt;
		}

		public Vector2i StartCell { get; set; }

		#region EventObject implementation
		public void Run (Simulator sim, EventManager ed) {
			//5-5 7-7
			var MAX_ENTITIES = 10;
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
					Logger.Info ("World", "Run", "Created object at " + position.ToString());
				}
			}
			this.HasEnded = true;
		}

		public bool HasEnded { get; set; }

		public bool HasStarted { get; set; }

		public int Initiative { get; set; }

		public bool IsWaiting { get; protected set; }

		public int UpdateInitiative () {
			this.Initiative += 10;
			return this.Initiative;
		}

		public int CompareTo (object obj) {
			try {
				var comp = (EventObject) obj;
				return this.Initiative.CompareTo (comp.Initiative);
			} catch (Exception ex) {
				Logger.Debug ("World", "CompareTo", "Trying to compare a wrong object" + ex.ToString());
				return 0;
			}
		}
		#endregion
	}
}

