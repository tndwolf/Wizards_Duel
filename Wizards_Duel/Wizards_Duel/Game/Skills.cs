using System;
using SFML.Graphics;
using WizardsDuel.Utils;

namespace WizardsDuel.Game
{
	public class Skill {
		public int CoolDown { get; set; }

		public string IconTexture { get; set; }

		public IntRect IconRect { get; set; }

		public string ID { get; set; }

		public string MouseIconTexture { get; set; }

		public IntRect MouseIconRect { get; set; }

		public string Name { get; set; }

		public int RoundsToGo { get; set; }

		public int Priority { get; set; }

		public SkillScript OnEmptyScript { get; set; }

		public SkillScript OnSelfScript { get; set; }

		public SkillScript OnTargetScript { get; set; }

		public void OnEmpty (Entity actor, int gx, int gy) {
			this.OnEmptyScript.Run(actor, null, gx, gy);
		}

		public void OnSelf (Entity actor) {
			this.OnSelfScript.Run(actor, null, 0, 0);
		}

		public void OnTarget (Entity actor, Entity target) {
			Logger.Debug ("Skill", "OnTarget", actor.ID + " vs " + target.ID);
			this.OnTargetScript.Run(actor, target, 0, 0);
		}
	}

	public class SkillScript {
		public string SelfAnimation { get; set; }
		public string SelfParticle { get; set; }
		public string TargetAnimation { get; set; }
		public string TargetParticle { get; set; }

		virtual public void Run (Entity actor, Entity target, int gx, int gy) {
			Logger.Debug ("SkillScript", "Run", "Not doing anything");
		}
	}

	public class DamageSkillScript: SkillScript {
		public int Damage { get; set; }

		public string DamageType { get; set; }

		override public void Run (Entity actor, Entity target, int gx, int gy) {
			Logger.Debug ("DamageSkillScript", "Run", actor.ID + " vs " + target.ID);

			Simulator.Instance.CreateParticleOn (this.SelfParticle, actor);
			Logger.Debug ("DamageSkillScript", "Run", "Actor animation: " + this.SelfAnimation);
			actor.OutObject.SetAnimation (this.SelfAnimation);
			Simulator.Instance.CreateParticleOn (this.TargetParticle, target);
			Logger.Debug ("DamageSkillScript", "Run", "Target animation: " + this.TargetAnimation);
			target.OutObject.SetAnimation (this.TargetAnimation);
			Simulator.Instance.Attack (actor.ID, target.ID);
		}
	}

	public class SpawnSkillScript: SkillScript {
		public string SpawnTemplateId { get; set; }

		override public void Run (Entity actor, Entity target, int gx, int gy) {
			Logger.Debug ("SpawnSkillScript", "Run", "Spawning " + SpawnTemplateId);
			
			Simulator.Instance.CreateParticleOn (this.SelfParticle, actor);
			Logger.Debug ("SpawnSkillScript", "Run", "Actor animation: " + this.SelfAnimation);
			actor.OutObject.SetAnimation (this.SelfAnimation);
			Simulator.Instance.CreateParticleAt (this.TargetParticle, gx, gy);
			Simulator.Instance.CreateObject (SpawnTemplateId, gx, gy);
		}
	}
}

