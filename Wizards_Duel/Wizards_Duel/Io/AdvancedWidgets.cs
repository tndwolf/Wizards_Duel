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
	#region animations
	public class AnimationFrame {
		public int duration; // duration in millis
		public IntRect frame;
		public Vector2f offset;

		public AnimationFrame(int u, int v, int width, int height, int duration) {
			this.frame = new IntRect (u, v, width, height);
			this.duration = duration;
		}

		override public string ToString() {
			return string.Format(
				"<frame x=\"%d\" y=\"%d\" width=\"%d\" height=\"%d\" offsetX=\"%d\" offsetY=\"%d\" duration=\"%d\"/>", 
				frame.Top, 
				frame.Left, 
				frame.Width, 
				frame.Height, 
				offset.X,
				offset.Y,
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
				sa.AppendSprite (frame.frame, frame.offset, frame.duration);
			}
			obj.AddAnimator (sa);
		}
	}
	#endregion

	public class OutObject : Icon, IComparable {
		public const string IDLE_ANIMATION = "IDLE";

		protected bool alreadyAnimated = false;
		public Dictionary<string, AnimationDefinition> animations = new Dictionary<string, AnimationDefinition>();
		protected Vector2f halfSize = new Vector2f (0f, 0f);
		protected List<ParticleSystem> particles = new List<ParticleSystem> ();
		protected float scale = 1f;

		protected bool primaVolta = true;

		public OutObject(string texture, IntRect srcRect) : base (texture, srcRect) {
			this.updateFacing = true;
		}

		public void AddAnimation(string id, AnimationDefinition animation) {
			this.animations.Add (id, animation);
			Logger.Debug ("OutObject", "AddAnimation", "Added animation " + id);
			if (id == OutObject.IDLE_ANIMATION) {
				this.IdleAnimation = id;
				this.SetAnimation (id);
				Logger.Debug ("OutObject", "AddAnimation", "Set idle animation " + id);
			}
		}

		public void AddParticleSystem(ParticleSystem ps) {
			this.particles.Add (ps);
		}

		virtual public void Animate() {
			List<Animator> newAnimators = new List<Animator> ();
			foreach (var animator in base.animators) {
				animator.Update (this);
				if (animator.HasEnded == false) {
					newAnimators.Add (animator);
				}
			}
			this.animators = newAnimators;
			this.alreadyAnimated = true;
			if (newAnimators.Count == 0 && this.IdleAnimation != null) {
				this.SetAnimation (this.IdleAnimation);
			}
		}

		public float CenterX {
			get { return this.X + this.Width/2f; }
		}

		public float CenterY {
			get { return this.Y + this.Height/2f; }
		}

		/// <summary>
		/// Compares two OutObjects to decide the drawing order.
		/// </summary>
		/// <returns>The comparison.</returns>
		/// <param name="obj">Reference object.</param>
		public int CompareTo(object obj) {
			try {
				var comp = (OutObject) obj;
				if (this.ZIndex == comp.ZIndex) {
					return this.FeetY.CompareTo (comp.FeetY);
				} else {
					return this.ZIndex.CompareTo (comp.ZIndex);
				}
			} catch (Exception ex) {
				Logger.Debug ("OutObject", "CompareTo", "Trying to compare a wrong object" + ex.ToString());
				return 0;
			}
		}

		override public Facing Facing {
			get { return this.facing; }
			set {
				// This is f****** stupid, I know
				// but facing MUST be inverted to work correctly
				// you cannot just flip the vector later on or
				// correct the facing just after initialization when everything
				// should not change...
				if (this.facing == value) {
					this.facing = (value == Facing.LEFT)? Facing.RIGHT : Facing.LEFT;
					this.updateFacing = true;
				}
			}
		}

		override public void Draw(RenderTarget target, RenderStates states) {
			//base.Draw(target, states);
			if (this.alreadyAnimated == false) {
				this.Animate ();
			}
			this.alreadyAnimated = false;
			if (this.updateFacing == true) {
				// XXX note that the facing vector is also used to center the
				// object inside a cell, see the Translate call a few lines below
				this.sprite.Scale = new Vector2f (-this.sprite.Scale.X, this.sprite.Scale.Y);
				if (this.Facing == Facing.LEFT) {
					this.facingOffset = new Vector2f (-this.Width/2f, -this.Height/2f);
				} else {
					this.facingOffset = new Vector2f (this.Width/2f, -this.Height/2f);
				}
				this.updateFacing = false;
				this.primaVolta = false;
			}
			var cs = new CircleShape (1);
			cs.Position = this.Position;
			//target.Draw (cs, states);
			states.Transform.Translate(this.facingOffset);
			states.Transform.Translate(this.Padding);
			target.Draw(this.sprite, states);
			//Logger.Debug ("OutObject", "Draw", "Drawing at " + this.Position.ToString ());
			foreach (var ps in this.particles) {
				ps.Position = this.Position;
			}
		}

		public float FeetY {
			get { return this.Y + this.Height; }
			private set { this.Y = value - this.Height; }
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

		/*override public float Height {
			set { 
				base.Height = value;
				if (this.Facing == Facing.LEFT) {
					this.facingOffset = new Vector2f (-this.Width/2f, -this.Height/2f);
				} else {
					this.facingOffset = new Vector2f (this.Width/2f, -this.Height/2f);
				}
			}
		}*/

		public string IdleAnimation {
			get;
			set;
		}

		virtual public bool IsAnimating {
			get { return !this.IsInIdle; }
		}

		public bool IsInIdle { get; set; }

		/// <summary>
		/// Sets the animation that will be used in the following frames
		/// </summary>
		/// <param name="id">Identifier.</param>
		public void SetAnimation(string id) {
			AnimationDefinition anim;
			if (this.animations.TryGetValue (id, out anim) == true) {
				this.animators.RemoveAll (x => x is SpriteAnimation);
				anim.SetAnimation (this);
				this.IsInIdle = (id == OutObject.IDLE_ANIMATION);
			}
		}

		public bool ToBeDeleted { get; set; }

		/*override public float Width {
			set { 
				base.Width = value;
				if (this.Facing == Facing.LEFT) {
					this.facingOffset = new Vector2f (-this.Width/2f, -this.Height/2f);
				} else {
					this.facingOffset = new Vector2f (this.Width/2f, -this.Height/2f);
				}
			}
		}*/

		public int ZIndex {
			get;
			set;
		}
	}

}

