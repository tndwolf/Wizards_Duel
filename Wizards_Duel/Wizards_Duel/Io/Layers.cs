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
	/// <summary>
	/// Base class for layers to be used inside Worldviews
	/// </summary>
	public class Layer {
		protected Vector2f center = new Vector2f(0f, 0f);
		protected float scale = 1.0f;

		public Layer() {
			this.Blend = new RenderStates (BlendMode.Alpha);
			this.Enabled = true;
		}

		public Layer(int width, int height) {
			this.Width = width;
			this.Height = height;
			this.Blend = new RenderStates (BlendMode.Alpha);
			this.Enabled = true;
		}

		virtual public bool Enabled {
			get;
			set;
		}

		virtual public RenderStates Blend {
			get;
			set;
		}

		virtual public int Height {
			get;
			set;
		}

		virtual public int Width {
			get;
			set;
		}

		virtual public float Scale {
			get { return this.scale; }
			set { this.scale = value; }
		}

		virtual public void Draw(RenderTarget target) {
			return;
		}

		/// <summary>
		/// Sets the center of the layer. This point will be drawn in the center of the
		/// layer render target
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		virtual public void SetCenter(float x, float y) {
			this.center.X = x;
			this.center.Y = y;
		}

		/// <summary>
		/// Sets the center of the layer. This point will be drawn in the center of the
		/// layer render target
		/// </summary>
		/// <param name="center">The center vector.</param>
		virtual public void SetCenter(Vector2f center) {
			this.center = center;
		}

		/// <summary>
		/// Gets or sets the center of the layer along the X axis.
		/// </summary>
		/// <value>The x.</value>
		virtual public float X {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the center of the layer along the Y axis.
		/// </summary>
		/// <value>The y.</value>
		virtual public float Y {
			get;
			set;
		}
	}

	/// <summary>
	/// Simple layer containing only one big texture
	/// </summary>
	public class BackgroundLayer: Layer {
		//protected Color color;
		protected Texture bgTexture;
		protected Sprite bgSprite;
		protected Vector2f bgSize;
		protected Vector2i bgRepeat;
		protected Vector2i bgOffset;

		public BackgroundLayer(int width, int height, string texture): base(width, height) {
			this.SetBackground (texture);
			this.Color = Color.White;
		}

		public Color Color {
			get { return this.bgSprite.Color; }
			set { this.bgSprite.Color = value; }
		}

		override public void Draw(RenderTarget target) {
			var bufferPosition = new Vector2f(this.bgOffset.X, this.bgOffset.Y);
			for (var y = 0; y < this.bgRepeat.Y; y++) {
				for (var x = 0; x < this.bgRepeat.X; x++) {
					this.bgSprite.Position = bufferPosition;
					target.Draw (this.bgSprite, this.Blend);
					bufferPosition.X += this.bgSize.X;
				}
				bufferPosition.X = this.bgOffset.X;
				bufferPosition.Y += this.bgSize.Y;
			}
			this.bgSprite.Position = new Vector2f(this.bgOffset.X, this.bgOffset.Y);
		}

		override public float Scale {
			get {
				return base.Scale;
			}
			set {
				base.Scale = value;
				this.bgSprite.Scale = new Vector2f (value, value);
				this.AdjustLayer ();
			}
		}

		/// <summary>
		/// Sets the background.
		/// </summary>
		/// <param name="texture">The file name of the texture.</param>
		virtual public void SetBackground(string texture) {
			this.bgTexture = IO.LoadTexture (texture, true);
			this.bgSprite = new Sprite (this.bgTexture);
			this.AdjustLayer ();
		}

		/// <summary>
		/// Sets the center of the layer. This point will be drawn in the center of the
		/// layer render target
		/// </summary>
		/// <remarks>
		/// If center is negative some clipping may occur, this is in fact intentional
		/// the world view should never move over 0,0 (top, left corner of the map)
		/// </remarks>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		override public void SetCenter(float x, float y) {
			if (!this.Static) {
				base.SetCenter (x, y);
				this.AdjustLayer ();
			}
		}

		/// <summary>
		/// Sets the center of the layer. This point will be drawn in the center of the
		/// layer render target
		/// </summary>
		/// <remarks>
		/// If center is negative some clipping may occur, this is in fat intentional
		/// the world view should never move over 0,0 (top, left corner of the map)
		/// </remarks>
		/// <param name="center">The center vector.</param>
		override public void SetCenter(Vector2f center) {
			if (!this.Static) {
				base.SetCenter (center);
				this.AdjustLayer ();
			}
		}

		public bool Static {
			get;
			set;
		}

		virtual protected void AdjustLayer() {
			this.bgSize.X = this.bgTexture.Size.X * this.Scale;
			this.bgSize.Y = this.bgTexture.Size.Y * this.Scale;
			this.bgRepeat = new Vector2i (
				2 + (int)(this.Width / this.bgSize.X),
				2 + (int)(this.Height / this.bgSize.Y)
			);
			this.bgOffset = new Vector2i (
				-1 * (int)this.center.X % (int)this.bgSize.X,
				-1 * (int)this.center.Y % (int)this.bgSize.Y
			);
		}
	}

	/// <summary>
	/// Special kind of TiledLayer to show the game grid
	/// </summary>
	public class GridLayer: Layer {
		private RenderTexture layerTexture;
		private Sprite layerSprite;
		private bool[,] drawGrid;
		private RectangleShape tile;
		private int gridBorder = 0;
		private int gridPadding = 0;

		private IntRect maskDrawRange = new IntRect(0, 0, 0, 0);
		private Vector2f maskDrawOffset = new Vector2f (0f, 0f);
		private Vector2i cellSize = new Vector2i (1, 1);

		public static readonly Color DEFAULT_FILL = new Color (255, 255, 255, 0);
		public static readonly Color DEFAULT_BORDER = new Color (255, 255, 255, 96);


		public GridLayer(int width, int height, int gridWidth, int gridHeight, int cellWidth, int cellHeight): base(width, height) {
			this.layerTexture = new RenderTexture ((uint)this.Width, (uint)this.Height);
			this.layerSprite = new Sprite (this.layerTexture.Texture);

			this.cellSize.X = (int)(cellWidth * this.Scale);
			this.cellSize.Y = (int)(cellHeight * this.Scale);
			this.GridWidth = gridWidth;
			this.GridHeight = gridHeight;
			this.drawGrid = new bool[gridHeight, gridWidth];

			this.tile = new RectangleShape(new Vector2f(cellWidth, cellHeight));
			this.GridPadding = 2;
			this.GridBorder = 2;
			this.FillColor = GridLayer.DEFAULT_FILL;
			this.OutColor = GridLayer.DEFAULT_BORDER;
			this.AdjustLayer ();
		}

		virtual protected void AdjustLayer() {
			// the subtraction is to take into account one tile less in respect to the top-left edge
			this.maskDrawOffset.X = -((int)(this.center.X) % this.cellSize.X) - this.cellSize.X + 2*this.GridPadding;
			this.maskDrawOffset.Y = -((int)(this.center.Y) % this.cellSize.Y) - this.cellSize.Y + 2*this.GridPadding;
			this.maskDrawRange.Left = (int)(this.center.X / this.cellSize.X) - 1;
			this.maskDrawRange.Top = (int)(this.center.Y / this.cellSize.Y) - 1;
			this.maskDrawRange.Width =  (int)(this.Width / this.cellSize.X) + 2;
			this.maskDrawRange.Height =  (int)(this.Height / this.cellSize.Y) + 2;
		}

		override public void Draw(RenderTarget target) {
			if (this.Enabled) {
				this.layerTexture.Clear (Color.Transparent);
				var bufferPosition = new Vector2f (this.maskDrawOffset.X, this.maskDrawOffset.Y);
				for (var y = this.maskDrawRange.Top; y < this.maskDrawRange.Height + this.maskDrawRange.Top; y++) {
					for (var x = this.maskDrawRange.Left; x < this.maskDrawRange.Width + this.maskDrawRange.Left; x++) {
						try {
							bufferPosition.X += this.cellSize.X;
							if (this.drawGrid [y, x] == true) {
								this.tile.Position = bufferPosition;
								this.layerTexture.Draw (this.tile);
							}
						} catch {

						}
					}
					bufferPosition.X = this.maskDrawOffset.X;
					bufferPosition.Y += this.cellSize.Y;
				}
				this.layerTexture.Display ();
				target.Draw (this.layerSprite, this.Blend);
			}
		}

		public Color FillColor {
			set { this.tile.FillColor = value; }
		}

		public int GridBorder {
			get { return this.gridBorder; }
			set { 
				this.gridBorder = value;
				this.tile.OutlineThickness = value;
				var adjustment = this.GridPadding * 2 + this.gridBorder * 2;
				this.tile.Size = new Vector2f(this.cellSize.X - adjustment, this.cellSize.Y - adjustment);
			}
		}

		public int GridHeight {
			get;
			set;
		}

		public int GridPadding {
			get { return this.gridPadding; }
			set {
				this.gridPadding = value;
				var adjustment = this.gridPadding * 2 + this.GridBorder * 2;
				this.tile.Size = new Vector2f(this.cellSize.X - adjustment, this.cellSize.Y - adjustment);
			}
		}

		public int GridWidth {
			get;
			set;
		}

		public Color OutColor {
			set { this.tile.OutlineColor = value; }
		}

		/// <summary>
		/// Sets the center of the layer. This point will be drawn in the center of the
		/// layer render target
		/// </summary>
		/// <remarks>
		/// If center is negative some clipping may occur, this is in fact intentional
		/// the world view should never move over 0,0 (top, left corner of the map)
		/// </remarks>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		override public void SetCenter(float x, float y) {
			base.SetCenter (x, y);
			this.AdjustLayer ();
		}

		override public void SetCenter(Vector2f center) {
			base.SetCenter (center);
			this.AdjustLayer ();
		}

		public void SetTile(int x, int y, bool draw) {
			try {
				this.drawGrid [y, x] = draw;
			} catch {
				Logger.Warning ("GridLayer", "SetTile", "Out of bounds " + x.ToString() + "," + y.ToString());
			}
		}
	}

	public class Light {
		private Color color = new Color(255, 255, 255);
		private Vector2f position = new Vector2f(0f, 0f);

		public Light(Vector2f center, float radius, Color color) {
			center.X -= radius;
			center.Y -= radius;
			this.color = color;
			this.position = center;
			this.Radius = radius;
		}

		public Color Color {
			get { return this.color; }
			set { this.color = value; }
		}

		public void DrawWithOffset(RenderTarget target, Texture texture, float x, float y) {
			var circle = new CircleShape (this.Radius);
			circle.Texture = texture;
			circle.FillColor = this.Color;
			if (Parent != null) {
				circle.Position = new Vector2f (Parent.CenterX - x - Radius / 2, Parent.CenterY - y - Radius / 2);
				if (Parent.ToBeDeleted) {
					this.Radius /= 1.5f;
				}
			} else {
				circle.Position = new Vector2f (this.position.X - x, this.position.Y - y);
			}
			target.Draw(circle);
		}

		public OutObject Parent { 
			get;
			set;
		}

		public Vector2f Position {
			get { return this.position; }
			set { value.X -= Radius / 2; value.Y -= Radius / 2; this.position = value; }
		}

		public float Radius {
			get;
			set;
		}

		public void SetPosition(float cx, float cy) {
			this.position = new Vector2f (cx - this.Radius / 2, cy - this.Radius / 2);
		}
	}

	/// <summary>
	/// This layer stores point lights and the ambient color.
	/// When drawn it uses a MULTIPLY blend.
	/// </summary>
	public class LightLayer: Layer {
		private List<Light> lights = new List<Light>();
		private RenderTexture layerTexture;
		private Sprite layerSprite;
		private Texture lightTexture; 

		public LightLayer(int width, int height): base(width, height) {
			this.AmbientLight = new Color (255, 255, 255);
			this.layerTexture = new RenderTexture ((uint)this.Width, (uint)this.Height);
			this.layerSprite = new Sprite (this.layerTexture.Texture);
			this.lightTexture = IO.CreateShadedCircle2 ();
			this.Blend = new RenderStates (BlendMode.Add);
		}

		public Light AddLight(float x, float y, float radius, Color color) {
			var light = new Light (new Vector2f (x, y), radius, color);
			this.lights.Add (light);
			return light;
		}

		public Color AmbientLight {
			get;
			set;
		}

		public void DeleteLight(Light light) {
			this.lights.Remove (light);
		}

		override public void Draw(RenderTarget target) {
			this.layerTexture.Clear (this.AmbientLight);
			//var buffPosition = new Vector2f ();
			foreach (var light in this.lights) {
				light.DrawWithOffset (this.layerTexture, this.lightTexture, this.center.X, this.center.Y);
				/*var circle = new CircleShape (light.Radius);
				circle.FillColor = light.Color;
				circle.Texture = this.lightTexture;
				//circle.Position = light.Position;
				//buffPosition.X = light.Position.X + this.center.X;
				//buffPosition.Y = light.Position.Y + this.center.Y;
				circle.Position = buffPosition;
				this.layerTexture.Draw (circle);*/
			}
			this.lights.RemoveAll (x => x.Radius < 2);
			this.layerTexture.Display ();
			//this.lightSprite = new Sprite (this.layerTexture.Texture);
			//this.layerSprite.Scale = this.Scale;
			target.Draw (this.layerSprite, this.Blend);
		}

		override public float Scale {
			get {
				return base.Scale;
			}
			set {
				base.Scale = value;
				this.layerSprite.Scale = new Vector2f (value, value);
			}
		}

		public void SetLightTexture(string texture, int u, int v, int w, int h) {
			this.lightTexture = IO.LoadTexture (texture);
			//TODO
		}
	}

	/// <summary>
	/// This layer has a background which is then masked by "mask tiles".
	/// Mask tiles are black and white with alpha and should be used as such:
	/// 1. White pixels take the color of the background
	/// 2. Black pixels are black
	/// 3. Transparent pixels are not displayed
	/// </summary>
	public class TiledLayer: BackgroundLayer {
		private RenderTexture layerTexture;
		private Sprite layerSprite;
		private Texture tileset;
		private _Tile[,] maskMap;
		private _Tile defaultTile;

		private IntRect maskDrawRange = new IntRect(0, 0, 0, 0);
		private Vector2f maskDrawOffset = new Vector2f (0f, 0f);
		private Vector2i cellSize = new Vector2i (1, 1);
		private Vector2i refCellSize = new Vector2i (0, 0);
		private float tileScale = 1.0f;

		public class _Tile {
			public Sprite tileSprite = null;
			public float dx = 0f;
			public float dy = 0f;
		}

		public TiledLayer(int width, int height, string texture): base(width, height, texture) {
			this.layerTexture = new RenderTexture ((uint)this.Width, (uint)this.Height);
			this.layerSprite = new Sprite (this.layerTexture.Texture);
			this.SetBackground (texture);
			this.MaskBlend = new RenderStates (BlendMode.Alpha);
		}

		override protected void AdjustLayer() {
			if (this.DrawBackground) {
				base.AdjustLayer ();
			}
			// the subtraction is to take into account one tile less in respect to the top-left edge
			this.maskDrawOffset.X = -((int)(this.center.X) % this.cellSize.X) - this.cellSize.X;
			this.maskDrawOffset.Y = -((int)(this.center.Y) % this.cellSize.Y) - this.cellSize.Y;
			this.maskDrawRange.Left = (int)(this.center.X / this.cellSize.X) - 1;
			this.maskDrawRange.Top = (int)(this.center.Y / this.cellSize.Y) - 1;
			this.maskDrawRange.Width =  (int)(this.Width / this.cellSize.X) + 2;
			this.maskDrawRange.Height =  (int)(this.Height / this.cellSize.Y) + 2;
		}

		public RenderStates MaskBlend {
			get;
			set;
		}

		override public void Draw(RenderTarget target) {
			this.layerTexture.Clear (Color.Transparent);
			if (this.DrawBackground) {
				base.Draw (this.layerTexture);
			}
			var bufferPosition = new Vector2f (this.maskDrawOffset.X, this.maskDrawOffset.Y);
			var oldPosition = new Vector2f (0f, 0f);
			for (var y = this.maskDrawRange.Top; y < this.maskDrawRange.Height + this.maskDrawRange.Top; y++) {
				for (var x = this.maskDrawRange.Left; x < this.maskDrawRange.Width + this.maskDrawRange.Left; x++) {
					bufferPosition.X += this.cellSize.X;
					try {
						var t = this.maskMap [y, x];
						bufferPosition.X += t.dx;
						bufferPosition.Y += t.dy;
						t.tileSprite.Position = bufferPosition;
						this.layerTexture.Draw (t.tileSprite, this.MaskBlend);
						bufferPosition.X -= t.dx;
						bufferPosition.Y -= t.dy;
					} catch {
						defaultTile.tileSprite.Position = bufferPosition;
						this.layerTexture.Draw (defaultTile.tileSprite, this.MaskBlend);
						//this.layerTexture.Draw (this.maskMap [0, 0].tileSprite, this.blendMode);
						//Console.WriteLine ("Errore al tile: " + x.ToString() + " " + y.ToString() + " " + this.maskMap.ToString());
					}
				}
				bufferPosition.X = this.maskDrawOffset.X;
				bufferPosition.Y += this.cellSize.Y;
			}
			this.layerTexture.Display ();
			target.Draw (this.layerSprite, this.Blend);
		}

		public bool DrawBackground {
			get;
			set;
		}

		public int GridHeight {
			get;
			set;
		}

		public int GridWidth {
			get;
			set;
		}

		public float TileScale {
			get {
				return this.tileScale;
			}
			set {
				cellSize.X = (int)(value * this.refCellSize.X);
				cellSize.Y = (int)(value * this.refCellSize.Y);
				this.defaultTile.tileSprite.Scale = new Vector2f (value, value);
				this.tileScale = value;
				this.AdjustLayer ();
			}
		}

		override public void SetBackground(string texture) {
			if (texture == "") {
				this.DrawBackground = false;
				this.bgSprite = new Sprite ();
			} else {
				this.bgTexture = IO.LoadTexture (texture, true);
				this.bgSprite = new Sprite (this.bgTexture);
				this.DrawBackground = true;
				this.AdjustLayer ();
			}
		}

		public void SetSize(int gridWidth, int gridHeight) {
			this.GridWidth = gridWidth;
			this.GridHeight = gridHeight;
			maskMap = new _Tile[gridHeight, gridWidth];
			this.AdjustLayer ();
		}
	
		public void SetTilemask(int gridWidth, int gridHeight, int cellWidth, int cellHeight, string texture) {
			this.refCellSize.X = cellWidth;
			this.refCellSize.Y = cellHeight;
			this.cellSize.X = (int)(cellWidth * this.TileScale);
			this.cellSize.Y = (int)(cellHeight * this.TileScale);
			this.GridWidth = gridWidth;
			this.GridHeight = gridHeight;
			maskMap = new _Tile[gridHeight, gridWidth];
			this.tileset = IO.LoadTexture (texture);
			this.AdjustLayer ();
		}

		public void SetTile(int x, int y, int u, int v, int w, int h, int dx = 0, int dy = 0) {
			try {
				var t = new _Tile ();
				t.tileSprite = new Sprite (this.tileset);
				t.tileSprite.TextureRect = new IntRect (u, v, w, h);
				t.tileSprite.Scale = new Vector2f (this.TileScale, this.TileScale);
				// CellWidth and CellHeight are already scaled
				t.tileSprite.Position = new Vector2f (
					//x * this.cellSize.X + dx * this.TileScale,
					//y * this.cellSize.Y + dy * this.TileScale
					dx * this.TileScale,
					dy * this.TileScale
				); // XXX no longer useful
				t.dx = dx;
				t.dy = dy;
				this.maskMap [y, x] = t;
			} catch {
				Logger.Warning ("TiledLayer", "SetTile", "Out of bounds " + x.ToString() + "," + y.ToString());
			}
		}

		public void SetDefaultTile(int u, int v, int w, int h, int dx = 0, int dy = 0) {
			this.defaultTile = new _Tile ();
			this.defaultTile.tileSprite = new Sprite (this.tileset);
			this.defaultTile.tileSprite.TextureRect = new IntRect (u, v, w, h);
			this.defaultTile.tileSprite.Scale = new Vector2f (this.TileScale, this.TileScale);
			// CellWidth and CellHeight are already scaled
			this.defaultTile.tileSprite.Position = new Vector2f (0f, 0f);
		}
	}

	/// <summary>
	/// Container layer for "objects". It has very few functionalities, it is mostly here
	/// for homogeneity
	/// </summary>
	public class ObjectsLayer: Layer {
		private RenderTexture layerTexture;
		private Sprite layerSprite;
		private List<OutObject> objects = new List<OutObject>();

		public ObjectsLayer(int width, int height): base (width, height) {
			this.layerTexture = new RenderTexture ((uint)this.Width, (uint)this.Height);
			this.layerSprite = new Sprite (this.layerTexture.Texture);
		}

		public OutObject AddObject(OutObject oo) {
			oo.Scale = this.Scale;
			this.objects.Add (oo);
			return oo;
		}

		public OutObject AddObject(string spriteId, int s, int t, int w, int h) {
			OutObject oo = new OutObject (spriteId, new IntRect (s, t, w, h));
			oo.Scale = this.Scale;
			this.objects.Add (oo);
			return oo;
		}

		public void DeleteObject(OutObject obj) {
			obj.ToBeDeleted = true;
			this.objects.Remove (obj);
		}

		override public void Draw(RenderTarget target) {
			this.objects.Sort ();
			this.layerTexture.Clear (Color.Transparent);
			foreach (var obj in this.objects) {
				obj.DrawWithOffset (this.layerTexture, this.center.X, this.center.Y);
			}
			this.layerTexture.Display ();
			target.Draw (this.layerSprite, this.Blend);
		}

		override public float Scale {
			get {
				return base.Scale;
			}
			set {
				base.Scale = value;
				foreach (var obj in this.objects) {
					obj.Scale = value;
					obj.X *= value;
					obj.Y *= value;
				}
			}
		}
	}
}

