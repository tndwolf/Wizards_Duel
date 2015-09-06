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
using WizardsDuel.Game;

namespace WizardsDuel.Io
{
	public enum LayerType {
		UNDEFINED,
		FLOOR,
		LIGHTS,
		OBJECTS,
		WALL,
		GRID,
		COUNT
	}

	public class WorldView: Widget, IClickable {
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
			this.HalfWidth = width / 2;
			this.HalfHeight = height / 2;
			this.layerTexture = new RenderTexture ((uint)width, (uint)height);
			this.layerSprite = new Sprite (layerTexture.Texture);

			this.GridWidth = WorldView.SIZE;
			this.GridHeight = WorldView.SIZE;
			this.CellWidth = cellWidth;
			this.CellHeight = cellHeight;
			this.CellObjectOffset = -cellHeight / 4;
			this.Scale = scale; // XXX init AFTER layerSprite
		}

		public int CellHeight { get; protected set; }

		public int CellObjectOffset { get; set; }

		public int CellWidth { get; protected set; }

		override public void Draw(RenderTarget target, RenderStates states) {
			states.Transform.Translate(this.Position);
			if (this.referenceObject != null) {
				this.referenceObject.Animate ();
				this.referenceCenter.X = this.HalfWidth -this.referenceObject.CenterX;
				this.referenceCenter.Y = this.HalfHeight -this.referenceObject.CenterY;
				foreach (var layer in this.layers) {
					layer.SetCenter (this.referenceCenter);
					target.Draw (layer, states);
				}
			} else {
				foreach (var layer in this.layers) {
					target.Draw (layer, states);
				}
			}

			if (this.ShowGuides) {
				var rect = new RectangleShape (new Vector2f (32, 32));
				rect.FillColor = Color.Transparent;
				rect.OutlineThickness = 0.5f;
				rect.OutlineColor = Color.Cyan;
				for (var y = 0; y < this.HalfHeight*2; y += 32) {
					for (var x = 0; x < this.HalfWidth*2; x += 32) {
						rect.Position = new Vector2f (x, y);
						target.Draw (rect, states);
					}
				}
				rect.Size = new Vector2f (128, 128);
				rect.OutlineColor = Color.Blue;
				for (var y = 0; y < this.HalfHeight*2; y += 128) {
					for (var x = 0; x < this.HalfWidth*2; x += 128) {
						rect.Position = new Vector2f (x, y);
						target.Draw (rect, states);
					}
				}
			}
		}

		public void EnableGrid(bool enable) {
			try {
				this.layers[this.gridLayer].Enabled = enable;
			} catch (Exception ex) {
				Logger.Warning("WorldView", "EnableGrid", ex.ToString());
			}
		}

		public int GridHeight { get; set; }

		public int GridWidth { get; set; }

		protected int HalfHeight { get; set; }

		protected int HalfWidth { get; set; }

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

		public bool ShowGuides { get; set; }

		public void ToggleGrid() {
			try {
				this.layers[this.gridLayer].Enabled = (this.layers[this.gridLayer].Enabled == true) ? false : true;
			} catch (Exception ex) {
				Logger.Warning("WorldView", "ToggleGrid", ex.ToString());
			}
		}

		#region layer management
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

		public TiledLayer FloorLayer {
			get { try { return (TiledLayer)this.layers [this.floorLayer]; } catch { return null; } }
		}

		public LightLayer LightLayer {
			get { try { return (LightLayer)this.layers [this.lightsLayer]; } catch { return null; } }
		}

		public GridLayer GridLayer {
			get { try { return (GridLayer)this.layers [this.gridLayer]; } catch { return null; } }
		}

		public ObjectsLayer ObjectsLayer {
			get { try { return (ObjectsLayer)this.layers [this.objectsLayer]; } catch { return null; } }
		}

		public TiledLayer WallLayer {
			get { try { return (TiledLayer)this.layers [this.wallLayer]; } catch { return null; } }
		}
		#endregion

		#region IClickable implementation
		public void OnMouseMove (object sender, MouseMoveEventArgs e) {
			return;
		}

		public void OnMousePressed (object sender, MouseButtonEventArgs e) {
			var gx = (int)(this.referenceObject.CenterX - this.HalfWidth + e.X) / this.CellWidth;
			var gy = (int)(this.referenceObject.CenterY - this.HalfHeight + e.Y) / this.CellHeight;
			var gl = this.layers [this.gridLayer] as GridLayer;
 			gl.Selected = new Vector2i (gx, gy);
			if (e.Button == Mouse.Button.Right) {
				Simulator.Instance.Select (gx, gy);
			} else {
				Simulator.Instance.SetUserEvent (new ClickEvent (Simulator.PLAYER_ID, gx, gy));
			}
		}

		public void OnMouseReleased (object sender, MouseButtonEventArgs e) {
			return;
		}

		public bool Enabled {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public Vector2f OffsetPosition {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}

		#endregion

		#region rule management
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
			public int maxX = 0;
			public int maxY = 0;
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
			virtual public int[] GetQuad(int x, int y) {
				return new int[] {
					//this.x[x % this.x.Length],
					(this.x[y % this.x.Length] + x * this.w) /*% this.maxX*/,
					(this.y[x % this.y.Length] + y * this.h) /*% this.maxY*/,
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
			virtual public int GetX(int x, int y) {
				return this.x [y % this.x.Length];
			}

			/// <summary>
			/// Gets the y coordinate of the tile sprite in pixel.
			/// Useful because the rule can represent "blocks" of tiles in a
			/// repeating pattern
			/// </summary>
			/// <returns>The y.</returns>
			/// <param name="y">The y coordinate on the map.</param>
			virtual public int GetY(int x, int y) {
				return this.y [y % this.y.Length];
			}

			public override string ToString() {
				return String.Format(
					"<tile x=\"{0}\" y=\"{1}\" maxX=\"{6}\" maxX=\"{7}\" dx=\"{2}\" dy=\"{3}\" w=\"{4}\" h=\"{5}\"/>", 
					string.Join(" ",x), 
					string.Join(" ",y), 
					dx, 
					dy, 
					w, 
					h,
					maxX,
					maxY );
			}
		}

		public class RowRule: Rule {
			/// <summary>
			/// Gets the quad represented by the rule at x,y coordinate on the map.
			/// Useful because the rule can represent "blocks" of tiles in a
			/// repeating pattern
			/// </summary>
			/// <returns>The quad.</returns>
			/// <param name="x">The x coordinate on the map.</param>
			/// <param name="y">The y coordinate on the map.</param>
			override public int[] GetQuad(int x, int y) {
				return new int[] {
					//this.x[x % this.x.Length],
					(this.x[y % this.x.Length] + x * this.w) % this.maxX,
					(this.y[x % this.y.Length] + y * this.h) /*% this.maxY*/,
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
			override public int GetX(int x, int y) {
				return (this.x [y % this.x.Length] + x * this.w) % this.maxX;
			}

			/// <summary>
			/// Gets the y coordinate of the tile sprite in pixel.
			/// Useful because the rule can represent "blocks" of tiles in a
			/// repeating pattern
			/// </summary>
			/// <returns>The y.</returns>
			/// <param name="y">The y coordinate on the map.</param>
			override public int GetY(int x, int y) {
				return this.y [y % this.y.Length];// + y * this.h;
			}

			public override string ToString() {
				return String.Format(
					"<tile type=\"ROW\" x=\"{0}\" y=\"{1}\" maxX=\"{6}\" maxX=\"{7}\" dx=\"{2}\" dy=\"{3}\" w=\"{4}\" h=\"{5}\"/>", 
					string.Join(" ",x), 
					string.Join(" ",y), 
					dx, 
					dy, 
					w, 
					h,
					maxX,
					maxY );
			}
		}

		public Dictionary<string, List<Rule>> ruleset = new Dictionary<string, List<Rule>> ();

		public void AddRule (Rule rule, string layerName) {
			if (this.ruleset.ContainsKey (layerName) == false) {
				var rules = new List<Rule> ();
				this.ruleset.Add (layerName, rules);
			}
			this.ruleset [layerName].Add (rule);
			Logger.Info("WorldView", "AddRule", "Adding Rule: " + rule.ToString() + " to layer " + layerName);
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
										layer.SetTile(x, y, rule.GetX(x, y), rule.GetY(x, y), rule.w, rule.h, rule.dx, rule.dy);
										break;
									}
								}
							}
							var glayer = (GridLayer)this.layers[this.gridLayer];
							glayer.SetTile(x, y, this.dungeon[y][x] == '.');
						} catch (Exception ex) {
							Logger.Debug("WorldView", "SetDungeon", ex.ToString());
						}
					}
				}
			} catch (Exception ex) {
				Logger.Debug("WorldView", "SetDungeon", ex.ToString());
			}
		}
		#endregion
	}
}
