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
using WizardsDuel.Io;
using WizardsDuel.Utils;
using SFML.Window;
using SFML.Graphics;

namespace WizardsDuel.Game
{
	static public class GameFactory
	{
		static XmlDocument xdoc;

		static public World LoadGame (string xmlfile) {
			GameFactory.xdoc = new XmlDocument();
			GameFactory.xdoc.Load(xmlfile);

			try {
				XmlNode mapNode = GameFactory.xdoc.SelectSingleNode ("//map");
				int gridWidth = Convert.ToInt32(mapNode.Attributes.GetNamedItem ("gridWidth").Value);
				int gridHeight = Convert.ToInt32(mapNode.Attributes.GetNamedItem ("gridHeight").Value);

				string map = mapNode.InnerText;
				var maprows = map.Split (new string[] {Environment.NewLine, "\t", " ", ","}, StringSplitOptions.RemoveEmptyEntries);

				var res = new World ();

				res.worldView = UiFactory.LoadPage(xmlfile);

				/*res.worldView = new WorldView(gridWidth, gridHeight, cellWidth, cellHeight, scale);
				var tilemask = xdoc.SelectSingleNode("//tileMasks[@id='tilemask_floor_01']");
				UiFactory.LoadTilemask(tilemask, LayerType.FLOOR, res.worldView);*/
				Logger.Debug ("GameFactory", "LoadGame", "Build world, initializing dungeon");
				res.SetMap(maprows);

				return res;
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
				var res = new World ();
				res.worldView = new WorldView();
				return res;
			}
		}

		static public Entity LoadFromTemplate(string templateId) {
			try {
				var res = new Entity ();
				XmlNode template = GameFactory.xdoc.SelectSingleNode ("//blueprint[@id='" + templateId + "']");
				XmlNode outTemplate = template.SelectSingleNode ("./output");
				res.OutObject = new OutObject (
					XmlUtilities.GetString(outTemplate, "texture"),
					new SFML.Graphics.IntRect(
						XmlUtilities.GetInt(outTemplate, "defaultX"), 
						XmlUtilities.GetInt(outTemplate, "defaultY"), 
						XmlUtilities.GetInt(outTemplate, "defaultW"), 
						XmlUtilities.GetInt(outTemplate, "defaultH")
					)
				);

				var animations = outTemplate.SelectNodes("./animation");
				for (int a = 0; a < animations.Count; a++) {
					AnimationDefinition ad = new AnimationDefinition ();
					var frames = animations[a].SelectNodes("./frame");
					for (int f = 0; f < frames.Count; f++) {
						var frame = new AnimationFrame(
							XmlUtilities.GetInt(frames[f], "x"),
							XmlUtilities.GetInt(frames[f], "y"),
							XmlUtilities.GetInt(frames[f], "width"),
							XmlUtilities.GetInt(frames[f], "height"),
							XmlUtilities.GetInt(frames[f], "duration")
						);
						frame.offset = new Vector2f(
							XmlUtilities.GetFloat(frames[f], "offsetX"), 
							XmlUtilities.GetFloat(frames[f], "offsetY")
						);
						ad.AddFrame(frame);
						//Logger.Debug ("GameFactory", "LoadFromTemplate", "Added frame" + frame.ToString());
					}
					res.OutObject.AddAnimation (XmlUtilities.GetString(animations[a], "name"), ad);
					//Logger.Debug ("GameFactory", "LoadFromTemplate", "Added animation " + XmlUtilities.GetString(animations[a], "name"));
				}

				Logger.Debug ("GameFactory", "LoadFromTemplate", "Built object " + templateId);

				return res;
			} catch (Exception ex) {
				Logger.Warning ("GameFactory", "LoadFromTemplate", ex.ToString ());
				return null;
			}
		}

		static public ParticleSystem LoadParticleFromTemplate(string templateId, float x, float y, ObjectsLayer layer, bool flip = false) {
			try {
				var xCoeff = flip ? -1f : 1f;
				var angleCoeff = flip ? 3.1415f : 0f;
				var res = new ParticleSystem ();
				XmlNode template = GameFactory.xdoc.SelectSingleNode ("//particle[@id='" + templateId + "']");
				res.TTL = XmlUtilities.GetInt(template, "ttl");
				res.Layer = layer;
				res.Position = new Vector2f(x, y);

				var emitters = template.SelectNodes("./emitter");
				for (int e = 0; e < emitters.Count; e++) {
					Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New emitter: " + emitters[e].ToString());
					var startDelay = XmlUtilities.GetInt(emitters[e], "startDelay");
					var emitter = new Emitter(res, startDelay);
					emitter.Offset = new Vector2f(
						XmlUtilities.GetFloat(emitters[e], "offsetX") * xCoeff,
						XmlUtilities.GetFloat(emitters[e], "offsetY")
					);
					emitter.ParticleTTL = XmlUtilities.GetInt(emitters[e], "particleTtl");
					emitter.SpawnCount = XmlUtilities.GetInt(emitters[e], "spawnCount");
					emitter.SpawnDeltaTime = XmlUtilities.GetInt(emitters[e], "spawnDeltaTime");
					emitter.TTL = XmlUtilities.GetInt(emitters[e], "ttl");
					emitter.ZIndex = XmlUtilities.GetInt(emitters[e], "zIndex", 0);

					var children = emitters[e].ChildNodes;
					for(var c = 0; c < children.Count; c++) {
						switch (children[c].Name) {
						case "boxSpawner":
							emitter.AddVariator (new BoxSpawner (
								XmlUtilities.GetFloat(children[c], "width"),
								XmlUtilities.GetFloat(children[c], "height")
							));
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New BoxSpawner: " + children[c].ToString());
							break;

						case "burstSpawner":
							emitter.AddVariator (new BurstSpawner (
								XmlUtilities.GetFloat(children[c], "maxForce"),
								XmlUtilities.GetFloat(children[c], "minForce"),
								XmlUtilities.GetFloat(children[c], "maxAngle") + angleCoeff,
								XmlUtilities.GetFloat(children[c], "minAngle") + angleCoeff
							));
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New BurstSpawner: " + children[c].ToString());
							break;

						case "colorAnimation":
							var buff = XmlUtilities.GetIntArray(children[c], "startColor");
							var startColor = new Color((byte)buff[0], (byte)buff[1], (byte)buff[2]);
							if (buff.Length > 3)
								startColor.A = (byte)buff[3];
							buff = XmlUtilities.GetIntArray(children[c], "endColor");
							var endColor = new Color((byte)buff[0], (byte)buff[1], (byte)buff[2]);
							if (buff.Length > 3)
								endColor.A = (byte)buff[3];
							emitter.AddAnimator (new ColorAnimation(startColor, endColor, XmlUtilities.GetInt(children[c], "duration")));
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New ColorAnimator: " + children[c].ToString());
							break;

						case "colorPicker":
							var cps  = new ColorPickerSpawner ();
							var colors = children[c].SelectNodes("./color");
							for (int i = 0; i < colors.Count; i++) {
								var color = XmlUtilities.GetIntArray(colors[i], "select");
								switch (color.Length) {
								case 3:
									cps.AddColor(new Color((byte)color[0], (byte)color[1], (byte)color[2]));
									break;
								case 4:
									cps.AddColor(new Color((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]));
									break;
								default: break;
								}
							}
							emitter.AddVariator (cps);
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New ColorPickerSpawner: " + children[c].ToString());
							break;

						case "fade":
							emitter.AddAnimator (new FadeAnimation (
								XmlUtilities.GetInt(children[c], "fadeInDuration"),
								XmlUtilities.GetInt(children[c], "duration"),
								XmlUtilities.GetInt(children[c], "fadeOutDuration")
							));
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New FadeAnimator: " + children[c].ToString());
							break;

						case "gravity":
							emitter.AddAnimator (new GravityAnimation (new Vector2f(
								XmlUtilities.GetFloat(children[c], "forceX") * xCoeff, 
								XmlUtilities.GetFloat(children[c], "forceY")
							)));
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New GravityAnimation: " + children[c].ToString());
							break;
						
						case "gridSpawner":
							emitter.AddVariator (new GridSpawner (
								XmlUtilities.GetInt(children[c], "gridWidth"),
								XmlUtilities.GetInt(children[c], "gridHeight"),
								XmlUtilities.GetFloat(children[c], "cellWidth") * xCoeff,
								XmlUtilities.GetFloat(children[c], "cellHeight"),
								XmlUtilities.GetFloat(children[c], "deltaX", 0f),
								XmlUtilities.GetFloat(children[c], "deltaY", 0f)
							));
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New GridSpawner: " + children[c].ToString());
							break;

						case "lightSpawner":
							buff = XmlUtilities.GetIntArray(children[c], "color");
							var lightColor = new Color((byte)buff[0], (byte)buff[1], (byte)buff[2]);
							if (buff.Length > 3)
								lightColor.A = (byte)buff[3];
							emitter.AddVariator (new LightSpawner(lightColor, XmlUtilities.GetInt(children[c], "radius")));
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New LightSpawner: " + children[c].ToString());
							break;

						case "particleTemplate":
							emitter.AddParticleTemplate(
								XmlUtilities.GetString(children[c], "texture"),
								XmlUtilities.GetInt(children[c], "x"),
								XmlUtilities.GetInt(children[c], "y"),
								XmlUtilities.GetInt(children[c], "width"),
								XmlUtilities.GetInt(children[c], "height"),
								XmlUtilities.GetFloat(children[c], "scale", 1f)
							);
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New particle: " + children[c].ToString());
							break;

						case "scaleAnimation":
							emitter.AddAnimator(new ScaleAnimation(
								XmlUtilities.GetInt(children[c], "duration"),
								XmlUtilities.GetFloat(children[c], "start"),
								XmlUtilities.GetFloat(children[c], "end")
							));
							break;
						
						case "zAnimation":
							emitter.AddAnimator(new ZAnimation(
								XmlUtilities.GetInt(children[c], "duration"),
								XmlUtilities.GetInt(children[c], "start"),
								XmlUtilities.GetInt(children[c], "end")
							));
							Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "New ZAnimation: " + children[c].ToString());
							break;

						default:
							break;
						}
					}

					//emitter.AddParticleTemplate ("FX01.png", 0, 0, 1, 1, 2f);

					res.AddEmitter(emitter);
				}

				Logger.Debug ("GameFactory", "LoadParticleFromTemplate", "Built particle system " + templateId);

				return res;
			} catch (Exception ex) {
				Logger.Warning ("GameFactory", "LoadParticleFromTemplate", ex.ToString ());
				return null;
			}
		}
	}
}

