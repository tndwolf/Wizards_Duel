﻿// Wizard's Duel, a procedural tactical RPG
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

