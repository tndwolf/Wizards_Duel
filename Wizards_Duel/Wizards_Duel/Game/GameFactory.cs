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
	}
}

