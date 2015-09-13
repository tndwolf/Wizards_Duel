using System;
using SFML.Graphics;
using WizardsDuel.Utils;
using WizardsDuel.Io;
using System.Collections.Generic;

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

		public SkillBehaviour OnEmptyScript { get; set; }

		public SkillBehaviour OnSelfScript { get; set; }

		public SkillBehaviour OnTargetScript { get; set; }

		public bool OnEmpty (Entity actor, int gx, int gy) {
			if (this.OnEmptyScript != null) {
				Logger.Debug ("Skill", "OnEmpty", actor.ID + " " + OnEmptyScript.ToString());
				if (this.OnEmptyScript.Run (actor, null, gx, gy)) {
					this.RoundsToGo = this.CoolDown;
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}

		public bool OnSelf (Entity actor) {
			if (this.OnSelfScript != null) {
				Logger.Debug ("Skill", "OnSelf", actor.ID + " " + OnSelfScript.ToString());
				if (this.OnSelfScript.Run (actor, actor, 0, 0)) {
					this.RoundsToGo = this.CoolDown;
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}

		public bool OnTarget (Entity actor, Entity target) {
			if (this.OnTargetScript != null) {
				this.RoundsToGo = this.CoolDown;
				Logger.Debug ("Skill", "OnTarget", actor.ID + " vs " + target.ID);
				if (this.OnTargetScript.Run (actor, target, 0, 0)) {
					this.RoundsToGo = this.CoolDown;
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
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

	public class SkillBehaviour {
		public string SelfAnimation { get; set; }
		public string SelfParticle { get; set; }
		public string TargetAnimation { get; set; }
		public string TargetParticle { get; set; }

		virtual public bool Run (Entity actor, Entity target, int gx, int gy) {
			Logger.Debug ("SkillScript", "Run", "Not doing anything");
			return true;
		}
	}

	public class DamageBehaviour: SkillBehaviour {
		public DamageBehaviour(int damage = 1, string damageType = Simulator.DAMAGE_TYPE_UNTYPED) {
			this.Damage = damage;
			this.DamageType = damageType;
		}

		public int Damage { get; set; }

		public string DamageType { get; set; }

		override public bool Run (Entity actor, Entity target, int gx, int gy) {
			if (actor != null && target != null) {
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
				return true;
			} else {
				return false;
			}
		}
	}

	public class EffectSkillScript: SkillBehaviour {
		public Effect Effect { get; set; }

		override public bool Run (Entity actor, Entity target, int gx, int gy) {
			if (target != null) {
				Simulator.Instance.CreateParticleOn (this.SelfParticle, actor);
				actor.OutObject.SetAnimation (this.SelfAnimation);
				Simulator.Instance.CreateParticleOn (this.TargetParticle, target);
				target.OutObject.SetAnimation (this.TargetAnimation);
				Simulator.Instance.AddEffect (target.ID, this.Effect.Clone);
				return true;
			} else {
				return false;
			}
		}
	}

	public class SpawnBehaviour: SkillBehaviour {
		internal List<string> templateIds = new List<string> ();

		public string SpawnTemplateId { 
			get { return this.templateIds [Simulator.Instance.Random (this.templateIds.Count)]; }
			set { this.templateIds.Add (value); }
		}

		override public bool Run (Entity actor, Entity target, int gx, int gy) {
			if (Simulator.Instance.IsSafeToWalk (actor, gx, gy)) {
				Logger.Debug ("SpawnSkillScript", "Run", "Spawning " + SpawnTemplateId);
				Simulator.Instance.CreateParticleOn (this.SelfParticle, actor);
				Logger.Debug ("SpawnSkillScript", "Run", "Actor animation: " + this.SelfAnimation);
				actor.OutObject.SetAnimation (this.SelfAnimation);
				Simulator.Instance.CreateParticleAt (this.TargetParticle, gx, gy);
				Simulator.Instance.CreateObject (SpawnTemplateId, gx, gy);
				return true;
			} else {
				return false;
			}
		}
	}
}

