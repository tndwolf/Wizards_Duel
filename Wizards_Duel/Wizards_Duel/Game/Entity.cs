using System;
using SFML.Graphics;
using System.Collections.Generic;
using WizardsDuel.Utils;
using WizardsDuel.Io;

namespace WizardsDuel.Game {
	public class Entity: EventObject {
		public string DeathAnimation = String.Empty;
		public Color DeathMain = Color.Red;
		public IntRect DeathRect = new IntRect (0, 0, 1, 1);
		public Color DeathSecundary = Color.Black;
		public List<Effect> effects = new List<Effect> ();
		public WizardsDuel.Io.OutObject OutObject = null;
		public List<Skill> skills = new List<Skill> ();
		public List<string> Tags = new List<string> ();
		public int Threat = 0;
		public Dictionary<string, int> Vars = new Dictionary<string, int> ();
		public int X = 0;
		public int Y = 0;

		public Entity (string id, string templateId = "") {
			this.ID = id;
			this.TemplateID = templateId;
			this.AI = new ArtificialIntelligence ();
			this.Visible = true;
			this.Health = 1;
			this.MaxHealth = 1;
			this.SpeedFactor = 1f;
		}

		public void AddEffect (Effect effect) {
			var existing = this.effects.Find (x => x.ID == effect.ID);
			if (existing != null) {
				existing.Duration = Math.Max (effect.Duration, existing.Duration);
				// TODO check for the maximum strength of effect
			}
			else {
				this.effects.Add (effect);
				effect.Parent = this;
				effect.OnAdded ();
			}
		}

		public void AddSkill (Skill skill) {
			this.skills.Add (skill);
			this.skills.Sort ();
		}

		public void AddTag (string tag) {
			if (!this.Tags.Contains (tag)) {
				this.Tags.Add (tag);
			}
		}

		private ArtificialIntelligence _AI;

		/// <summary>
		/// Gets or sets the Artificial Intelligence Controlling the object.
		/// By default the object is inert.
		/// </summary>
		/// <value>AI</value>
		public ArtificialIntelligence AI {
			get { return this._AI; }
			set { 
				value.Parent = this; 
				this._AI = value;
				//this._AI.onCreate ();
			}
		}

		/// <summary>
		/// Gets the maximum range of skills currently usable taking
		/// into account Cooldowns and Status
		/// </summary>
		/// <value>The current active range.</value>
		public int CurrentActiveRange {
			get {
				var res = 1;
				foreach (var s in this.skills) {
					if (s.RoundsToGo < 1 && s.Range > res) {
						res = s.Range;
					}
				}
				return res;
			}
		}

		public void Damage (int howMuch, string type) {
			Logger.Debug ("Entity", "Damage", "Receiving " + type + " damage: " + howMuch.ToString () + " vs health " + this.Health.ToString ());
			/*foreach (var skill in this.skills) {
				howMuch = skill.ProcessDamage (howMuch, type);
			}*/
			foreach (var effect in this.effects) {
				howMuch = effect.ProcessDamage (howMuch, type);
			}
			this.AI.OnDamage (ref howMuch, type);
			if (howMuch > 0 && type == Simulator.DAMAGE_TYPE_PHYSICAL) {
				Simulator.Instance.CreateParticleOn (Simulator.BLOOD_PARTICLE, this);
			}
			Logger.Debug ("Entity", "Damage", "After process " + type + " damage: " + howMuch.ToString () + " vs health " + this.Health.ToString ());
			this.Health -= howMuch;
			Logger.Debug ("Entity", "Damage", "Receiving " + type + " damage: " + howMuch.ToString () + " vs health " + this.Health.ToString ());
			if (this.DamageBar != null) {
				this.DamageBar.Level = 1f - (float)this.Health / (float)this.MaxHealth;
			}
			if (this.Health < 1) {
				Simulator.Instance.Kill (this);
			}
		}

		/// <summary>
		/// If set the Enity is part of the environment, it will not act and cannot
		/// be interacted with
		/// </summary>
		/// <value><c>true</c> if dressing; otherwise, <c>false</c>.</value>
		public bool Dressing  { get; set; }

		/// <summary>
		/// Gets or sets the faction of the Entity.
		/// </summary>
		/// <value>The faction.</value>
		public string Faction  { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="WizardsDuel.Game.Entity"/> is "stopped in time".
		/// </summary>
		/// <value><c>true</c> if frozen; otherwise, <c>false</c>.</value>
		public bool Frozen { 
			get { return this.frozen; } 
			set {
				this.frozen = value;
				// XXX this to avoid freezing an object whiel animating
				// thus blocking the game waiting for its idle cycle
				// notice thta the animation should be frozen in the current state
				// the important thing is that the outobject is set to be in idle
				this.OutObject.SetAnimation (this.OutObject.IdleAnimation);
			}
		}

		private bool frozen;

		/// <summary>
		/// Return the first skills that matches the combo or null if none is defined.
		/// Note that combo is sorted by this function
		/// </summary>
		/// <returns>The combo skill.</returns>
		/// <param name="combo">Combo.</param>
		public Skill GetComboSkill(List<string> combo) {
			combo.Sort ();
			foreach (var s in this.skills) {
				Logger.Debug ("Entity", "GetComboSkill", "Comparing: " + String.Join(",", s.Combo.ToArray()) + " vs " + String.Join(",", combo.ToArray()));
				if (System.Linq.Enumerable.SequenceEqual (combo, s.Combo)) {
					return s;
				}
			}
			return null;
		}

		public Skill GetPrioritySkillInRange (int range) {
			// note that skills are sorted by priority when added
			return this.skills.Find (x => x.RoundsToGo < 1 && x.Range <= range);
		}

		public int GetVar (string name, int def = 0) {
			int res;
			if (this.Vars.TryGetValue (name, out res) == true) {
				return res;
			}
			else {
				return def;
			}
		}

		public bool HasTag (string tag) {
			return this.Tags.Contains (tag);
		}

		public int Health {
			get { return this.GetVar ("HEALTH"); }
			set { this.SetVar ("HEALTH", value); }
		}

		public string ID {
			get;
			protected set;
		}

		public void Kill () {
			/*foreach (var skill in this.skills) {
				howMuch = skill.OnDeath ();
			}*/
			this.AI.OnDeath ();
		}

		public int LastSeen { get; set; }

		public int MaxHealth { get; set; }

		public Skill PrioritySkill {
			get {
				// note that skills are sorted by priority when added
				return this.skills.Find (x => x.RoundsToGo < 1);
			}
		}

		public void RemoveEffect (Effect effect) {
			this.effects.Remove (effect);
			effect.OnRemoved ();
		}

		public void RemoveTag (string tag) {
			this.Tags.Remove (tag);
		}

		public void SetVar (string name, int value) {
			this.Vars [name] = value;
		}

		public float SpeedFactor { get; set; }

		public bool Static { get; set; }

		public string TemplateID { get; protected set; }

		#region EventObject implementation

		public void Run (Simulator sim, EventManager ed) {
			if (this.ID == Simulator.PLAYER_ID) {
				ed.AcceptEvent = true;
			}
			if (!(this.ID == Simulator.PLAYER_ID && ed.WaitingForUser == true)) {
				// must check, otherwise the effects on players will be repeated
				// at each query for user inputs
				foreach (var effect in this.effects) {
					effect.OnRound ();
					if (effect.RemoveMe == true) {
						effect.OnRemoved ();
					}
				}
				this.effects.RemoveAll (x => x.RemoveMe == true);
				foreach (var skill in this.skills) {
					skill.RoundsToGo -= 1;
					if (skill.DamageBar != null)
						skill.DamageBar.Level = (float)skill.RoundsToGo / (float)skill.CurrentCoolDown;
				}
			}
			if (this.Frozen == false && this.Dressing == false && this.Health > 0) {
				this.AI.OnRound ();
			}
			if (this.ID == Simulator.PLAYER_ID && ed.WaitingForUser == true) {
				return;
			}
			else {
				Logger.Debug ("Entity", "Run", "Ran " + this.ID + " at initiative " + sim.InitiativeCount.ToString ());
				this.HasEnded = true;
				this.Initiative += (int)(Simulator.ROUND_LENGTH / this.SpeedFactor);
			}
			ed.AcceptEvent = false;
			//*
			if (sim.world.InLos (this.X, this.Y)) {
				this.LastSeen = this.Initiative;
				if (this.Visible == false) {
					//Logger.Debug ("Entity", "Run", "Showing " + this.ID);
					this.Visible = true;
					this.OutObject.AddAnimator (new WizardsDuel.Io.ColorAnimation (Color.Transparent, Color.White, 300));
				}
			}
			else {
				if (this.Visible == true && this.LastSeen + Simulator.ROUND_LENGTH < this.Initiative) {
					this.Visible = false;
					this.OutObject.AddAnimator (new WizardsDuel.Io.ColorAnimation (Color.White, Color.Transparent, 300));
				}
			}//*/
		}

		bool hasEnded = false;

		public bool HasEnded {
			get { 
				if (this.Dressing == true || this.Static == true) {
					return true;
				}
				else {
					return this.hasEnded; //&& this.OutObject.IsInIdle;
				}
			}
			set { 
				this.hasEnded = value;
			}
		}

		/// <summary>
		/// Gets or sets the current absolute initiative count.
		/// </summary>
		/// <value>The initiative.</value>
		public int Initiative { get; set; }

		public bool IsAnimating {
			get { 
				return this.Visible && !this.OutObject.IsInIdle;
			}
		}

		/// <summary>
		/// Compares objects to sort them based on Initiative.
		/// </summary>
		/// <returns>The to.</returns>
		/// <param name="obj">Object.</param>
		public int CompareTo (object obj) {
			try {
				var comp = (EventObject)obj;
				return this.Initiative.CompareTo (comp.Initiative);
			}
			catch (Exception ex) {
				Logger.Debug ("Entity", "CompareTo", "Trying to compare a wrong object" + ex.ToString ());
				return 0;
			}
		}

		public bool Visible { get; set; }

		#endregion

		#region OutputUserInterface

		internal Icon OutIcon { get; set; }

		internal SolidBorder Border { get ; set; }

		internal DamageBarDecorator DamageBar { get; set; }

		#endregion
	}
}

