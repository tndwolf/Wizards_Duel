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

namespace WizardsDuel.Io
{
	public enum Facing {
		LEFT,
		RIGHT
	}

	/// <summary>
	/// Basic container widget. No layouts are enforced
	/// </summary>
	public class Frame: Widget {
		private List<Widget> widgets = new List<Widget>();

		public void AddWidget(Widget widget) {
			widgets.Add (widget);
		}

		/// <summary>
		/// Clear the contents of the frame.
		/// </summary>
		public void Clear() {
			this.widgets.Clear ();
		}

		public void DeleteWidget(Widget widget) {
			widgets.Remove (widget);
		}

		override public void Draw(RenderTarget target) {
			base.Draw (target);
			foreach(var widget in this.widgets) {
				widget.Draw (target);
			}
		}
	}

	/// <summary>
	/// Basic icon widget
	/// </summary>
	public class Icon: Widget {
		public Sprite sprite = null;
		private Facing facing = Facing.RIGHT;
		protected bool updateFacing = false;
		protected Vector2f offset = new Vector2f (0f, 0f);

		public Icon(string texture, IntRect srcRect, float scale = 1f) {
			var tex = IO.LoadTexture (texture);
			this.sprite = new Sprite (tex, srcRect);
			//this.drawRect = this.sprite.GetGlobalBounds ();
			this.drawRect.Width = srcRect.Width;
			this.drawRect.Height = srcRect.Height;
			this.Scale = scale;
		}

		virtual public Color Color { 
			get { 
				return this.sprite.Color;
			}
			set {
				this.sprite.Color = value;
			}
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
			this.drawRect.Left += dx;
			this.drawRect.Top += dy;
			this.sprite.Position = new Vector2f (this.drawRect.Left, this.drawRect.Top);
		}

		virtual public float Scale {
			set { 
				this.sprite.Scale = new Vector2f (value, value);
				this.drawRect.Width *= value;
				this.drawRect.Height *= value;
			}
		}

		override public void SetPosition(float x, float y) {
			this.drawRect.Left = x;
			this.drawRect.Top = y;
			this.sprite.Position = new Vector2f (x, y);
		}

		override public void Draw(RenderTarget target) {
			base.Draw (target);
			if (this.updateFacing == true) {
				this.sprite.Scale = new Vector2f (-this.sprite.Scale.X, this.sprite.Scale.Y);
				switch (this.Facing) {
				case Facing.LEFT:
					this.offset = new Vector2f (this.Width, 0f);
					break;
				case Facing.RIGHT:
					this.offset = new Vector2f (0f, 0f);
					break;
				default:
					break;
				}
				this.updateFacing = false;
			}
			target.Draw(this.sprite);
		}
	}

	/// <summary>
	/// Basic single line label
	/// </summary>
	public class Label: Widget {
		private string text = "";
		private Text label;

		public Label(string text, int size = 12, string fontID = IO.DEFAULT_FONT) {
			this.text = text;
			this.label = new Text(text, IO.LoadFont(fontID, size), (uint)size);
			this.Width = label.GetGlobalBounds ().Width;
		}

		override public void Draw(RenderTarget target) {
			base.Draw (target);
			target.Draw(label);
		}

		override public float X {
			get { return this.drawRect.Left; }
			set { 
				this.drawRect.Left = value;
				this.label.Position = new Vector2f(value, this.Y);
			}
		}

		override public float Y {
			get { return this.drawRect.Top; }
			set { 
				this.drawRect.Top = value; 
				this.label.Position = new Vector2f(this.X, value);
			}
		}

		override public void SetPosition(float x, float y) {
			this.X = x;
			this.Y = y;
		}
	}

	/// <summary>
	/// Basic Widget class from which all widgets inhereit
	/// </summary>
	public class Widget {
		protected FloatRect drawRect = new FloatRect (0f, 0f, 0f, 0f);
		protected List<Animation> animators = new List<Animation> ();

		/// <summary>
		/// Adds an animator.
		/// </summary>
		/// <param name="animator">Animator.</param>
		public void AddAnimator(Animation animator) {
			this.animators.Add (animator);
		}

		/// <summary>
		/// Draw the widget on the specified target.
		/// </summary>
		/// <param name="target">Target.</param>
		virtual public void Draw(RenderTarget target) {
			List<Animation> newAnimators = new List<Animation> ();
			foreach (var animator in this.animators) {
				animator.Update (this);
				if (animator.HasEnded == false) {
					newAnimators.Add (animator);
				}
			}
			this.animators = newAnimators;
		}

		virtual public void Move(int dx, int dy) {
			this.X += dx;
			this.Y += dy;
		}

		/// <summary>
		/// Gets or sets the height of the widget in pixels.
		/// </summary>
		/// <value>The height.</value>
		virtual public float Height {
			get { return this.drawRect.Height; }
			set { this.drawRect.Height = value; }
		}

		virtual public bool IsAnimating {
			get { return this.animators.Count > 0; }
		}

		virtual public int PaddingBottom {
			get;
			set;
		}

		/// <summary>
		/// Sets the position of the widget relative to the screen.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		virtual public void SetPosition(float x, float y) {
			this.drawRect.Top = y;
			this.drawRect.Left = x;
		}

		/// <summary>
		/// Gets or sets the x coordinate relative to the screen.
		/// </summary>
		/// <value>The x.</value>
		virtual public float X {
			get { return this.drawRect.Left; }
			set { this.drawRect.Left = value; }
		}

		/// <summary>
		/// Gets or sets the y coordinate relative to the screen.
		/// </summary>
		/// <value>The y.</value>
		virtual public float Y {
			get { return this.drawRect.Top; }
			set { this.drawRect.Top = value; }
		}

		/// <summary>
		/// Gets or sets the width of the widget in pixels.
		/// </summary>
		/// <value>The width.</value>
		virtual public float Width {
			get { return this.drawRect.Width; }
			set { this.drawRect.Width = value; }
		}
	}
}

