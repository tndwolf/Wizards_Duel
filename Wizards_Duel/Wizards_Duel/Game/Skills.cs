using System;
using SFML.Graphics;
using WizardsDuel.Utils;
using WizardsDuel.Io;

namespace WizardsDuel.Game
{
	public class Skill: IComparable {
		public Skill() {
			this.Range = 1;
		}

		public int CoolDown { get; set; }

		public string IconTexture { get; set; }

		public IntRect IconRect { get; set; }

		public string ID { get; set; }

		public string MouseIconTexture { get; set; }

		public IntRect MouseIconRect { get; set; }

		public string Name { get; set; }

		int roundsToGo = 0;
		public int RoundsToGo { 
			get { return this.roundsToGo; }
			set { this.roundsToGo = (value < 0) ? 0 : value; }
		}

		public int Priority { get; set; }

		public int Range { get; set; }

		public SkillScript OnEmptyScript { get; set; }

		public SkillScript OnSelfScript { get; set; }

		public SkillScript OnTargetScript { get; set; }

		public void OnEmpty (Entity actor, int gx, int gy) {
			this.RoundsToGo = this.CoolDown;
			this.OnEmptyScript.Run(actor, null, gx, gy);
		}

		public void OnSelf (Entity actor) {
			this.RoundsToGo = this.CoolDown;
			this.OnSelfScript.Run(actor, actor, 0, 0);
		}

		public void OnTarget (Entity actor, Entity target) {
			this.RoundsToGo = this.CoolDown;
			Logger.Debug ("Skill", "OnTarget", actor.ID + " vs " + target.ID);
			this.OnTargetScript.Run(actor, target, 0, 0);
		}

		#region OutputUserInterface
		internal Icon OutIcon { get; set; }
		internal SolidBorder Border { get; set; }
		internal DamageBarDecorator DamageBar { get; set; }
		internal bool Show { get; set; }
		#endregion

		#region IComparable implementation
		public int CompareTo (object obj) {
			try {
				//var comp = obj as Skill;
				return (obj as Skill).Priority.CompareTo(this.Priority);
			} catch (Exception ex) {
				Logger.Debug ("OutObject", "CompareTo", "Trying to compare a wrong object" + ex.ToString());
				return 0;
			}
		}
		#endregion
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
		public DamageSkillScript(int damage = 1, string damageType = Simulator.DAMAGE_TYPE_UNTYPED) {
			this.Damage = damage;
			this.DamageType = damageType;
		}

		public int Damage { get; set; }

		public string DamageType { get; set; }

		override public void Run (Entity actor, Entity target, int gx, int gy) {
			//Logger.Debug ("DamageSkillScript", "Run", actor.ID + " vs " + target.ID);
			Simulator.Instance.CreateParticleOn (this.SelfParticle, actor);
			//Logger.Debug ("DamageSkillScript", "Run", "Actor animation: " + this.SelfAnimation);
			actor.OutObject.SetAnimation (this.SelfAnimation);
			Simulator.Instance.CreateParticleOn (this.TargetParticle, target);
			//Logger.Debug ("DamageSkillScript", "Run", "Target animation: " + this.TargetAnimation);
			target.OutObject.SetAnimation (this.TargetAnimation);
			var bonusDamage = 0;
			if (this.DamageType == Simulator.DAMAGE_TYPE_PHYSICAL) {
				bonusDamage += actor.GetVar ("STRENGTH");
			}
			Simulator.Instance.Attack (actor.ID, target.ID, this.Damage + bonusDamage, this.DamageType);
		}
	}

	public class EffectSkillScript: SkillScript {
		public Effect Effect { get; set; }

		override public void Run (Entity actor, Entity target, int gx, int gy) {
			Simulator.Instance.CreateParticleOn (this.SelfParticle, actor);
			actor.OutObject.SetAnimation (this.SelfAnimation);
			Simulator.Instance.CreateParticleOn (this.TargetParticle, target);
			target.OutObject.SetAnimation (this.TargetAnimation);
			Simulator.Instance.AddEffect (target.ID, this.Effect.Clone);
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

