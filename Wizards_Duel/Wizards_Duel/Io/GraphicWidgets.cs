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

namespace WizardsDuel.Io
{
	public enum Facing {
		RIGHT,
		LEFT
	}

	/// <summary>
	/// Basic icon widget
	/// </summary>
	public class Icon: Widget {
		protected Facing facing = Facing.RIGHT;
		protected Vector2f facingOffset = new Vector2f(0f, 0f);
		protected Vector2f size = new Vector2f();
		protected Sprite sprite;
		protected bool updateFacing = false;

		public Icon(string iconFileName, IntRect sprite): base() {
			var tex = IoManager.LoadTexture(iconFileName);
			this.sprite = new Sprite(tex);
			this.sprite.TextureRect = sprite;
			this.size = new Vector2f (sprite.Width, sprite.Height);
		}

		public Icon(string iconFileName, int x, int y, int w, int h): base() {
			var tex = IoManager.LoadTexture(iconFileName);
			this.sprite = new Sprite(tex);
			this.sprite.TextureRect = new IntRect(x, y, w, h);
			this.size = new Vector2f (w, h);
		}

		virtual public int Alpha {
			get { return this.sprite.Color.A; }
			set {
				var buff = this.sprite.Color;
				buff.A = (byte)value;
				this.sprite.Color = buff;
			} 
		}

		virtual public Color Color {
			get { return this.sprite.Color; }
			set { this.sprite.Color = value; } 
		}

		override protected void DrawingRoutine(RenderTarget target, RenderStates states) {
			if (this.updateFacing == true) {
				// flip the sprite
				this.sprite.Scale = new Vector2f (-this.sprite.Scale.X, this.sprite.Scale.Y);
				switch (this.Facing) {
				case Facing.LEFT:
					this.facingOffset = new Vector2f (this.Width, 0f);
					break;
				case Facing.RIGHT:
					this.facingOffset = new Vector2f (0f, 0f);
					break;
				default:
					break;
				}
				this.updateFacing = false;
			}
			states.Transform.Translate(this.facingOffset);
			states.Transform.Translate(this.Padding);
			target.Draw(this.sprite, states);
		}

		virtual public Facing Facing {
			get { return this.facing; }
			set {
				if (this.facing != value) {
					this.facing = value;
					this.updateFacing = true;
				}
			}
		}

		override public void Move(int dx, int dy) {
			this.sprite.Position = new Vector2f(this.sprite.Position.X + dx, this.sprite.Position.Y + dy);
		}

		public IntRect Sprite {
			get { return this.sprite.TextureRect; } 
			set { this.sprite.TextureRect = value; }
		}

		override public float Height { 
			get { return this.size.Y; }
			set { 
				this.size.Y = value; 
				this.sprite.Scale = new Vector2f(this.sprite.Scale.X, value / this.sprite.TextureRect.Height);
			}
		}

		virtual public Vector2f Origin { 
			get { return this.sprite.Origin; } 
			set { this.sprite.Origin = value; }
		}

		virtual public Vector2f Padding {
			get;
			set;
		}

		override public Vector2f Position { 
			get { return this.sprite.Position; } 
			set { this.sprite.Position = value; }
		}

		virtual public float ScaleX { 
			get { return this.sprite.Scale.X; } 
			set { this.Width = value * this.sprite.TextureRect.Width; }
		}

		virtual public float ScaleY { 
			get { return this.sprite.Scale.Y; } 
			set { this.Height = value * this.sprite.TextureRect.Height; }
		}

		override public Vector2f Size { 
			get { return this.size; } 
			set { this.X = value.X; this.Y = value.Y; }
		}

		override public float X { 
			get { return this.sprite.Position.X; } 
			set { this.sprite.Position = new Vector2f(value, this.sprite.Position.Y); }
		}

		override public float Y { 
			get { return this.sprite.Position.Y; } 
			set { this.sprite.Position = new Vector2f(this.sprite.Position.X, value); }
		}


		override public float Width { 
			get { return this.size.X; }
			set { 
				this.size.X = value; 
				this.sprite.Scale = new Vector2f(value / this.sprite.TextureRect.Width, this.sprite.Scale.X);
			}
		}
	}

	public class ButtonIcon: Icon, IClickable {
		public ButtonIcon(string iconFileName, IntRect sprite): base(iconFileName, sprite) {}

		virtual public bool Contains(int x, int y) {
			return x > this.OffsetPosition.X
				&& x < this.OffsetPosition.X + this.Width
				&& y > this.OffsetPosition.Y
				&& y < this.OffsetPosition.Y + this.Height;
		}

		override public void Draw(RenderTarget target, RenderStates states) {
			base.Draw(target, states);
			this.OffsetPosition = states.Transform.TransformPoint(this.Position);
		}

		public Action MousePressed;

		#region IClickable implementation
		public void OnMouseMove (object sender, MouseMoveEventArgs e) {
			return;
		}

		public void OnMousePressed (object sender, MouseButtonEventArgs e) {
			if (this.Contains (e.X, e.Y)) {
				this.MousePressed ();
			}
		}

		public void OnMouseReleased (object sender, MouseButtonEventArgs e) {
			return;
		}

		public bool Enabled { get; set; }

		public Vector2f OffsetPosition { get; set; }
		#endregion


	}
}

