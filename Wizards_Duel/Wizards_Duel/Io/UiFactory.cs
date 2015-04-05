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
using WizardsDuel.Utils;
using SFML.Graphics;

namespace WizardsDuel.Io
{
	static public class UiFactory {
		static public WorldView LoadPage(string xmlfile, string pageId = "page_0") {
			IoManager.Clear ();
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(xmlfile);
			WorldView res = null;

			try {
				XmlNode pageNode = xdoc.SelectSingleNode ("//page[@id='" + pageId + "']");
				var widgets = pageNode.ChildNodes;
				for(var i = 0; i < widgets.Count; i++) {
					var widget = widgets[i];
					switch (widget.Name) {
					case "backgroundmusic":
						LoadBackgroundMusic(widget, pageNode);
						break;
					case "icon":
						IoManager.AddWidget(UiFactory.LoadIcon(widget, pageNode));
						break;
					case "worldView":
						res = UiFactory.LoadWorldView(widget, pageNode);
						IoManager.AddWidget (res);
						break;
					default:
						break;
					}
				}
			} catch (Exception ex) {
				Logger.Error ("UiFactory", "LoadPage", ex.ToString ());
			}
			return res;
		}

		static private void LoadBackgroundMusic(XmlNode widget, XmlNode page) {
			//return;
			//var music = new SFML.Audio.Music("Assets\\" + XmlUtilities.GetString (widget, "file"));
			//music.Play ();
			Logger.Info ("UiFactory", "LoadBackgroundMusic","loaded music: " + "Assets\\" + XmlUtilities.GetString (widget, "file"));
			var music = IoManager.LoadMusic(XmlUtilities.GetString (widget, "file"));
			if (music != null) {
				Logger.Info ("UiFactory", "LoadBackgroundMusic","loaded music");
				try {
					var loops = widget.ChildNodes;
					for(var i = 0; i < loops.Count; i++) {
						var xloop = loops[i];
						music.AddLoop(
							XmlUtilities.GetString (xloop, "name"),
							XmlUtilities.GetInt (xloop, "start"),
							XmlUtilities.GetInt (xloop, "end")
						);
					}
				} catch (Exception ex) {
					Logger.Warning ("UiFactory", "LoadBackgroundMusic", ex.ToString ());
				}
			}
		}

		static private Icon LoadIcon(XmlNode widget, XmlNode page) {
			try {
				var width = XmlUtilities.GetInt(widget, "width");
				var height = XmlUtilities.GetInt(widget, "height");
				var u = XmlUtilities.GetInt(widget, "u");
				var v = XmlUtilities.GetInt(widget, "v");
				var scale = XmlUtilities.GetFloat(widget, "scale", 1f);
				var res = new Icon (
					XmlUtilities.GetString(widget, "texture"),
					new IntRect (u, v, width, height)
				);
				res.ScaleX = scale;
				res.ScaleY = scale;
				res.Position = new SFML.Window.Vector2f(XmlUtilities.GetInt(widget, "x"), XmlUtilities.GetInt(widget, "y"));
				return res;
			} catch (Exception ex) {
				Logger.Warning ("UiFactory", "LoadIcon", ex.ToString ());
				return null;
			}
		}

		static public void LoadTilemask(XmlNode tilemask, string layerName, WorldView worldView) {
			// load the default rule
			var rule = new WorldView.Rule();
			rule.x = XmlUtilities.GetIntArray(tilemask, "defaultX");
			rule.y = XmlUtilities.GetIntArray(tilemask, "defaultY");
			rule.w = XmlUtilities.GetInt(tilemask, "defaultW");
			rule.h = XmlUtilities.GetInt(tilemask, "defaultH");
			worldView.AddRule (rule, layerName);

			// load all the rules
			var rules = tilemask.SelectNodes ("./tile");
			for (var i = 0; i < rules.Count; i++) {
				try {
					var xrule = rules.Item (i);
					var type = XmlUtilities.GetString(xrule, "type");
					if (type == "ROW") {
						rule = new WorldView.RowRule();
					} else {
						rule = new WorldView.Rule();
					}
					rule.x = XmlUtilities.GetIntArray(xrule, "x");
					rule.y = XmlUtilities.GetIntArray(xrule, "y");
					rule.dx = XmlUtilities.GetInt(xrule, "dx");
					rule.dy = XmlUtilities.GetInt(xrule, "dy");
					rule.w = XmlUtilities.GetInt(xrule, "w");
					rule.h = XmlUtilities.GetInt(xrule, "h");
					rule.maxX = XmlUtilities.GetInt(xrule, "maxX");
					rule.maxX = (rule.maxX < 1) ? rule.w + rule.x[rule.x.Length-1] : rule.maxX;
					//rule.maxY = XmlUtilities.GetInt(xrule, "maxY");
					//rule.maxY = (rule.maxY < 1) ? rule.h + rule.y[rule.y.Length-1] : rule.maxY;
					var conditions = xrule.ChildNodes;
					for (var j = 0; j < conditions.Count; j++) {
						var xcondition = conditions.Item (j);
						var condition = new WorldView.Rule.Condition() {
							dx = XmlUtilities.GetInt(xcondition, "dx"),
							dy = XmlUtilities.GetInt(xcondition, "dy"),
							value = XmlUtilities.GetValue(xcondition)[0]
						};
						rule.conditions.Add (condition);
						Logger.Info ("UiFactory", "LoadTilemask", "Adding Condition: " + condition.ToString());
					}
					Logger.Info ("UiFactory", "LoadTilemask", "Adding Rule: " + rule.ToString());
					worldView.AddRule (rule, layerName);
				} catch (Exception ex) {
					Logger.Warning ("UiFactory", "LoadTilemask", rules.Item (i).ToString () + "; " + ex.ToString ());
				}
			}
		}

		static private WorldView LoadWorldView(XmlNode widget, XmlNode page) {
			var width = XmlUtilities.GetInt(widget, "width");
			var height = XmlUtilities.GetInt(widget, "height");
			var cellWidth = XmlUtilities.GetInt(widget, "cellWidth");
			var cellHeight = XmlUtilities.GetInt(widget, "cellHeight");
			var scale = XmlUtilities.GetFloat(widget, "scale", 1f);
			var view = new WorldView(width, height, cellWidth, cellHeight, scale);
			try {
				var layers = widget.ChildNodes;
				for(var i = 0; i < layers.Count; i++) {
					var xlayer = layers[i];
					switch (xlayer.Name) {
					case "backgroundLayer":
						var bl = new BackgroundLayer (
							width, 
							height, 
							XmlUtilities.GetString (xlayer, "texture")
						);
						try {
							var color = XmlUtilities.GetIntArray(xlayer, "color");
							bl.Color = new Color((byte)color[0], (byte)color[1], (byte)color[2], (byte)color[3]);
							Logger.Info ("UiFactory", "LoadWorldView", "SET COLOR TO BG " + color.ToString());
						}catch{}
						bl.Static = XmlUtilities.GetBool(xlayer, "static");
						bl.Scale = XmlUtilities.GetFloat(xlayer, "scale", 1f);
						bl.Blend = UiFactory.GetBlend(xlayer);
						view.AddLayer (bl, LayerType.UNDEFINED);
						break;
					case "lightLayer":
						var ll = new LightLayer (width, height);
						ll.Scale = XmlUtilities.GetFloat(xlayer, "scale", 1f);
						ll.AmbientLight = new SFML.Graphics.Color(
							(byte)XmlUtilities.GetInt (xlayer, "ambientRed"),
							(byte)XmlUtilities.GetInt (xlayer, "ambientGreen"),
							(byte)XmlUtilities.GetInt (xlayer, "ambientBlue")
						);
						ll.Blend = UiFactory.GetBlend(xlayer);
						view.AddLayer (ll, LayerType.LIGHTS);
						break;
					case "objectsLayer":
						var gl = new GridLayer(width, height, 200, 200, cellWidth, cellHeight);
						gl.GridBorder = 3;
						gl.GridPadding = 2;
						gl.OutColor = new Color(0,0,0,128);
						view.AddLayer(gl);
						var ol = new ObjectsLayer ();
						ol.Scale = XmlUtilities.GetFloat(xlayer, "scale", 1f);
						ol.Blend = UiFactory.GetBlend(xlayer);
						view.AddLayer (ol, LayerType.OBJECTS);
						break;
					case "tiledLayer":
						var ml = new TiledLayer (
							width, 
							height, 
							XmlUtilities.GetString (xlayer, "texture")
						);
						var name = XmlUtilities.GetString(xlayer, "name", WorldView.UNNAMED_LAYER);
						view.AddLayer (ml, name);
						var maskId = XmlUtilities.GetString(xlayer, "mask");
						var tileMask = page.SelectSingleNode("//tileMasks[@id='" + maskId + "']");
						ml.SetTilemask(
							1, // XXX will be updated later on
							1, // XXX will be updated later on
							XmlUtilities.GetInt(tileMask, "defaultW"),
							XmlUtilities.GetInt(tileMask, "defaultH"),
							XmlUtilities.GetString(tileMask, "texture")
						);
						ml.SetDefaultTile(
							XmlUtilities.GetInt(tileMask, "defaultX"),
							XmlUtilities.GetInt(tileMask, "defaultY"),
							XmlUtilities.GetInt(tileMask, "defaultW"),
							XmlUtilities.GetInt(tileMask, "defaultH")
						);
						UiFactory.LoadTilemask(tileMask, name, view);
						ml.Scale = XmlUtilities.GetFloat(xlayer, "scale", 1f);
						ml.TileScale = XmlUtilities.GetFloat(xlayer, "tileScale", 1f);
						ml.Blend = UiFactory.GetBlend(xlayer);
						ml.MaskBlend = UiFactory.GetBlend(xlayer, "maskBlend");
						break;
					default:
						break;
					}
				}
			} catch (Exception ex) {
				Logger.Warning ("UiFactory", "LoadWorldView", ex.ToString ());
			}
			return view;
		}

		static private BlendMode GetBlend(XmlNode node, string attributeName="blend") {
			try {
				var type = node.Attributes.GetNamedItem(attributeName).Value;
				switch (type) {
				case "ADD":
					return BlendMode.Add;
				case "MULTIPLY":
					return BlendMode.Multiply;
				default:
					return BlendMode.Alpha;
				}
			} catch (Exception ex) {
				Logger.Warning ("UiFactory", "GetFloat", ex.ToString ());
				return BlendMode.Alpha;
			}
		}

		static private LayerType GetLayerType(XmlNode node, string attributeName="type", LayerType def=LayerType.UNDEFINED) {
			try {
				var type = node.Attributes.GetNamedItem(attributeName).Value;
				switch (type) {
				case "FLOOR":
					return LayerType.FLOOR;
				case "LIGHTS":
					return LayerType.LIGHTS;
				case "OBJECTS":
					return LayerType.OBJECTS;
				case "WALL":
					return LayerType.WALL;
				default:
					return def;
				}
			} catch (Exception ex) {
				Logger.Warning ("UiFactory", "GetLayerType", ex.ToString ());
				return def;
			}
		}
	}
}

