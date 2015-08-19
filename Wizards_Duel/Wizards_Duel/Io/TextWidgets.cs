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
	public class Label: Widget {
		Text text;

		public Label(string text, int size = 16, string fontFile = null) {
			if (fontFile != null) {
				var font = IoManager.LoadFont(fontFile);
				this.text = new Text(text, font, (uint)size);
			}
			else if (IoManager.DefaultFontId != null) {
				this.text = new Text(text, IoManager.DefaultFont, (uint)size);
			}
		}

		virtual public Color Color {
			get { return this.text.Color; }
			set { this.text.Color = value; } 
		}

		override public void Draw(RenderTarget target, RenderStates states) {
			base.Draw(target, states);
			target.Draw(this.text, states);
		}

		virtual public int FontSize {
			get { return (int)this.text.CharacterSize; }
			set { this.text.CharacterSize = (uint)value; } 
		}

		override public Vector2f Position {
			get { return this.text.Position; } 
			set { this.text.Position = value; }
		}

		virtual public string Text {
			get { return this.text.DisplayedString; }
			set { this.text.DisplayedString = value; } 
		}

		override public float X { 
			get { return this.text.Position.X; } 
			set { this.text.Position = new Vector2f(value, this.text.Position.Y); }
		}

		override public float Y { 
			get { return this.text.Position.Y; } 
			set { this.text.Position = new Vector2f(this.text.Position.X, value); }
		}
	}
}

