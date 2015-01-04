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
	public enum AnimationDirection {
		UP = 0x00000001,
		DOWN = 0x00000010,
		LEFT = 0x00000100,
		RIGHT = 0x00001000
	}

	/// <summary>
	/// Base animation class from which all the Animations are derived
	/// </summary>
	public class Animation {
		protected bool hasEnded = false;

		/// <summary>
		/// This function is checked by the parent, if the animation has ended then it is removed
		/// from the list of active animations
		/// </summary>
		/// <returns><c>true</c> if this instance has ended; otherwise, <c>false</c>.</returns>
		public bool HasEnded {
			get {return this.hasEnded;}
		}

		/// <summary>
		/// Update the specified parent.
		/// </summary>
		/// <param name="parent">Parent.</param>
		virtual public void Update(Widget parent) {
			this.hasEnded = true;
			return;
		}
	}

	public class AttractAnimation: Animation {
		private float strength;
		private float strengthX;
		private float strengthY;
		private Vector2f target;
		private const float DESTROY_RADIUS = 24f;

		public AttractAnimation(Vector2f source, Vector2f target, float strength, bool deleteOnReach = true) {
			this.DeleteOnReach = deleteOnReach;
			var dx = target.X - source.X;
			var dy = target.Y - source.Y;
			var distance = (float)Math.Sqrt (dx * dx + dy * dy);
			this.strength = strength;
			this.strengthX = strength * dx / distance;
			this.strengthY = strength * dy / distance;
			this.target = target;
		}

		public bool DeleteOnReach { get; set; }

		override public void Update(Widget parent) {
			var dx = this.target.X - parent.X;
			var dy = this.target.Y - parent.Y;
			/*var acceleration = new Vector2f (
				Math.Sign(dx) * this.strengthX,
				Math.Sign(dy) * this.strengthY
			);*/
			var distance = (float)Math.Sqrt (dx * dx + dy * dy);
			var acceleration = new Vector2f (
				this.strength * dx / (distance*distance),
				this.strength * dy / (distance*distance)
			);
			((Particle)parent).Accelerate (acceleration, IO.GetDelta ());
			if (this.DeleteOnReach) {
				if (distance < DESTROY_RADIUS)
					((Particle)parent).TTL = 0;
			}
		}
	}

	public class ColorAnimation : Animation {
		private int endTime = 0;
		private int refTime = 0;
		float k = 0.0f; // position on the animation [0.0 -> 1.0]

		public ColorAnimation(Color startColor, Color endColor, int duration) {
			this.EndColor = endColor;
			this.StartColor = startColor;
			this.endTime = duration;
		}

		public Color EndColor { get; set; }

		public Color StartColor { get; set; }

		override public void Update(Widget parent) {
			var p = (Icon)parent;
			this.refTime += IO.GetDelta ();
			if (this.refTime > this.endTime) {
				this.k = 1.0f;
				p.Color = this.EndColor;
				this.hasEnded = true;
			}
			else {
				this.k = 1.0f - (float)(this.endTime - this.refTime) / (float)(this.endTime);
				p.Color = new Color (
					(byte)(this.StartColor.R * (1f-k) + this.EndColor.R * k),
					(byte)(this.StartColor.G * (1f-k) + this.EndColor.G * k),
					(byte)(this.StartColor.B * (1f-k) + this.EndColor.B * k),
					(byte)(this.StartColor.A * (1f-k) + this.EndColor.A * k)
				);
			}
		}
	}

	public class GravityAnimation : Animation {
		private Vector2f gravity = new Vector2f(0f, 0.032f);

		/// <summary>
		/// Initializes a new instance of the <see cref="WizardsDuel.Io.GravityAnimation"/> class.
		/// </summary>
		/// <param name="gravity">Gravity acceleration in pixels/milliseconds^2.</param>
		public GravityAnimation(Vector2f gravity) {
			this.Gravity = gravity;
		}
			
		// Gravity in pixels/milliseconds^2
		public Vector2f Gravity { get; set ; }

		override public void Update(Widget parent) {
			var p = (Particle)parent;
			var t = IO.GetDelta ();
			p.Accelerate (this.Gravity, t);
		}
	}

	/// <summary>
	/// Keyframes fro the <see cref="SpriteAnimation"/>.
	/// </summary>
	public struct KeyFrame {
		public int millis;
		public IntRect rect;
	}

	public class OrthoPathAnimation: Animation {
		int endRef; // end millis
		int currRef = 0; // millis from the start
		int stepSize; // in pixels
		int refStep = 0;
		float k = 0.0f; // position on the animation [0.0 -> 1.0]
		int step = 0;
		List<AnimationDirection> path = new List<AnimationDirection> ();

		public OrthoPathAnimation(int stepMillis, int stepSize) {
			this.endRef = stepMillis;
			this.stepSize = stepSize;
		}

		public void AddStep(AnimationDirection direction) {
			this.path.Add(direction);
		}

		public void AddPath(AnimationDirection[] path) {
			this.path.AddRange (path);
		}

		public void AddPath(List<AnimationDirection> path) {
			this.path.AddRange (path);
		}

		override public void Update(Widget parent) {
			int move = 0;
			int deltaMove = 0;
			this.currRef += IO.GetDelta ();
			if (this.currRef > this.endRef) {
				this.refStep = 0;
				this.currRef = this.currRef - this.endRef;
				this.k = 0.0f;
				if (++this.step >= this.path.Count) {
					this.hasEnded = true;
					return;
				}
			}
			else {
				this.k = 1.0f - (float)(this.endRef - this.currRef) / (float)(this.endRef);
				move = (int)(this.stepSize * k);
			}
			deltaMove = move - this.refStep;
			switch (this.path[this.step]) {
			case AnimationDirection.DOWN:
				parent.Move (0, deltaMove);
				break;

			case AnimationDirection.UP:
				parent.Move (0, -deltaMove);
				break;

			case AnimationDirection.LEFT:
				parent.Move (-deltaMove, 0);
				break;

			case AnimationDirection.RIGHT:
				parent.Move (deltaMove, 0);
				break;

			default:
				break;
			}
			this.refStep = move;
		}
	}

	public class SpringAnimation: Animation {
		int endRef; // end millis
		int currRef = 0; // millis from the start
		int maxExtension; // in pixels
		int loops;
		float k = 0.0f; // position on the animation [0.0 -> 1.0]

		/// <summary>
		/// Initializes a new instance of the <see cref="EverchangingRogueArena.StepAnimation"/> class.
		/// </summary>
		/// <param name="durationMillis">Duration in milliseconds of a loop.</param>
		/// <param name="maxExtension">The movement in pixel (reached during half animation)</param>
		public SpringAnimation(int durationMillis, int maxExtension, int loops = 1) {
			this.endRef = durationMillis;
			this.maxExtension = maxExtension;
			this.loops = loops;
		}

		/*public void SetSpeed(float pixelsPerSecond) {
			this.speed = pixelsPerSecond;
		}

		public void SetLength(int durationMillis) {
			this.hasEnded = durationMillis;
		}*/

		override public void Update(Widget parent) {
			int move = 0;
			this.currRef += IO.GetDelta ();
			if (this.currRef > this.endRef) {
				this.currRef = this.currRef - this.endRef;
				this.k = 0.0f;
				if (--this.loops < 1) {
					this.hasEnded = true;
				}
			}
			else if (this.currRef <= this.endRef/2) {
				this.k = 1.0f - (float)(this.endRef - this.currRef * 2) / (float)(this.endRef);
				move = (int)(this.maxExtension * k);
			}
			else {
				this.k = (1.0f - (float)(this.endRef - this.currRef * 2)) / (float)(this.endRef);
				move = this.maxExtension - (int)(this.maxExtension * k);
			}
			parent.Move (0, parent.PaddingBottom - move);
			parent.PaddingBottom = move;
		}
	}

	public class SpriteAnimation: Animation {
		int currRef = 0; // millis from the start
		int endRef = 0; // ending of animation
		int currFrame = 0;
		List<KeyFrame> frames = new List<KeyFrame> ();

		public SpriteAnimation(bool looping = false) {
			this.Looping = looping;
		}

		/// <summary>
		/// Append the sprite.
		/// </summary>
		/// <param name="sprite">Sprite.</param>
		/// <param name="duration">Duration of the sprite in milliseconds. 
		/// Note that it is different from when instancing the Animation directly</param>
		public void AppendSprite(IntRect sprite, int duration) {
			var end = duration + this.endRef;
			var kf = new KeyFrame() { millis = end, rect = sprite};
			//Console.WriteLine ("Appending sprite " + kf.rect.ToString() + " ending at " + end.ToString());
			this.frames.Add (kf);
			this.endRef += duration;
		}

		/// <summary>
		/// If looping the aniamtion will loop forever. 
		/// </summary>
		/// <value><c>true</c> if looping; otherwise, <c>false</c>.</value>
		public bool Looping {
			get;
			set;
		}

		override public void Update(Widget parent) {
			this.currRef += IO.GetDelta ();
			if (this.currRef > this.endRef) {
				//var sprite = ((Icon)parent).sprite;
				(((Icon)parent).sprite).TextureRect = this.frames [this.frames.Count-1].rect;
				if (this.Looping == false) {
					this.hasEnded = true;
					return;
				} else {
					this.currRef -= this.endRef;
				}
			}
			try {
				if (this.frames [this.currFrame].millis < this.currRef) {
					this.currFrame += 1;
					var sprite = ((Icon)parent).sprite;
					(((Icon)parent).sprite).TextureRect = this.frames [this.currFrame].rect;
				}
			}
			catch {
				this.hasEnded = true;
			}
		}
	}

	public class TranslateAnimation: Animation {
		int endRef; // end millis
		int currRef = 0; // millis from the start
		int deltaX; // in pixels
		int deltaY; // in pixels
		int refStepX = 0;
		int refStepY = 0;
		float k = 0.0f; // position on the animation [0.0 -> 1.0]

		public TranslateAnimation(int stepMillis, int deltaX, int deltaY) {
			this.endRef = stepMillis;
			this.deltaX = deltaX;
			this.deltaY = deltaY;
		}

		override public void Update(Widget parent) {
			int moveX = 0;
			int moveY = 0;
			this.currRef += IO.GetDelta ();
			if (this.currRef > this.endRef) {
				this.k = 1.0f;
				moveX = (int)(this.deltaX * k);
				moveY = (int)(this.deltaY * k);
				parent.Move (moveX - this.refStepX, moveY - this.refStepY);
				this.hasEnded = true;
				Console.WriteLine ("Translate ending at " + IO.GetTime().ToString());
			}
			else {
				this.k = 1.0f - (float)(this.endRef - this.currRef) / (float)(this.endRef);
				moveX = (int)(this.deltaX * k);
				moveY = (int)(this.deltaY * k);
				parent.Move (moveX - this.refStepX, moveY - this.refStepY);
				this.refStepX = moveX;
				this.refStepY = moveY;
			}
		}
	}
}

