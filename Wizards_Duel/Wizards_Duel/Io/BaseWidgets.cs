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
	public class Button: Widget, IClickable {
		//Icon icon = null;
		Label text = null;

		virtual public bool Contains(int x, int y) {
			return x > this.OffsetPosition.X
				&& x < this.OffsetPosition.X + this.Width
				&& y > this.OffsetPosition.Y
				&& y < this.OffsetPosition.Y + this.Height;
		}

		override public void Draw(RenderTarget target, RenderStates states) {
			base.Draw(target, states);
			this.OffsetPosition = states.Transform.TransformPoint(this.Position);
			if (text != null) target.Draw(text, states);
		}

		public string Text {
			get { 
				if (this.text != null) {
					return this.text.Text;
				}
				else {
					return String.Empty;
				}
			}
			set {
				if (this.text != null) {
					this.text.Text = value;
				}
				else {
					this.text = new Label(value);
					this.text.Color = Color.White;
				}
			}
		}

		#region IClickable implementation
		public bool Enabled { get; set; }

		public Vector2f OffsetPosition { get; set; }

		public void OnMouseMove(object sender, MouseMoveEventArgs e) {
			//throw new NotImplementedException();
			return;
		}

		private bool pressed = false;
		public void OnMousePressed(object sender, MouseButtonEventArgs e) {
			//Console.WriteLine("Clicked: " + e.ToString());
			//Console.WriteLine("My Position: " + this.Position.ToString());
			//Console.WriteLine("My Offset: " + this.OffsetPosition.ToString());
			if (this.Contains(e.X, e.Y)) {
				this.pressed = true;
			}
		}

		public void OnMouseReleased(object sender, MouseButtonEventArgs e) {
			if (this.Contains(e.X, e.Y) && this.pressed == true) {
				Console.WriteLine("Released: " + e.ToString());
				this.pressed = false;
			}
		}
		#endregion
	}

	/// <summary>
	/// Basic container for widgets. It does no transformations on the contents.
	/// </summary>
	public class Frame: Widget, IEnumerable<Widget> {
		List<Widget> widgets = new List<Widget>();

		public Frame() {
		}

		virtual public void AddWidget(Widget widget) {
			this.widgets.Add(widget);
		}

		virtual public void Clear() {
			this.widgets.Clear();
		}

		virtual public void DeleteWidget(Widget widget) {
			this.widgets.Remove (widget);
		}

		#region IEnumerable implementation
		public IEnumerator<Widget> GetEnumerator () {
			return this.widgets.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () {
			return GetEnumerator ();
		}
		#endregion

		override public void Draw(RenderTarget target, RenderStates states) {
			states.Transform.Translate(this.Position);
			base.Draw(target, states);
			foreach (var widget in this.widgets) {
				widget.Draw(target, states);
			}
		}

		virtual public bool Remove(Widget widget) {
			return this.widgets.Remove (widget);
		}

		override public float X { 
			get { return this.Position.X; }
			set { this.Position = new Vector2f(value, this.Position.Y); }
		}

		override public float Y { 
			get { return this.Position.Y; }
			set { this.Position = new Vector2f(this.Position.X, value); }
		}
	}

	/// <summary>
	/// Basic Widget class from which all widgets inhereit
	/// </summary>
	public class Widget: Drawable {
		protected List<Animator> animators = new List<Animator>();
		List<Decorator> decorators = new List<Decorator>();
		Vector2f position = new Vector2f();
		Vector2f size = new Vector2f();

		public Widget() {}

		virtual public void AddAnimator(Animator animator) {
			if (animator.IsParentValid(this)) {
				//this.animators.RemoveAll (x => animator.GetType () == x.GetType ());
				this.animators.Add(animator);
			}
			else {
				// TODO Log error
			}
		}

		virtual public void AddDecorator(Decorator decorator) {
			decorator.Parent = this;
			this.decorators.Add(decorator);
		}

		virtual public void ClearDecorators() {
			this.decorators.Clear();
		}

		#region Drawable implementation
		virtual public void Draw(RenderTarget target, RenderStates states) {
			PrivateDraw (this.DrawingRoutine, target, states);
		}

		virtual protected void DrawingRoutine(RenderTarget target, RenderStates states) {
			return;
		}

		protected void PrivateDraw(Action<RenderTarget, RenderStates> drawingRouting, RenderTarget target, RenderStates states) {
			foreach (var animator in this.animators) {
				animator.Update(this);
			}
			drawingRouting (target, states);
			foreach (var decorator in this.decorators) {
				decorator.Draw(target, states);
			}
		}
		#endregion

		virtual public void Move(int dx, int dy) {
			this.position = new Vector2f(this.position.X + dx, this.position.Y + dy);
		}

		virtual public float Height { 
			get { return this.size.Y; } 
			set { this.size = new Vector2f(this.size.X, value); }
		}

		virtual public Vector2f Position {
			get { return this.position; } 
			set { this.position = value; }
		}

		virtual public Vector2f Size {
			get { return this.size; } 
			set { this.size = value; }
		}

		virtual public float X { 
			get { return this.position.X; } 
			set { this.position = new Vector2f(value, this.position.Y); }
		}

		virtual public float Y { 
			get { return this.position.Y; } 
			set { this.position = new Vector2f(this.position.X, value); }
		}

		virtual public float Width { 
			get { return this.size.X; } 
			set { this.size = new Vector2f(value, this.size.Y); }
		}
	}
}

