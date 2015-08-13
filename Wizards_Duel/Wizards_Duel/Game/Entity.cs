using System;
using SFML.Graphics;
using System.Collections.Generic;
using WizardsDuel.Utils;

namespace WizardsDuel.Game
{
	public class Entity: EventObject {
		public string DeathAnimation = String.Empty;
		public Color DeathMain = Color.Red;
		public IntRect DeathRect = new IntRect (0, 0, 1, 1);
		public Color DeathSecundary = Color.Black;
		public WizardsDuel.Io.OutObject OutObject = null;
		public Dictionary<string, int> Vars = new Dictionary<string, int>();
		public int X = 0;
		public int Y = 0;

		public Entity(string id, string templateId = "") {
			this.ID = id;
			this.TemplateID = templateId;
			this.AI = new ArtificialIntelligence ();
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
		/// If set the Enity is part of the environment, it will not act and cannot
		/// be interacted with
		/// </summary>
		/// <value><c>true</c> if dressing; otherwise, <c>false</c>.</value>
		public bool Dressing {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the faction of the Entity.
		/// </summary>
		/// <value>The faction.</value>
		public string Faction {
			get;
			set;
		}

		public int GetVar(string name, int def=0) {
			int res;
			if (this.Vars.TryGetValue (name, out res) == true) {
				return res;
			} else {
				return def;
			}
		}

		public string ID {
			get;
			protected set;
		}

		public void SetAI(ArtificialIntelligence ai, bool runOnCreateScript = false) {
			ai.Parent = this;
			this._AI = ai;
			if (runOnCreateScript) {
				this._AI.onCreate ();
			}
		}

		public void SetVar(string name, int value) {
			this.Vars[name] = value;
		}

		public bool Static {
			get;
			set;
		}

		public string TemplateID {
			get;
			protected set;
		}

		#region EventObject implementation
		public void Run (Simulator sim, EventManager ed) {
			if (this.Dressing || this.Static) {
				this.AI.onRound ();
				this.HasStarted = true;
				this.HasEnded = true;
			} else if (this.ID == Simulator.PLAYER_ID) {
				//Logger.Debug ("Entity", "Run", "Running PLAYER event");
				if (ed.RunUserEvent () == true) {
					Logger.Debug ("Entity", "Run", "Running PLAYER event at initiative " + sim.InitiativeCount.ToString());
					this.HasStarted = true;
					this.HasEnded = true;
				} else {
					Logger.Debug ("Entity", "Run", "NOT Running PLAYER event at initiative " + sim.InitiativeCount.ToString());
					this.HasStarted = false;
					this.HasEnded = false;
				}
			} else {
				//Logger.Debug ("Entity", "Run", "Running NPC event");
				/*var player = sim.GetObject (Simulator.PLAYER_ID);
				var dx = Math.Sign(player.X - this.X);
				var dy = Math.Sign(player.Y - this.Y);
				sim.CanShift(this.ID, dx, dy, true);*/
				this.AI.onRound ();

				this.HasStarted = true;
				this.HasEnded = true;
			}
		}

		bool hasEnded = false;
		public bool HasEnded {
			get { return this.hasEnded && this.OutObject.IsInIdle; }
			set { this.hasEnded = value; }
		}

		public bool HasStarted { get; set; }

		public int Initiative { get; set; }

		public bool IsWaiting { 
			get { 
				if (this.ID == Simulator.PLAYER_ID) {
					return !Simulator.Instance.IsUserEventInQueue ();
				} else {
					return false;
				}
			}
		}

		public int UpdateInitiative () {
			this.Initiative += 10;
			return this.Initiative;
		}

		public int CompareTo (object obj) {
			try {
				var comp = (EventObject) obj;
				return this.Initiative.CompareTo (comp.Initiative);
			} catch (Exception ex) {
				Logger.Debug ("Entity", "CompareTo", "Trying to compare a wrong object" + ex.ToString());
				return 0;
			}
		}
		#endregion
	}
}

