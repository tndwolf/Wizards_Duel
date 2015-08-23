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
		public string sfx;

		public AnimationFrame(int u, int v, int width, int height, int duration, string sfx="") {
			this.frame = new IntRect (u, v, width, height);
			this.duration = duration;
			this.sfx = sfx;
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
				sa.AppendSprite (frame.frame, frame.offset, frame.duration, frame.sfx);
			}
			obj.AddAnimator (sa);
		}
	}
	#endregion

	public class OutObject : Icon, IComparable {
		public const string IDLE_ANIMATION = "IDLE";
		protected bool alreadyAnimated = false;
		public Dictionary<string, AnimationDefinition> animations = new Dictionary<string, AnimationDefinition>();
		protected List<ParticleSystem> particles = new List<ParticleSystem> ();

		public OutObject(string texture, IntRect srcRect) : base (texture, srcRect) {}

		public void AddAnimation(string id, AnimationDefinition animation) {
			this.animations.Add (id, animation);
			Logger.Debug ("OutObject", "AddAnimation", "Added animation " + id);
			if (id == OutObject.IDLE_ANIMATION) {
				this.IdleAnimation = id;
				this.SetAnimation (id);
				Logger.Debug ("OutObject", "AddAnimation", "Set idle animation " + id);
			}
		}

		override public void AddAnimator(Animator a) {
			base.AddAnimator (a);
			foreach (var ps in this.particles) {
				ps.AddAnimator (a);
			}
		}

		public void AddParticleSystem(ParticleSystem ps) {
			this.particles.Add (ps);
		}

		virtual public void Animate() {
			if (StopAnimation == false) {
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
		}

		public Vector2f Center {
			get { return new Vector2f(this.X + this.Width/2f, this.Y + this.Height/2f); }
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

		override public void Draw(RenderTarget target, RenderStates states) {
			if (this.alreadyAnimated == false) {
				this.Animate ();
			}
			this.alreadyAnimated = false;
			/*var cs = new CircleShape (1);
			cs.Position = this.Position;
			target.Draw (cs, states);
			cs.Position = new Vector2f(this.Position.X + this.Width, this.Position.Y + Height);
			target.Draw (cs, states);*/
			states.Transform.Translate(this.facingOffset);
			states.Transform.Translate(this.Padding);
			target.Draw(this.sprite, states);
			//Logger.Debug ("OutObject", "Draw", "Drawing at " + this.Position.ToString ());
			foreach (var ps in this.particles) {
				ps.Position = this.Center;
			}
		}

		override public Facing Facing {
			get { return this.facing; }
			set {
				if (this.facing != value) {
					this.facing = value;
					//this.updateFacing = true;
					this.sprite.Scale = new Vector2f (-this.sprite.Scale.X, this.sprite.Scale.Y);
					if (this.Facing == Facing.LEFT) {
						this.facingOffset = new Vector2f (this.Width, 0f);
					} else {
						this.facingOffset = new Vector2f (0f, 0f);
					}
				}
			}
		}

		public float FeetY {
			get { return this.Y + this.Height + this.Padding.Y; }
			set { this.Y = value - this.Height - this.Padding.Y; }
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

		override public float Height { 
			get { return this.size.Y; }
			set { //throw new NotImplementedException(); }
				this.size.Y = value; 
				this.sprite.Scale = new Vector2f(this.sprite.Scale.X, value / this.sprite.TextureRect.Height);
			}
		}

		public string IdleAnimation {
			get;
			set;
		}

		virtual public bool IsAnimating {
			get { return !this.IsInIdle; }
		}

		public bool IsInIdle { get; set; }

		public float LightRadius { get; set; }

		public Color LightColor { get; set; }

		override public void Move(int dx, int dy) {
			this.sprite.Position = new Vector2f(this.sprite.Position.X + dx, this.sprite.Position.Y + dy);
		}

		override public Vector2f Position { 
			get { return this.sprite.Position; } 
			set { this.sprite.Position = value; }
		}

		public void RemoveAllParticleSystems() {
			foreach (var particle in this.particles) {
				particle.TTL = 0;
			}
			this.particles.RemoveAll (x => x.TTL < 1);
		}

		public void RemoveParticleSystem(string id) {
			foreach (var particle in this.particles) {
				if (particle.ID == id) {
					particle.TTL = 0;
				}
			}
			this.particles.RemoveAll (x => x.ID == id);
		}

		override public float ScaleX { 
			get { return this.sprite.Scale.X; } 
			set { this.Width = value * this.sprite.TextureRect.Width; }
		}

		override public float ScaleY { 
			get { return this.sprite.Scale.Y; } 
			set { this.Height = value * this.sprite.TextureRect.Height; }
		}

		public void SetAnimation(string id) {
			AnimationDefinition anim;
			if (this.animations.TryGetValue (id, out anim) == true) {
				this.animators.RemoveAll (x => x is SpriteAnimation);
				anim.SetAnimation (this);
				this.IsInIdle = (id == this.IdleAnimation);
			}
		}

		public bool StopAnimation { get; set; }

		public bool ToBeDeleted { get; set; }

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
			set { //throw new NotImplementedException(); }
				this.size.X = value; 
				this.sprite.Scale = new Vector2f(value / this.sprite.TextureRect.Width, this.sprite.Scale.X);
			}
		}

		public int ZIndex {
			get;
			set;
		}
	}

}

