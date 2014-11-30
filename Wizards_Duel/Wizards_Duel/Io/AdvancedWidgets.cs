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
	public enum LayerType {
		UNDEFINED,
		FLOOR,
		LIGHTS,
		OBJECTS,
		WALL,
		GRID,
		COUNT
	}

	public class AnimationFrame {
		public int duration; // duration in millis
		public IntRect frame;

		public AnimationFrame(int u, int v, int width, int height, int duration) {
			this.frame = new IntRect (u, v, width, height);
			this.duration = duration;
		}

		override public string ToString() {
			return string.Format(
				"<frame x=\"%d\" y=\"%d\" width=\"%d\" height=\"%d\" duration=\"%d\"/>", 
				frame.Top, 
				frame.Left, 
				frame.Width, 
				frame.Height, 
				duration
			);
		}
	}

	public class AnimationDefinition {
		private int duration = 0; // duration in millis
		public List<AnimationFrame> frames = new List<AnimationFrame> ();

		public void AddFrame(AnimationFrame frame) {
			this.duration += frame.duration;
			this.frames.Add (frame);
		}

		public int Duration {get {return this.duration; }}

		public void SetAnimation(OutObject obj) {
			var sa = new SpriteAnimation ();
			foreach (var frame in this.frames) {
				sa.AppendSprite (frame.frame, frame.duration);
			}
			obj.AddAnimator (sa);
		}
	}

	public class OutObject : Icon {
		protected float scale = 1f;
		protected Vector2f halfSize = new Vector2f (0f, 0f);
		protected bool alreadyAnimated = false;
		public Dictionary<string, AnimationDefinition> animations = new Dictionary<string, AnimationDefinition>();

		public OutObject(string texture, IntRect srcRect, float scale = 1f) : base (texture, srcRect, scale) {
			this.halfSize.X = srcRect.Width / 2f;
			this.halfSize.Y = srcRect.Height / 2f;
		}

		public void AddAnimation(string id, AnimationDefinition animation) {
			this.animations.Add (id, animation);
		}

		virtual public void Animate() {
			List<Animation> newAnimators = new List<Animation> ();
			foreach (var animator in base.animators) {
				animator.Update (this);
				if (animator.HasEnded == false) {
					newAnimators.Add (animator);
				}
			}
			this.animators = newAnimators;
			this.alreadyAnimated = true;
		}

		public float CenterX {
			get { return this.X + this.halfSize.X; }
		}

		public float CenterY {
			get { return this.Y + this.halfSize.Y; }
		}

		public void DrawWithOffset(RenderTarget target, float x, float y) {
			if (this.alreadyAnimated == false) {
				/*List<Animation> newAnimators = new List<Animation> ();
				foreach (var animator in base.animators) {
					animator.Update (this);
					if (animator.HasEnded == false) {
						newAnimators.Add (animator);
					}
				}
				this.animators = newAnimators;*/
				this.Animate ();
			}
			this.alreadyAnimated = false;
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
			this.sprite.Position = new Vector2f (this.X - x + this.offset.X, this.Y - y + this.offset.Y);
			target.Draw(this.sprite);
		}

		/// <summary>
		/// Gets the length of the specific animation in milliseconds.
		/// </summary>
		/// <returns>The animation length.</returns>
		/// <param name="id">Identifier.</param>
		public int GetAnimationLength(string id) {
			AnimationDefinition anim;
			if (this.animations.TryGetValue (id, out anim) == true) {
				return anim.Duration;
			} else {
				return 0;
			}
		}

		override public float Scale {
			set {
				base.Scale = value;
				this.halfSize.X *= value;
				this.halfSize.Y *= value;
				this.scale = value;
			}
		}

		/// <summary>
		/// Sets the animation that will be used in the following frames
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void SetAnimation(string id) {
			AnimationDefinition anim;
			if (this.animations.TryGetValue (id, out anim) == true) {
				anim.SetAnimation (this);
			}
		}
	}

}

