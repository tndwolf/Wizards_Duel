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
using System.Globalization;
using System.Xml;
using SFML.Graphics;
using SFML.Window;
using WizardsDuel.Io;
using WizardsDuel.Utils;

namespace WizardsDuel.Game {
	static public class GameFactory {
		static XmlDocument xdoc;

		static public World LoadGame (string xmlfile) {
			GameFactory.xdoc = new XmlDocument ();
			GameFactory.xdoc.Load (xmlfile);

			try {
				XmlNode mapNode = GameFactory.xdoc.SelectSingleNode ("//map");
				int gridWidth = Convert.ToInt32 (mapNode.Attributes.GetNamedItem ("gridWidth").Value);
				int gridHeight = Convert.ToInt32 (mapNode.Attributes.GetNamedItem ("gridHeight").Value);

				string map = mapNode.InnerText;
				var maprows = map.Split (new string[] { Environment.NewLine, "\t", " ", "," }, StringSplitOptions.RemoveEmptyEntries);

				var res = new World ();

				res.worldView = UiFactory.LoadPage (xmlfile);

				/*res.worldView = new WorldView(gridWidth, gridHeight, cellWidth, cellHeight, scale);
				var tilemask = xdoc.SelectSingleNode("//tileMasks[@id='tilemask_floor_01']");
				UiFactory.LoadTilemask(tilemask, LayerType.FLOOR, res.worldView);*/
				Logger.Debug ("GameFactory", "LoadGame", "Build world, initializing dungeon");
				res.SetMap (maprows);

				return res;
			}
			catch (Exception ex) {
				Logger.Error ("GameFactory", "LoadGame", ex.ToString ());
				var res = new World ();
				res.worldView = new WorldView ();
				return res;
			}
		}

		static public AnimationDefinition LoadAnimationDefinition (XmlNode animationRoot) {
			AnimationDefinition res = new AnimationDefinition ();
			var frames = animationRoot.SelectNodes ("./frame");
			for (int f = 0; f < frames.Count; f++) {
				var frame = new AnimationFrame (
					            XmlUtilities.GetInt (frames [f], "x"),
					            XmlUtilities.GetInt (frames [f], "y"),
					            XmlUtilities.GetInt (frames [f], "width"),
					            XmlUtilities.GetInt (frames [f], "height"),
					            XmlUtilities.GetInt (frames [f], "duration"),
					            XmlUtilities.GetString (frames [f], "sfx")
				            );
				frame.offset = new Vector2f (
					XmlUtilities.GetFloat (frames [f], "offsetX"), 
					XmlUtilities.GetFloat (frames [f], "offsetY")
				);
				res.AddFrame (frame);
			}
			return res;
		}

		static public Entity LoadFromTemplate (string templateId, string assignedId) {
			try {
				XmlNode template = GameFactory.xdoc.SelectSingleNode ("//blueprint[@id='" + templateId + "']");
				var res = new Entity (assignedId, templateId);

				var properties = template.SelectSingleNode ("./properties");
				res.Faction = XmlUtilities.GetString (properties, "faction");
				res.Static = XmlUtilities.GetBool (properties, "static");
				res.Dressing = XmlUtilities.GetBool (properties, "dressing");
				res.Threat = XmlUtilities.GetInt (properties, "threat");
				var ai = XmlUtilities.GetStringArray (properties, "ai", true);
				switch (ai [0]) {
					case ArtificialIntelligence.ICE:
						res.AI = new IceAI ();
						break;
					case ArtificialIntelligence.LAVA_EMITTER:
						res.AI = new LavaEmitterAI ();
						break;
					case ArtificialIntelligence.LAVA:
						res.AI = new LavaAI ();
						if (ai.Length > 1 && ai[1] == "PERMANENT") {
							(res.AI as LavaAI).CanHarden = false;
						}
						break;
					case ArtificialIntelligence.MELEE:
						res.AI = new MeleeAI ();
						break;
					default:
						break;
				}
				var tags = XmlUtilities.GetStringArray (properties, "tags", true);
				foreach (var tag in tags) {
					res.AddTag (tag);
				}

				var icon = template.SelectSingleNode ("./icon");
				res.OutIcon = new Icon (XmlUtilities.GetString (icon, "texture"), XmlUtilities.GetIntRect (icon, "rect", new IntRect (0, 0, 0, 0)));

				var skills = template.SelectNodes ("./skill");
				for (int s = 0; s < skills.Count; s++) {
					var skillId = XmlUtilities.GetString (skills [s], "ref");
					var xmlSkill = GameFactory.xdoc.SelectSingleNode ("//skill[@id='" + skillId + "']");
					var skill = LoadSkill (xmlSkill);
					skill.Show = XmlUtilities.GetBool (skills [s], "show");
					res.AddSkill (skill);
				}

				var variables = template.SelectNodes ("./var");
				for (int v = 0; v < variables.Count; v++) {
					var vname = XmlUtilities.GetString (variables [v], "name");
					var vvalue = XmlUtilities.GetInt (variables [v], "value");
					res.Vars [vname] = vvalue;
					//res.Vars.Add(vname, vvalue);
					if (vname == "HEALTH") {
						res.MaxHealth = vvalue;
					}
					else if (vname == "SPEED") {
						res.SpeedFactor = (float)vvalue / 10f;
						Logger.Debug ("GameFactory", "LoadFromTemplate", "Speed factor of " + templateId + ": " + res.SpeedFactor.ToString ());
					}
				}

				var death = template.SelectSingleNode ("./death");
				res.DeathAnimation = XmlUtilities.GetString (death, "animation", String.Empty);
				res.DeathMain = XmlUtilities.GetColor (death, "color1", Color.Red);
				res.DeathSecundary = XmlUtilities.GetColor (death, "color2", Color.Black);
				res.DeathRect.Left = XmlUtilities.GetInt (death, "offsetX");
				res.DeathRect.Top = XmlUtilities.GetInt (death, "offsetY");
				res.DeathRect.Width = XmlUtilities.GetInt (death, "width", 1);
				res.DeathRect.Height = XmlUtilities.GetInt (death, "height", 1);

				XmlNode outTemplate = template.SelectSingleNode ("./output");
				res.OutObject = new OutObject (
					XmlUtilities.GetString (outTemplate, "texture"),
					new SFML.Graphics.IntRect (
						XmlUtilities.GetInt (outTemplate, "defaultX"), 
						XmlUtilities.GetInt (outTemplate, "defaultY"), 
						XmlUtilities.GetInt (outTemplate, "defaultW"), 
						XmlUtilities.GetInt (outTemplate, "defaultH")
					)
				);
				res.OutObject.LightColor = XmlUtilities.GetColor (outTemplate, "lightColor", Color.White);
				res.OutObject.LightRadius = XmlUtilities.GetInt (outTemplate, "lightRadius");
				res.OutObject.ZIndex = XmlUtilities.GetInt (outTemplate, "zIndex");

				var shadow = XmlUtilities.GetString (outTemplate, "shadow", String.Empty);
				if (shadow != String.Empty) {
					var shadowWidth = 24;
					var shadowHeight = (int)(shadowWidth / 3);
					var shadowSprite = new Sprite (IoManager.LoadTexture ("00_base_pc_fx.png"), new IntRect (576, 40, shadowWidth, shadowHeight));
					shadowSprite.Origin = new Vector2f (shadowWidth / 2, shadowHeight / 2);
					shadowSprite.Color = new Color (0, 0, 0, 96);
					shadowSprite.Scale = new Vector2f (2f, 2f);
					res.OutObject.Shadow = shadowSprite;
					//Simulator.Instance.CreateParticleOn(shadow,res);
				}

				var animations = outTemplate.SelectNodes ("./animation");
				for (int a = 0; a < animations.Count; a++) {
					var ad = LoadAnimationDefinition (animations [a]);
					res.OutObject.AddAnimation (XmlUtilities.GetString (animations [a], "name"), ad);
					//Logger.Debug ("GameFactory", "LoadFromTemplate", "Added animation " + XmlUtilities.GetString(animations[a], "name"));
				}

				Logger.Debug ("GameFactory", "LoadFromTemplate", "Built object " + templateId);

				return res;
			}
			catch (Exception ex) {
				Logger.Warning ("GameFactory", "LoadFromTemplate", ex.ToString ());
				return null;
			}
		}

		static public ParticleSystem LoadParticleFromTemplate (string templateId, float x, float y, ObjectsLayer layer, bool flip = false) {
			try {
				var xCoeff = flip ? -1f : 1f;
				var angleCoeff = flip ? 3.1415f : 0f;
				var res = new ParticleSystem (templateId);
				XmlNode template = GameFactory.xdoc.SelectSingleNode ("//particle[@id='" + templateId + "']");
				res.TTL = XmlUtilities.GetInt (template, "ttl");
				if (res.TTL < 0)
					res.TTL = int.MaxValue;
				res.Layer = layer;
				res.Position = new Vector2f (x, y);

				var emitters = template.SelectNodes ("./emitter");
				for (int e = 0; e < emitters.Count; e++) {
					Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New emitter: " + emitters [e].ToString ());
					var startDelay = XmlUtilities.GetInt (emitters [e], "startDelay");
					var emitter = new Emitter (res, startDelay);
					emitter.Offset = new Vector2f (
						XmlUtilities.GetFloat (emitters [e], "offsetX") * xCoeff,
						XmlUtilities.GetFloat (emitters [e], "offsetY")
					);
					emitter.ParticleTTL = XmlUtilities.GetInt (emitters [e], "particleTtl");
					emitter.SpawnCount = XmlUtilities.GetInt (emitters [e], "spawnCount");
					emitter.SpawnDeltaTime = XmlUtilities.GetInt (emitters [e], "spawnDeltaTime");
					emitter.TTL = XmlUtilities.GetInt (emitters [e], "ttl");
					if (emitter.TTL < 0)
						emitter.TTL = int.MaxValue;
					emitter.ZIndex = XmlUtilities.GetInt (emitters [e], "zIndex", 0);

					var children = emitters [e].ChildNodes;
					for (var c = 0; c < children.Count; c++) {
						switch (children [c].Name) {
							case "boxSpawner":
								emitter.AddVariator (new BoxSpawner (
									XmlUtilities.GetFloat (children [c], "width"),
									XmlUtilities.GetFloat (children [c], "height")
								));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New BoxSpawner: " + children [c].ToString ());
								break;

							case "burstSpawner":
								emitter.AddVariator (new BurstSpawner (
									XmlUtilities.GetFloat (children [c], "maxForce"),
									XmlUtilities.GetFloat (children [c], "minForce"),
									XmlUtilities.GetFloat (children [c], "maxAngle") + angleCoeff,
									XmlUtilities.GetFloat (children [c], "minAngle") + angleCoeff
								));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New BurstSpawner: " + children [c].ToString ());
								break;
						
							case "burstInSpawner":
								emitter.AddVariator (new BurstInSpawner (
									XmlUtilities.GetFloat (children [c], "maxRadius"),
									XmlUtilities.GetFloat (children [c], "minRadius"),
									XmlUtilities.GetFloat (children [c], "maxForce"),
									XmlUtilities.GetFloat (children [c], "minForce"),
									XmlUtilities.GetFloat (children [c], "maxAngle") + angleCoeff,
									XmlUtilities.GetFloat (children [c], "minAngle") + angleCoeff
								));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New BurstInSpawner: " + children [c].ToString ());
								break;

							case "colorAnimation":
								var buff = XmlUtilities.GetIntArray (children [c], "startColor");
								var startColor = new Color ((byte)buff [0], (byte)buff [1], (byte)buff [2]);
								if (buff.Length > 3)
									startColor.A = (byte)buff [3];
								buff = XmlUtilities.GetIntArray (children [c], "endColor");
								var endColor = new Color ((byte)buff [0], (byte)buff [1], (byte)buff [2]);
								if (buff.Length > 3)
									endColor.A = (byte)buff [3];
								emitter.AddAnimator (new ColorAnimation (startColor, endColor, XmlUtilities.GetInt (children [c], "duration")));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New ColorAnimator: " + children [c].ToString ());
								break;

							case "colorPicker":
								var cps = new ColorPickerSpawner ();
								var colors = children [c].SelectNodes ("./color");
								for (int i = 0; i < colors.Count; i++) {
									var color = XmlUtilities.GetIntArray (colors [i], "select");
									switch (color.Length) {
										case 3:
											cps.AddColor (new Color ((byte)color [0], (byte)color [1], (byte)color [2]));
											break;
										case 4:
											cps.AddColor (new Color ((byte)color [0], (byte)color [1], (byte)color [2], (byte)color [3]));
											break;
										default:
											break;
									}
								}
								emitter.AddVariator (cps);
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New ColorPickerSpawner: " + children [c].ToString ());
								break;

							case "fade":
								emitter.AddAnimator (new FadeAnimation (
									XmlUtilities.GetInt (children [c], "fadeInDuration"),
									XmlUtilities.GetInt (children [c], "duration"),
									XmlUtilities.GetInt (children [c], "fadeOutDuration")
								));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New FadeAnimator: " + children [c].ToString ());
								break;

							case "gravity":
								emitter.AddAnimator (new GravityAnimation (new Vector2f (
									XmlUtilities.GetFloat (children [c], "forceX") * xCoeff, 
									XmlUtilities.GetFloat (children [c], "forceY")
								)));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New GravityAnimation: " + children [c].ToString ());
								break;
						
							case "gridSpawner":
								emitter.AddVariator (new GridSpawner (
									XmlUtilities.GetInt (children [c], "gridWidth"),
									XmlUtilities.GetInt (children [c], "gridHeight"),
									XmlUtilities.GetFloat (children [c], "cellWidth") * xCoeff,
									XmlUtilities.GetFloat (children [c], "cellHeight"),
									XmlUtilities.GetFloat (children [c], "deltaX", 0f),
									XmlUtilities.GetFloat (children [c], "deltaY", 0f)
								));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New GridSpawner: " + children [c].ToString ());
								break;

							case "lightSpawner":
								buff = XmlUtilities.GetIntArray (children [c], "color");
								var lightColor = new Color ((byte)buff [0], (byte)buff [1], (byte)buff [2]);
								if (buff.Length > 3)
									lightColor.A = (byte)buff [3];
								emitter.AddVariator (new LightSpawner (lightColor, XmlUtilities.GetInt (children [c], "radius")));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New LightSpawner: " + children [c].ToString ());
								break;

							case "particleTemplate":
								if (children [c].HasChildNodes) {
									emitter.AddParticleTemplate (
										XmlUtilities.GetString (children [c], "texture"),
										XmlUtilities.GetInt (children [c], "x"),
										XmlUtilities.GetInt (children [c], "y"),
										XmlUtilities.GetInt (children [c], "width"),
										XmlUtilities.GetInt (children [c], "height"),
										XmlUtilities.GetFloat (children [c], "scale", 1f),
										LoadAnimationDefinition (children [c])
									);
								}
								else {
									emitter.AddParticleTemplate (
										XmlUtilities.GetString (children [c], "texture"),
										XmlUtilities.GetInt (children [c], "x"),
										XmlUtilities.GetInt (children [c], "y"),
										XmlUtilities.GetInt (children [c], "width"),
										XmlUtilities.GetInt (children [c], "height"),
										XmlUtilities.GetFloat (children [c], "scale", 1f)
									);
								}
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New particle: " + children [c].ToString ());
								break;

							case "scaleAnimation":
								emitter.AddAnimator (new ScaleAnimation (
									XmlUtilities.GetInt (children [c], "duration"),
									XmlUtilities.GetFloat (children [c], "start"),
									XmlUtilities.GetFloat (children [c], "end")
								));
								break;

							case "sfxSpawner":
								emitter.AddVariator (new SfxSpawner (
									XmlUtilities.GetString (children [c], "sfx")
								));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New SfxSpawner: " + children [c].ToString ());
								break;
						
							case "zAnimation":
								emitter.AddAnimator (new ZAnimation (
									XmlUtilities.GetInt (children [c], "duration"),
									XmlUtilities.GetInt (children [c], "start"),
									XmlUtilities.GetInt (children [c], "end")
								));
								Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New ZAnimation: " + children [c].ToString ());
								break;

							default:
								break;
						}
					}

					res.AddEmitter (emitter);
				}

				Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "Built particle system " + templateId);

				return res;
			}
			catch (Exception ex) {
				Logger.Warning ("GameFactory", "LoadParticleFromTemplate", ex.ToString ());
				return null;
			}
		}

		static public Skill LoadSkill (XmlNode skillRoot) {
			Skill res = new Skill ();
			res.CoolDown = XmlUtilities.GetInt (skillRoot, "cooldown");
			res.ID = XmlUtilities.GetString (skillRoot, "id");
			res.Name = XmlUtilities.GetString (skillRoot, "name");
			res.Priority = XmlUtilities.GetInt (skillRoot, "priority");
			res.Range = XmlUtilities.GetInt (skillRoot, "range");
			res.IconTexture = XmlUtilities.GetString (skillRoot, "iconTexture");
			res.MouseIconTexture = XmlUtilities.GetString (skillRoot, "mouseIconTexture");
			res.IconRect = XmlUtilities.GetIntRect (skillRoot, "iconRect", new IntRect (0, 0, 0, 0));
			res.MouseIconRect = XmlUtilities.GetIntRect (skillRoot, "mouseIconRect", new IntRect (0, 0, 0, 0));
			var scripts = skillRoot.SelectNodes ("./script");
			for (int s = 0; s < scripts.Count; s++) {
				var type = XmlUtilities.GetString (scripts [s], "type");
				switch (type) {
					case "ON_EMPTY":
						res.OnEmptyScript = LoadSkillBehaviour (scripts [s]);
						break;

					case "ON_SELF":
						res.OnSelfScript = LoadSkillBehaviour (scripts [s]);
						break;

					case "ON_TARGET":
						res.OnTargetScript = LoadSkillBehaviour (scripts [s]);
						break;

					default:
						break;
				}
			}
			return res;
		}

		static public SkillBehaviour LoadSkillBehaviour (XmlNode scriptRoot) {
			SkillBehaviour res;
			var script = XmlUtilities.GetStringArray (scriptRoot, "script", true);
			Logger.Debug ("GameFactory", "LoadSkillScript", script.ToString ());
			switch (script [0]) {
				case "DAMAGE":
					res = new DamageBehaviour ();
					var dtmp = res as DamageBehaviour;
					dtmp.Damage = int.Parse (script [1]);
					dtmp.DamageType = script [2];
					break;

				case "EFFECT":
					res = new EffectSkillScript ();
					var etmp = res as EffectSkillScript;
					etmp.Effect = ParseEffect (script);
					break;

				case "SPAWN":
					res = new SpawnBehaviour ();
					var stmp = res as SpawnBehaviour;
					for (var i = 1; i < script.Length; i++) {
						stmp.SpawnTemplateId = script [i];
					}
					break;

				default:
					res = new SkillBehaviour ();
					break;
			}
			res.SelfAnimation = XmlUtilities.GetString (scriptRoot, "selfAnimation");
			res.SelfParticle = XmlUtilities.GetString (scriptRoot, "selfParticle");
			res.TargetAnimation = XmlUtilities.GetString (scriptRoot, "targetAnimation");
			res.TargetParticle = XmlUtilities.GetString (scriptRoot, "targetParticle");
			return res;
		}

		static private Effect ParseEffect (string[] script) {
			Effect res;
			switch (script [1]) {
				case "BURN":
					res = new BurningEffect ();
					var bres = res as BurningEffect;
					bres.Duration = Simulator.ROUND_LENGTH * int.Parse (script [2]) + 1;
					bres.Strength = int.Parse (script [3]);
					break;

				case "FREEZE":
					res = new FreezeEffect ();
					var fres = res as FreezeEffect;
					fres.Duration = Simulator.ROUND_LENGTH * int.Parse (script [2]) + 1;
					break;

				case "GUARD":
					res = new GuardEffect ();
					var gres = res as GuardEffect;
					gres.Duration = Simulator.ROUND_LENGTH * int.Parse (script [2]) + 1;
					gres.Strength = int.Parse (script [3]);
					break;

				case "VULNERABLE":
					res = new VulnerableEffect (float.Parse (script [3]), script [2]);
					break;

				default:
					res = new Effect ();
					break;
			}
			return res;
		}
	}
}

