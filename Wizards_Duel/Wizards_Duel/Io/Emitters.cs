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
using WizardsDuel.Game;

namespace WizardsDuel.Io
{
	public struct ParticleTemplate {
		public string texture;
		public IntRect textureRect;
		public float scale;
	}

	public class Emitter {
		public List<Animator> animators = new List<Animator> ();
		public int currentParticles = 0;
		public int maxParticles = 0;
		public Vector2f Offset = new Vector2f (0f, 0f);
		private ParticleSystem particleSystem = null;
		private List<ParticleTemplate> particleTemplates = new List<ParticleTemplate> ();
		public int ParticleTTL = 0;
		private int refDeltaTime = 0;
		public int SpawnDeltaTime = 0;
		public int SpawnCount = 0;
		public int StartDelay = 0;
		public int TTL = 0;
		private List<Spawner> variators = new List<Spawner>();

		public Emitter(ParticleSystem ps, int startDelay = 0) {
			this.particleSystem = ps;
			this.refDeltaTime -= startDelay;
			this.StartDelay = startDelay;
		}

		public void AddAnimator(Animator animator) {
			this.animators.Add (animator);
		}

		public void AddParticleTemplate(string texture, int x, int y, int width, int height, float scale = 1f) {
			var rect = new IntRect (x, y, width, height);
			var pt = new ParticleTemplate () { texture = texture, textureRect = rect, scale = scale};
			this.particleTemplates.Add (pt);
		}

		public void AddVariator(Spawner variator) {
			this.variators.Add (variator);
		}

		public Vector2f Position {
			get;
			set;
		}

		protected void Spawn() {
			Random rnd = new Random();
			for (int i = 0; i < this.SpawnCount; i++) {
				/*var particle = new Particle (IO.LIGHT_TEXTURE_ID, new IntRect (0, 0, IO.LIGHT_TEXTRE_MAX_RADIUS*2, IO.LIGHT_TEXTRE_MAX_RADIUS*2));

				this.particleSystem.AddParticle (particle);

				var colmin = 200;//100
				var colmax = 255;//255
				var col1 = new Color (255, (byte)rnd.Next(colmin, colmax), (byte)rnd.Next(colmin/2, colmax/2), 200);
				var col2 = new Color (col1.R, col1.G, col1.B, 0);
				particle.Color = col1;
				particle.AddAnimator (new ColorAnimation(col1, col2, 1000));
				particle.TTL = this.ParticleTTL;

				particle.SetPosition (this.Origin.X, this.Origin.Y);
				particle.Scale = 0.025f;*/
				var template = this.particleTemplates [rnd.Next (this.particleTemplates.Count)];
				var particle = new Particle (template.texture, template.textureRect);
				this.particleSystem.AddParticle (particle);
				//particle.Origin = new Vector2f(particle.Width/2, particle.Height/2);
				particle.Position = new Vector2f(this.Position.X, this.Position.Y);
				particle.ScaleX = template.scale;
				particle.ScaleY = template.scale;
				particle.TTL = this.ParticleTTL;
				particle.ZIndex = this.ZIndex;
				//Logger.Debug ("Emitter", "Spawn", "Creating particle at: "  + this.Offset.ToString());

				foreach (var variator in this.variators) {
					variator.Apply (particle);
				}
				foreach (var animator in this.animators) {
					particle.AddAnimator (animator.Clone());
				}
			}
		}

		public void Update(int deltaTime) {
			this.TTL -= deltaTime;
			this.refDeltaTime += deltaTime;
			if (this.TTL > 0 && this.refDeltaTime > this.SpawnDeltaTime) {
				//this.SpawnDeltaTime = this.refDeltaTime - this.SpawnDeltaTime;
				this.refDeltaTime = this.refDeltaTime - this.SpawnDeltaTime;
				this.Spawn();
			}
		}

		override public string ToString() {
			var res = String.Format (
				"<emitter offsetX=\"{0}\" offsetY=\"{1}\" particleTtl=\"{2}\" spawnCount=\"{3}\" spawnDeltaTime=\"{4}\" startDelay=\"{5}\" ttl=\"{6}\" zIndex=\"{7}\">", 
				this.Offset.X,
				this.Offset.Y,
				this.ParticleTTL,
				this.SpawnCount,
				this.SpawnDeltaTime,
				this.StartDelay,
				this.TTL,
				this.ZIndex
			);
			res += "</emitter>";
			return res;
		}

		public int ZIndex { get; set; }
	}

	public class Spawner {
		virtual public void Apply(Particle particle) {
			return;
		}
	}

	public class BurstSpawner: Spawner {
		private float deltaForce;
		private float minForce;
		private float minAngle;
		private float deltaAngle;

		public BurstSpawner(float maxForce, float minForce = 0f, float minAngle = 0f, float maxAngle = (float)Math.PI * 2) {
			this.deltaForce = maxForce - minForce;
			this.minForce = minForce;
			this.deltaAngle = maxAngle - minAngle;
			this.minAngle = minAngle;
		}

		override public void Apply(Particle particle) {
			var force = this.minForce + Simulator.Instance.Random() * this.deltaForce;
			var angle = this.minAngle + Simulator.Instance.Random() * this.deltaAngle;

			var forceX = force * (float)Math.Cos (angle);
			var forceY = force * (float)Math.Sin (angle);

			particle.Velocity = new Vector2f (forceX, forceY);
		}
	}

	public class BurstInSpawner: Spawner {
		private float deltaForce;
		private float minForce;
		private float minAngle;
		private float deltaAngle;
		private float deltaRadius;
		private float minRadius;

		public BurstInSpawner(float maxRadius, float minRadius, float maxForce, float minForce = 0f, float minAngle = 0f, float maxAngle = (float)Math.PI * 2) {
			this.deltaForce = maxForce - minForce;
			this.minForce = minForce;
			this.deltaAngle = maxAngle - minAngle;
			this.minAngle = minAngle;
			this.deltaRadius= maxRadius - minRadius;
			this.minRadius = minRadius;
		}

		override public void Apply(Particle particle) {
			var force = this.minForce + Simulator.Instance.Random() * this.deltaForce;
			var angle = this.minAngle + Simulator.Instance.Random() * this.deltaAngle;
			var radius = this.minRadius + Simulator.Instance.Random() * this.deltaRadius;

			var x = particle.Position.X + radius * (float)Math.Cos (angle);
			var y = particle.Position.Y + radius * (float)Math.Sin (angle); 
			particle.Position = new Vector2f(x, y);

			var forceX = -force * (float)Math.Cos (angle);
			var forceY = -force * (float)Math.Sin (angle);
			particle.Velocity = new Vector2f (forceX, forceY);
		}
	}

	public class BoxSpawner: Spawner {
		private float height;
		private float width;

		public BoxSpawner(float width, float height) {
			this.height = height;
			this.width = width;
		}

		override public void Apply(Particle particle) {
			var x = particle.Position.X + Simulator.Instance.Random() * this.width;
			var y = particle.Position.Y + Simulator.Instance.Random() * this.height;
			particle.Position = new Vector2f(x, y);
		}
	}

	public class ColorSpawner: Spawner {
		private int duration;
		private Color startColor;
		private Color endColor;

		public ColorSpawner(Color startColor, Color endColor, int duration) {
			this.duration = duration;
			this.startColor = startColor;
			this.endColor = endColor;
		}

		override public void Apply(Particle particle) {
			particle.Color = this.startColor;
			particle.AddAnimator (new ColorAnimation(this.startColor, this.endColor, this.duration));
		}
	}

	public class ColorPickerSpawner: Spawner {
		private List<Color> colors = new List<Color>();

		public void AddColor(Color color) {
			this.colors.Add(color);
		}

		override public void Apply(Particle particle) {
			particle.Color = this.colors[Simulator.Instance.Random(this.colors.Count)];
		}
	}

	public class GridSpawner: Spawner {
		private float cellHeight;
		private float cellWidth;
		private float deltaX;
		private float deltaY;
		private int gridHeight;
		private int gridWidth;
		private int lastSpawn = 0;
		private int maxSpawn;

		public GridSpawner(int gridWidth, int gridHeight, float cellWidth, float cellHeight, float deltaX=0f, float deltaY=0f) {
			this.gridHeight = gridHeight;
			this.gridWidth = gridWidth;
			this.cellHeight = cellHeight;
			this.cellWidth = cellWidth;
			this.deltaX = deltaX;
			this.deltaY = deltaY;
			this.maxSpawn = gridWidth * gridHeight - 1;
		}

		override public void Apply(Particle particle) {
			var x = particle.Position.X + (this.lastSpawn % this.gridWidth) * this.cellWidth + Simulator.Instance.Random() * deltaX - deltaX/2f;
			var y = particle.Position.Y + (int)(this.lastSpawn / this.gridHeight) * this.cellHeight + Simulator.Instance.Random() * deltaY - deltaY/2f;
			particle.Position = new Vector2f(x, y);
			if (++this.lastSpawn > this.maxSpawn)
				this.lastSpawn = 0;
		}
	}

	public class LightSpawner: Spawner {
		private Color color;
		private int radius;

		public LightSpawner(Color color, int radius) {
			this.color = color;
			this.radius = radius;
		}

		override public void Apply(Particle particle) {
			Simulator.Instance.AddLight (particle, this.radius, this.color);
		}
	}
}

