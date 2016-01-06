using System;
using SFML.Graphics;
using WizardsDuel.Utils;
using WizardsDuel.Io;
using System.Collections.Generic;

namespace WizardsDuel.Game {
	public class Skill: IComparable {
		public Skill () {
			this.Range = 1;
			this.Combo = new List<string>();
		}

		public List<string> Combo { get; set; }

		public int CoolDown { get; set; }

		public int CurrentCoolDown {
			// XXX This way combo spells have no effect on cooldowns
			// XXX remove this to have the cooldown derived from the combo skill
			get { return this.CoolDown; } 
			set {}
		}

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

		public List<SkillBehaviour> OnEmptyScript { get; set; }

		public List<SkillBehaviour> OnSelfScript { get; set; }

		public List<SkillBehaviour> OnTargetScript { get; set; }

		public bool OnEmpty (Entity actor, int gx, int gy) {
			if (this.OnEmptyScript != null && this.OnEmptyScript.Count > 0) {
				Logger.Debug ("Skill", "OnEmpty", actor.ID + " " + OnEmptyScript.ToString ());
				if (this.OnEmptyScript[0].Run (actor, null, gx, gy)) {
					this.RoundsToGo = this.CoolDown;
					this.CurrentCoolDown = this.CoolDown;
					return true;
				}
				else {
					return false;
				}
			}
			else {
				return false;
			}
		}

		public bool OnSelf (Entity actor) {
			if (this.OnSelfScript != null && this.OnSelfScript.Count > 0) {
				Logger.Debug ("Skill", "OnSelf", actor.ID + " " + OnSelfScript.ToString ());
				if (this.OnSelfScript[0].Run (actor, actor, -1, -1)) {
					this.RoundsToGo = this.CoolDown;
					this.CurrentCoolDown = this.CoolDown;
					return true;
				}
				else {
					return false;
				}
			}
			else {
				return false;
			}
		}

		public bool OnTarget (Entity actor, Entity target) {
			if (this.OnTargetScript != null && this.OnTargetScript.Count > 0) {
				Logger.Debug ("Skill", "OnTarget", actor.ID + " vs " + target.ID);
				var done = false;
				foreach (var behaviour in this.OnTargetScript) {
					Logger.Debug ("Skill", "OnTarget", "Running " + behaviour.ToString());
					if (behaviour.Run (actor, target, 0, 0) == true) {
						this.RoundsToGo = this.CoolDown;
						this.CurrentCoolDown = this.CoolDown;
						done = true;
					}
				}
				return done;
				/*if (this.OnTargetScript[0].Run (actor, target, 0, 0)) {
					this.RoundsToGo = this.CoolDown;
					this.CurrentCoolDown = this.CoolDown;
					return true;
				}
				else {
					return false;
				}*/
			}
			else {
				return false;
			}
		}

		#region OutputUserInterface
		internal ButtonIcon OutIcon { get; set; }

		internal SolidBorder Border { get; set; }

		internal DamageBarDecorator DamageBar { get; set; }

		internal bool Show { get; set; }
		#endregion

		#region IComparable implementation
		public int CompareTo (object obj) {
			try {
				//var comp = obj as Skill;
				return (obj as Skill).Priority.CompareTo (this.Priority);
			}
			catch (Exception ex) {
				Logger.Debug ("OutObject", "CompareTo", "Trying to compare a wrong object" + ex.ToString ());
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
		public DamageBehaviour (int damage = 1, string damageType = Simulator.DAMAGE_TYPE_UNTYPED) {
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
			}
			else {
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
			}
			else {
				return false;
			}
		}
	}

	public class SpawnBehaviour: SkillBehaviour {
		internal List<string> templateIds = new List<string> ();

		/// <summary>
		/// Gets or sets a value indicating whether the spawned entity is independent from its parent
		/// (i.e. it does not count against the spawn limit)
		/// </summary>
		/// <value><c>true</c> if independent; otherwise, <c>false</c>.</value>
		public bool Independent { get; set; }

		public bool Loop { get; set; }

		public string SpawnTemplateId { 
			get { return this.templateIds [Simulator.Instance.Random (this.templateIds.Count)]; }
			set { this.templateIds.Add (value); }
		}

		override public bool Run (Entity actor, Entity target, int gx, int gy) {
			if (actor.ID != Simulator.PLAYER_ID && Simulator.Instance.InitiativeCount < Simulator.ROUND_LENGTH) {
				return false;
			}
			if (gx < 0 || gy < 0) {
				gx = actor.X;
				gy = actor.Y;
			}
			if (target != null) {
				gx = target.X;
				gy = target.Y;
			}
			if (this.Loop) {
				Logger.Debug ("SpawnSkillScript", "Run", "Searching for old siblings...");
				var siblings = Simulator.Instance.GetByParent (actor.ID);
				if (siblings.Count > 0) {
					Logger.Debug ("SpawnSkillScript", "Run", "Killing old sibling " + siblings [0].ID);
					Simulator.Instance.CreateParticleAt ("P_SPAWN", siblings [0].X, siblings [0].Y);
					Simulator.Instance.DestroyObject (siblings [0].ID);
				}
			}
			if (/*Simulator.Instance.IsSafeToWalk (actor, gx, gy)*/this.Independent || actor.GetVar("SPAWNS") < actor.GetVar("MAX_SPAWNS")) {
				Logger.Debug ("SpawnSkillScript", "Run", "Spawning " + SpawnTemplateId);
				Simulator.Instance.CreateParticleOn (this.SelfParticle, actor);
				Logger.Debug ("SpawnSkillScript", "Run", "Actor animation: " + this.SelfAnimation);
				actor.OutObject.SetAnimation (this.SelfAnimation);
				/*if (target != null) {
					Simulator.Instance.CreateParticleOn (this.TargetParticle, target);
				}
				else {
					Simulator.Instance.CreateParticleAt (this.TargetParticle, gx, gy);
				}*/
				var createdId = Simulator.Instance.CreateObject (SpawnTemplateId, gx, gy);

				var created = Simulator.Instance.GetObject (createdId);
				Simulator.Instance.CreateParticleOn (this.TargetParticle, created);
				if (created != null && !this.Independent) {
					created.SpawnedBy = actor.ID;
					actor.ChangeVar ("SPAWNS", 1);
				}
				return true;
			}
			else {
				Logger.Debug ("SpawnSkillScript", "Run", "Not spawning " + SpawnTemplateId);
				return false;
			}
		}
	}
}

