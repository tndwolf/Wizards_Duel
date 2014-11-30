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
using SFML.Graphics;
using SFML.Window;
using WizardsDuel.Utils;

namespace WizardsDuel.Io
{
	public class WorldView: Widget {
		public class Rule {
			public struct Condition {
				public int dx;
				public int dy;
				public char value;

				public override string ToString() {
					return String.Format("<condition dx=\"{0}\" dy=\"{1}\" value=\"{2}\"/>", dx, dy, value);
				}
			}

			public int[] x = {0};
			public int[] y = {0};
			public int dx = 0;
			public int dy = 0;
			public int w = 0;
			public int h = 0;
			public List<Condition> conditions = new List<Condition>();

			public bool Test(int x, int y, string[] map) {
				// test all the conditions, as soon as something does not match
				// return false
				foreach (var condition in this.conditions) {
					try {
						if (map[y+condition.dy][x+condition.dx] != condition.value)
							return false;
					} catch {
						return false;
					}
				}
				// everything matches, return true
				return true;
			}

			/// <summary>
			/// Gets the quad represented by the rule at x,y coordinate on the map.
			/// Useful because the rule can represent "blocks" of tiles in a
			/// repeating pattern
			/// </summary>
			/// <returns>The quad.</returns>
			/// <param name="x">The x coordinate on the map.</param>
			/// <param name="y">The y coordinate on the map.</param>
			public int[] GetQuad(int x, int y) {
				return new int[] {
					this.x[x % this.x.Length],
					this.y[y % this.y.Length],
					this.w,
					this.h
				};
			}

			/// <summary>
			/// Gets the x coordinate of the tile sprite in pixel.
			/// Useful because the rule can represent "blocks" of tiles in a
			/// repeating pattern
			/// </summary>
			/// <returns>The x.</returns>
			/// <param name="x">The x coordinate on the map.</param>
			public int GetX(int x) {
				return this.x [x % this.x.Length];
			}

			/// <summary>
			/// Gets the y coordinate of the tile sprite in pixel.
			/// Useful because the rule can represent "blocks" of tiles in a
			/// repeating pattern
			/// </summary>
			/// <returns>The y.</returns>
			/// <param name="y">The y coordinate on the map.</param>
			public int GetY(int y) {
				return this.y [y % this.y.Length];
			}

			public override string ToString() {
				return String.Format("<tile x=\"{0}\" y=\"{1}\" dx=\"{2}\" dy=\"{3}\" w=\"{4}\" h=\"{5}\"/>", x, y, dx, dy, w, h);
			}
		}
		public Dictionary<string, List<Rule>> ruleset = new Dictionary<string, List<Rule>> ();

		public List<Layer> layers = new List<Layer>();
		private Dictionary<string, int> namedLayers = new Dictionary<string, int> ();
		private int objectsLayer = -1;
		private int lightsLayer = -1;
		private int floorLayer = -1;
		private int wallLayer = -1;
		private int gridLayer = -1;
		private RenderTexture layerTexture;
		private Sprite layerSprite;
		public const string UNNAMED_LAYER = "UNNAMED";

		private const int SIZE = 24;
		public string[] dungeon = {}; 
		private OutObject referenceObject = null;
		private Vector2f referenceCenter = new Vector2f (0f, 0f);

		public WorldView (int width=800, int height=480, int cellWidth=32, int cellHeight=32, float scale=1f) {
			this.Width = width;
			this.Height = height;
			int w = (int)this.Width;
			int h = (int)this.Height;
			this.layerTexture = new RenderTexture ((uint)this.Width, (uint)this.Height);
			this.layerSprite = new Sprite (layerTexture.Texture);

			this.GridWidth = WorldView.SIZE;
			this.GridHeight = WorldView.SIZE;
			this.CellWidth = cellWidth;
			this.CellHeight = cellHeight;
			this.Scale = scale; // XXX init AFTER layerSprite
		}

		public void AddLayer(Layer l, LayerType type = LayerType.UNDEFINED) {
			if (l.GetType () == typeof(ObjectsLayer)) {
				this.objectsLayer = this.layers.Count;
			} else if (l.GetType () == typeof(LightLayer)) {
				this.lightsLayer = this.layers.Count;
			} else if (l.GetType () == typeof(GridLayer)) {
				this.gridLayer = this.layers.Count;
			}
			// XXX the following two can probably be deleted
			// Use AddLayer (named) instead
			else if (type == LayerType.FLOOR) {
				this.floorLayer = this.layers.Count;
				this.namedLayers ["FLOOR"] = this.layers.Count;
			} else if (type == LayerType.WALL) {
				this.wallLayer = this.layers.Count;
				this.namedLayers ["WALL"] = this.layers.Count;
			}
			Logger.Info("WorldView", "AddLayer", "Added layer: " + type.ToString());
			this.layers.Add (l);
		}

		public void AddLayer(Layer l, string name) {
			if (name != WorldView.UNNAMED_LAYER) {
				this.namedLayers [name] = this.layers.Count;
			}
			Logger.Info("WorldView", "AddLayer", "Added named layer: " + name);
			this.layers.Add (l);
		}

		public void AddRule (Rule rule, string layerName) {
			if (this.ruleset.ContainsKey (layerName) == false) {
				var rules = new List<Rule> ();
				this.ruleset.Add (layerName, rules);
			}
			this.ruleset [layerName].Add (rule);
			Logger.Info("WorldView", "AddRule", "Adding Rule: " + rule.ToString() + " to layer " + layerName);
		}

		public int CellHeight { get; protected set; }

		public int CellWidth { get; protected set; }

		override public void Draw(RenderTarget target) {
			layerTexture.Clear (Color.Black);
			if (this.referenceObject != null) {
				this.referenceObject.Animate ();
				this.referenceCenter.X = this.referenceObject.CenterX - this.Width / 2;
				this.referenceCenter.Y = this.referenceObject.CenterY - this.Height / 2;
				foreach (var layer in this.layers) {
					layer.SetCenter (this.referenceCenter);
					layer.Draw (layerTexture);
				}
			} else {
				foreach (var layer in this.layers) {
					layer.Draw (layerTexture);
				}
			}
			layerTexture.Display ();
			target.Draw (this.layerSprite);
		}

		public void EnableGrid(bool enable) {
			try {
				this.layers[this.gridLayer].Enabled = enable;
			} catch (Exception ex) {
				Logger.Warning("WorldView", "EnableGrid", ex.ToString());
			}
		}

		public TiledLayer FloorLayer {
			get { try { return (TiledLayer)this.layers [this.floorLayer]; } catch { return null; } }
		}

		public int GridHeight { get; set; }

		public int GridWidth { get; set; }

		public LightLayer LightLayer {
			get { try { return (LightLayer)this.layers [this.lightsLayer]; } catch { return null; } }
		}

		public ObjectsLayer ObjectsLayer {
			get { try { return (ObjectsLayer)this.layers [this.objectsLayer]; } catch { return null; } }
		}

		public OutObject ReferenceObject {
			set {
				this.referenceObject = value;
			}
		}

		public float Scale {
			set {
				this.layerSprite.Scale = new Vector2f (value, value);
			}
		}

		public void SetDungeon(string[] dungeon) {
			try {
				// Setup new grid
				this.GridHeight = dungeon.Length;
				this.GridWidth = dungeon[0].Length;
				foreach (var layer in this.layers) {
					if (layer is TiledLayer) {
						((TiledLayer)layer).SetSize(this.GridWidth, this.GridHeight);
					}
				}
				// Setup dungeon
				this.dungeon = dungeon;
				for (int y = 0; y < GridHeight; y++) {
					for (int x = 0; x < GridWidth; x++) {
						try {
							//Logger.Debug("WorldView", "SetDungeon2", "Processing " + x.ToString() + "," + y.ToString());
							foreach (var pair in this.namedLayers) {
								var ruleset = this.ruleset[pair.Key];
								var layer = (TiledLayer)this.layers[pair.Value];
								for (var r = 1; r < ruleset.Count; r++) {
									var rule = ruleset[r];
									if (rule.Test (x, y, this.dungeon) == true) {
										layer.SetTile(x, y, rule.GetX(x), rule.GetY(y), rule.w, rule.h, rule.dx, rule.dy);
										break;
									}
								}
							}
							var glayer = (GridLayer)this.layers[this.gridLayer];
							glayer.SetTile(x, y, this.dungeon[y][x] == '.');
						} catch (Exception ex) {
							Logger.Debug("WorldView", "SetDungeon2", ex.ToString());
						}
					}
				}
			} catch (Exception ex) {
				Logger.Debug("WorldView", "SetDungeon2", ex.ToString());
			}
		}

		public TiledLayer WallLayer {
			get { try { return (TiledLayer)this.layers [this.wallLayer]; } catch { return null; } }
		}
	}
}
