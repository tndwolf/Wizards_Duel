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
	public class Particle : OutObject {
		public long TTL = 1000;

		public Particle (string texture, IntRect srcRect) : base (texture, srcRect) {
		}

		/// <summary>
		/// Accelerate the particles.
		/// </summary>
		/// <param name="a">The acceleration vector (pixel/milliseconds).</param>
		/// <param name="t">The delta time (milliseconds).</param>
		public void Accelerate(Vector2f a, long t) {
			//this.Velocity = new 
			this.Velocity.X += a.X * t;
			this.Velocity.Y += a.Y * t;
		}

		override public void Animate() {
			List<Animator> newAnimators = new List<Animator> ();
			foreach (var animator in base.animators) {
				animator.Update (this);
				if (animator.HasEnded == false) {
					newAnimators.Add (animator);
				}
			}
			this.animators = newAnimators;
			this.alreadyAnimated = true;
			var t = IoManager.DeltaTime;
			this.X += this.Velocity.X * t;
			this.Y += this.Velocity.Y * t;
			this.TTL -= t;
		}

		/// <summary>
		/// Gets or sets the instantaneous velocity of the particle.
		/// </summary>
		/// <value>The velocity in pixels/milliseconds.</value>
		public Vector2f Velocity = new Vector2f ();
	}

	public class ParticleSystem : Widget {
		public ObjectsLayer Layer = null;
		private List<Emitter> emitters = new List<Emitter> ();
		private List<Particle> particles = new List<Particle>();

		public ParticleSystem(string id) {
			this.ID = id;
		}

		public void AddEmitter(Emitter emitter) {
			//emitter.Offset = new Vector2f (this.X + emitter.Offset.X, this.Y + emitter.Offset.Y);
			emitter.Position = new Vector2f (this.X + emitter.Offset.X, this.Y + emitter.Offset.Y);
			this.emitters.Add (emitter);
		}

		public void AddParticle(Particle particle) {
			this.particles.Add (particle);
			this.Layer.AddObject (particle);
		}

		override public void Draw(RenderTarget target, RenderStates states) {
			var delta = IoManager.DeltaTime;
			this.TTL -= delta;
			if (this.TTL < 0) {
				this.emitters.Clear ();
			}
			foreach(var emitter in this.emitters) {
				emitter.Update (delta);
			}
			foreach(var particle in this.particles) {
				if (particle.TTL < 0) {
					this.Layer.DeleteObject (particle);
				}
			}
		}

		public string ID { 
			get;
			protected set;
		}

		override public Vector2f Position {
			get { return base.Position; }
			set {
				base.Position = value;
				foreach(var emitter in this.emitters) {
					emitter.Position = new Vector2f (value.X + emitter.Offset.X, value.Y + emitter.Offset.Y);
					//Logger.Debug ("Emitter", "Spawn", "Emitter at: "  + value.ToString());
				}
			}
		}

		override public string ToString() {
			var res = String.Format ("<particle id=\"???\" ttl=\"{0}\">\n", this.TTL);
			foreach (var emitter in this.emitters) {
				res += emitter.ToString () + "\n";
			}
			res += "</particle>";
			return res;
		}

		public long TTL { get; set; }
	}
}

