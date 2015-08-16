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
			this.AI.onRound ();
			if (this.ID == Simulator.PLAYER_ID && ed.WaitingForUser == true) {
				return;
			} else {
				Logger.Debug ("Entity", "Run", "Ran " + this.ID + " at initiative " + sim.InitiativeCount.ToString());
				this.HasEnded = true;
				this.Initiative += Simulator.ROUND_LENGTH;
			}
			/*if (sim.world.InLos (this.X, this.Y)) {
					this.OutObject.Alpha = 255;
				} else {
					this.OutObject.Alpha = 0;
				}*/
		}

		bool hasEnded = false;
		public bool HasEnded {
			get { 
				if (this.Dressing == true || this.Static == true) {
					return true;
				} else {
					return this.hasEnded; //&& this.OutObject.IsInIdle;
				}
			}
			set { 
				this.hasEnded = value;
			}
		}

		public int Initiative { get; set; }

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

