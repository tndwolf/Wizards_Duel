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

		public Particle (string texture, IntRect srcRect, float scale = 1f) : base (texture, srcRect, scale) {
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
			List<Animation> newAnimators = new List<Animation> ();
			foreach (var animator in base.animators) {
				animator.Update (this);
				if (animator.HasEnded == false) {
					newAnimators.Add (animator);
				}
			}
			this.animators = newAnimators;
			this.alreadyAnimated = true;
			var t = IO.GetDelta ();
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
		private ObjectsLayer layer = null;
		private List<Particle> particles = new List<Particle>();

		public void CreateParticleEffect(ObjectsLayer layer, Vector2f position) {
			this.layer = layer;
			Random rnd = new Random();
			for (int i = 0; i < 100; i++) {
				var particle = new Particle (IO.LIGHT_TEXTURE_ID, new IntRect (0, 0, IO.LIGHT_TEXTRE_MAX_RADIUS*2, IO.LIGHT_TEXTRE_MAX_RADIUS*2));

				var force = (float)(rnd.NextDouble ())/4f;
				var angle = (float)(rnd.NextDouble ()) * Math.PI * 2;

				var forceX = force * (float)Math.Cos (angle);
				var forceY = force * (float)Math.Sin (angle);

				particle.Velocity = new Vector2f (forceX, forceY);
				particle.SetPosition (position.X, position.Y);
				particle.AddAnimator (new GravityAnimation (new Vector2f(0f, 0.0004f)));

				var colmin = 200;//100
				var colmax = 255;//255
				var col1 = new Color (255, (byte)rnd.Next(colmin, colmax), (byte)rnd.Next(colmin/2, colmax/2), 200);
				var col2 = new Color (col1.R, col1.G, col1.B, 0);
				particle.Color = col1;
				particle.AddAnimator (new ColorAnimation(col1, col2, 1000));
				particle.TTL = 1000;

				this.particles.Add (particle);
				this.layer.AddObject (particle);
				particle.Scale = 0.025f;
			}
		}

		override public void Draw(RenderTarget target = null) {
			this.TTL -= IO.GetDelta ();
			if (this.TTL < 0) {
				foreach (var particle in this.particles) {
					this.layer.DeleteObject (particle);
					//Logger.Debug ("ParticleSystem", "Draw", "Removed particle 1");
				}
				this.particles.Clear ();
				return;
			}
			foreach(var particle in this.particles) {
				if (particle.TTL < 0) {
					this.layer.DeleteObject (particle);
					//Logger.Debug ("ParticleSystem", "Draw", "Removed particle 2");
				}
			}
		}

		public long ParticlesTTL { get; set; }

		public long TTL { get; set; }
	}
}

