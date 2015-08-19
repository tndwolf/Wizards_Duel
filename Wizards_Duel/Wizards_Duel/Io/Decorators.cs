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
using WizardsDuel.Utils;

namespace WizardsDuel.Io
{
	public class Decorator: Drawable {
		public Decorator() {
			return;
		}

		public Widget Parent { get; set; }

		#region Drawable implementation
		virtual public void Draw(RenderTarget target, RenderStates states) {
			throw new NotImplementedException();
		}
		#endregion
	}

	public class DamageBarDecorator: Decorator {
		private RectangleShape fill;

		public DamageBarDecorator(Color color, float level = 0f) {
			this.fill = new RectangleShape ();
			this.Color = color;
			this.Level = level;
		}

		/// <summary>
		/// Gets or sets the color of the bar.
		/// Alpha should be used to modulate the overlay effect
		/// </summary>
		/// <value>The color.</value>
		public Color Color {
			get { return fill.FillColor; }
			set { fill.FillColor = value; }
		}

		/// <summary>
		/// The level represents how much of the parent is covered by the bar
		/// from 0.0 (no overlay) to 1.0 (fully covered).
		/// </summary>
		/// <value>The level.</value>
		public float Level { get; set; }

		/// <summary>
		/// By default the bar is drawn from top to bottom. If set to true the bar
		/// will rise from the bottom
		/// </summary>
		/// <value><c>true</c> if invert axis; otherwise, <c>false</c>.</value>
		public bool InvertAxis { get; set; }

		#region Drawable implementation
		override public void Draw(RenderTarget target, RenderStates states) {
			fill.Size = new Vector2f(this.Parent.Width, this.Parent.Height * this.Level);
			if (this.InvertAxis) {
				var y = this.Parent.Position.Y + this.Parent.Height * (1.0f - this.Level);
				fill.Position = new Vector2f(this.Parent.Position.X, y);
			} else {
				fill.Position = this.Parent.Position;
			}
			target.Draw (fill, states);
		}
		#endregion
	}

	public class SolidBorder: Decorator {
		private RectangleShape outline;

		public SolidBorder() {
			this.outline = new RectangleShape ();
			this.outline.FillColor = Color.Transparent;
			this.outline.OutlineColor = Color.White;
			this.Thickness = 1f;
		}

		public SolidBorder(Color color, float thickness = 1f) {
			this.outline = new RectangleShape ();
			this.Color = color;
			this.Thickness = thickness;
		}

		public Color Color {
			get { return outline.OutlineColor; }
			set { outline.OutlineColor = value; }
		}

		public float Thickness {
			get { return this.outline.OutlineThickness; }
			set { this.outline.OutlineThickness = value; }
		}

		#region Drawable implementation
		override public void Draw(RenderTarget target, RenderStates states) {
			this.outline.Size = new Vector2f(this.Parent.Width, this.Parent.Height);
			this.outline.Position = this.Parent.Position;
			target.Draw (this.outline, states);
		}
		#endregion
	}
}

