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

		public void CreateParticleEffect(ObjectsLayer layer) {
			this.Layer = layer;
			var e = new Emitter (this, 250);
			e.Offset = new Vector2f(8f, -4f);
			e.SpawnCount = 3;
			e.SpawnDeltaTime = 50;
			e.TTL = 1000;
			e.ParticleTTL = 2500;
			//e.AddAnimator (new GravityAnimation (new Vector2f(0f, -0.0004f)));
			e.AddVariator (new BurstSpawner (0.15f, 0.025f));
			//e.AddVariator (new BoxSpawner (new Vector2f(position.X+12f, position.Y+48f), new Vector2f(position.X+48f, position.Y+52f)));
			//e.AddVariator (new BoxSpawner (new Vector2f(position.X-220f, position.Y-148f), new Vector2f(position.X+256f, position.Y-164f)));
			//e.AddVariator (new ColorSpawner(new Color(255, 255, 255, 196), new Color(255, 255, 255, 0), 2000));
			//e.AddParticleTemplate ("FX01.png", 50, 176, 28, 78, 0.6f);
			//e.AddParticleTemplate ("FX01.png", 150, 165, 24, 84, 0.6f);
			//e.AddParticleTemplate ("FX01.png", 244, 178, 14, 64, 0.6f);//*/
			e.AddParticleTemplate ("FX01.png", 0, 0, 1, 1, 2f);
			//e.AddParticleTemplate ("FX01.png", 2, 0, 1, 3, 2f);
			e.AddParticleTemplate ("FX01.png", 10, 0, 1, 1, 2f);
			//e.AddParticleTemplate ("FX01.png", 6, 0, 1, 3, 2f);
			e.AddParticleTemplate ("FX01.png", 8, 0, 1, 1, 2f);//*/
			//this.AddEmitter (e);

			//e.Origin = new Vector2f(position.X+56f, position.Y+4f);
			//e.Origin = new Vector2f(position.X+32, position.Y+12);
			//e.AddVariator (new BurstSpawner (0.2f, 0.22f, (float)Math.PI - 0.1f, (float)Math.PI + 0.1f));
			//e.AddVariator (new BoxSpawner (new Vector2f(position.X-64*2, position.Y-64*2), new Vector2f(position.X+64*3, position.Y+64*3)));
			//e.AddAnimator(new AttractAnimation(e.Origin, new Vector2f(position.X+64f*3, position.Y+4), 0.05f));
			//e.AddAnimator(new AttractAnimation(e.Origin, new Vector2f(position.X+56f*3, position.Y+100), 0.00125f, false));
			//e.AddAnimator(new AttractAnimation(new Vector2f(position.X+32f, position.Y+32f), new Vector2f(position.X+32f, position.Y+32f), 0.025f, false));
			//e.AddVariator (new ColorSpawner(new Color(255, 255, 255, 255), new Color(255, 255, 255, 128), 5000));
			//e.AddParticleTemplate ("FX01.png", 143, 331, 42, 42, 0.5f);
			//e.AddParticleTemplate ("FX01.png", 8, 0, 1, 1, 2f);//18, 14, 16 giallo
			//e.AddParticleTemplate ("FX01.png", 4, 0, 1, 1, 2f);
			//e.AddParticleTemplate ("FX01.png", 6, 0, 1, 1, 2f);
			//e.AddParticleTemplate ("FX01.png", 10, 0, 1, 1, 2f);
			this.AddEmitter (e);//*/

			// Lower part of atom
			/*/e = new Emitter (this, 250);
			e.Origin = new Vector2f(position.X+56f, position.Y+4f);
			e.SpawnCount = 2;
			e.SpawnDeltaTime = 100;
			e.TTL = 3250;
			e.ParticleTTL = 3500;
			e.Origin = new Vector2f(position.X+32, position.Y+64+24);
			e.AddVariator (new BurstSpawner (0.05f, 0.06f, 0.1f, 0.1f));
			e.AddAnimator(new AttractAnimation(new Vector2f(position.X+32f, position.Y+32f), new Vector2f(position.X+32f, position.Y+32f), 0.025f, false));
			e.AddVariator (new ColorSpawner(new Color(255, 255, 255, 255), new Color(255, 255, 255, 128), 5000));
			e.AddParticleTemplate ("FX01.png", 18, 0, 1, 1, 2f);
			e.AddParticleTemplate ("FX01.png", 14, 0, 1, 1, 2f);
			e.AddParticleTemplate ("FX01.png", 16, 0, 1, 1, 2f);
			this.AddEmitter (e);//*/


			/*/e = new Emitter (this, 250);
			e.Origin = position;//new Vector2f (position.X + 32f, position.Y + 48f);
			e.SpawnCount = 1;
			e.SpawnDeltaTime = 200;
			e.TTL = 3000;
			e.ParticleTTL = 500;
			e.AddAnimator (new GravityAnimation (new Vector2f(0f, -0.0003f)));
			e.AddVariator (new BurstSpawner (0.25f));
			e.AddVariator (new BoxSpawner (new Vector2f(position.X+8f, position.Y+48f), new Vector2f(position.X+48f, position.Y+64f)));
			e.AddVariator (new ColorSpawner(new Color(255, 255, 255, 196), Color.Transparent, 1000));
			e.AddParticleTemplate (IO.LIGHT_TEXTURE_ID, 0, 0, IO.LIGHT_TEXTRE_MAX_RADIUS * 2, IO.LIGHT_TEXTRE_MAX_RADIUS * 2, 0.025f);
			this.AddEmitter (e);//*/
		}

		public void AddEmitter(Emitter emitter) {
			//emitter.Offset = new Vector2f (this.X + emitter.Offset.X, this.Y + emitter.Offset.Y);
			emitter.Position = new Vector2f (this.X + emitter.Offset.X, this.Y + emitter.Offset.Y);
			this.emitters.Add (emitter);
		}

		public void AddParticle(Particle particle) {
			/*if (this.LightsLayer != null) {
				Random rnd = new Random();
				var colmin = 164;
				var colmax = 255;
				//var col = new Color (200, (byte)rnd.Next(colmin, colmax), (byte)rnd.Next(colmin, colmax));
				var col = new Color ((byte)rnd.Next(colmin, colmax), (byte)rnd.Next(colmin, colmax), (byte)rnd.Next(colmin, colmax));
				var light = this.LightsLayer.AddLight (0f, 0f, 32f, col);
				light.Parent = particle;
			}*/
			this.particles.Add (particle);
			this.Layer.AddObject (particle);
			//Logger.Debug ("ParticleSystem", "AddParticle", "Siamo sicuri che ha aggiunto qualcosa? " + particle.Position.ToString());
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

		public LightLayer LightsLayer { get; set; }

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

