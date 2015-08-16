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
			this.AI = new AreaAI ();
		}

		private AreaAI _AI;
		/// <summary>
		/// Gets or sets the Artificial Intelligence Controlling the object.
		/// By default the object is inert.
		/// </summary>
		/// <value>AI</value>
		public AreaAI AI {
			get { return this._AI; }
			set { value.Parent = this; this._AI = value; }
		}

		public void CalculateFoV(int cx, int cy, int sightRadius, int updateRadius = 10) {
			/*int x0 = cx - updateRadius;
			int x1 = cx + updateRadius - 1;
			for (int y = cy - updateRadius; y < cy + updateRadius; y++) {
				if (IsValid(x0,y)) CalculateLoS(cx, cy, x0, y, sightRadius);
				if (IsValid(x1,y)) CalculateLoS(cx, cy, x1, y, sightRadius);
			}
			int y0 = cy - updateRadius;
			int y1 = cy + updateRadius - 1;
			for (int x = cx - updateRadius; x < cx + updateRadius; x++) {
				if (IsValid(x,y0)) CalculateLoS(cx, cy, x, y0, sightRadius);
				if (IsValid(x,y1)) CalculateLoS(cx, cy, x, y1, sightRadius);
			}*/

			/*for (int y = cy - updateRadius; y < cy + updateRadius; y++) {
				for (int x = cx - updateRadius; x < cx + updateRadius; x++) {
					if (IsValid(x,y)) CalculateLoS(cx, cy, x, y, sightRadius);
				}
			}*/
		}

		public void CalculateLoS(int x0, int y0, int x1, int y1, int maxRadius = 10) {
			var dx = x0 - x1;
			var dy = y0 - y1;
			var error = 0.0;
			var de = (dx == 0) ? 100000.0 : Math.Abs(dy / dx);
			dy = Math.Sign(dy); // delta no longer useful in itself, update with +1/-1 for movement
			int y = y0; 
			Tile cell;
			bool isBlocking = false;
			int calculatedCount = 0;
			if (x0 <= x1) {
				for (int x = x0; x < x1; x++) {
					cell = this.GetTile(x, y);
					cell.InLos = !isBlocking;
					this.worldView.GridLayer.SetInLos(x, y, cell.InLos);
					if (!cell.IsExplored && cell.InLos) cell.IsExplored = true;
					if (cell.Template.IsSolid || ++calculatedCount > maxRadius) isBlocking = true;
					error += de;
					while (error >= 0.5) {
						y += dy;
						error -= 1.0;
						cell = this.GetTile(x, y);
						cell.InLos = !isBlocking;
						this.worldView.GridLayer.SetInLos(x, y, cell.InLos);
						if (!cell.IsExplored && cell.InLos) cell.IsExplored = true;
						if (cell.Template.IsSolid || ++calculatedCount > maxRadius) isBlocking = true;
					}
				}
			} else {
				for (int x = x0; x > x1; x--) {
					cell = this.GetTile(x, y);
					cell.InLos = !isBlocking;
					this.worldView.GridLayer.SetInLos(x, y, cell.InLos);
					if (!cell.IsExplored && cell.InLos) cell.IsExplored = true;
					if (cell.Template.IsSolid || ++calculatedCount > maxRadius) isBlocking = true;
					error += de;
					while (error >= 0.5) {
						y += dy;
						error -= 1.0;
						cell = this.GetTile(x, y);
						cell.InLos = !isBlocking;
						this.worldView.GridLayer.SetInLos(x, y, cell.InLos);
						if (!cell.IsExplored && cell.InLos) cell.IsExplored = true;
						if (cell.Template.IsSolid || ++calculatedCount > maxRadius) isBlocking = true;
					}
				}
			}
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

		/// <summary>
		/// Returns a list of objects at x, y if any
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		public List<Entity> GetObjectsAt(int x, int y) {
			var res = new List<Entity>();
			foreach (var entity in this.entities) {
				if (entity.Value.X == x && entity.Value.Y == y) {
					res.Add(entity.Value);
				}
			}
			return res;
		}

		public Tile GetTile(int x, int y) {
			try {
				return this.map [y, x];
			} catch {
				Logger.Debug ("World", "GetTile", "Out of bounds " + x.ToString() + "," + y.ToString());
				return null;
			}
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
			return (x >= 0 && x < this.GridWidth && y >= 0 && y < this.GridHeight);
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
				for (int y = 0; y < this.GridHeight; y++) {
					for (int x = 0; x < this.GridWidth; x++) {
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
				for (int y = 0; y < this.GridHeight; y++) {
					for (int x = 0; x < this.GridWidth; x++) {
						var tile = new Tile();
						tile.InLos = false;
						tile.IsExplored = false;
						try {
							if (this.tiles.TryGetValue(dungeon[x,y], out tile.Template) == false) {
								tile.Template = this.tiles[DEFAULT_TILE_ID];
							}
							linearDungeon[y] += dungeon[x,y];
						}
						catch {
							tile.Template = this.tiles[DEFAULT_TILE_ID];
							linearDungeon[y] += ".";
						}
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
			//return;
			//5-5 7-7
			this.AI.onRound();
			this.HasEnded = true;
			this.Initiative += Simulator.ROUND_LENGTH;
		}

		public bool HasEnded { get; set; }

		public int Initiative { get; set; }

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

