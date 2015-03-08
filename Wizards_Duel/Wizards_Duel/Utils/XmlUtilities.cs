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

namespace WizardsDuel.Utils
{
	static public class XmlUtilities
	{
		/// <summary>
		/// Returns true if the node has the attribute defined, its content is irrilevant
		/// </summary>
		/// <returns><c>true</c>, if the attribute is set, <c>false</c> otherwise.</returns>
		/// <param name="node">Node.</param>
		/// <param name="attributeName">Attribute name.</param>
		/// <param name="def">Default value that will be returned on errors.</param>
		static public bool GetBool(XmlNode node, string attributeName, bool def=false) {
			try {
				return node.Attributes.GetNamedItem(attributeName) != null;
			} catch {
				return def;
			}
		}

		/// <summary>
		/// Gets an XML node attribute as a SFML Color.
		/// </summary>
		/// <returns>The attribute value or the default value.</returns>
		/// <param name="node">Node.</param>
		/// <param name="attributeName">Attribute name.</param>
		/// <param name="def">Default value that will be returned on errors.</param>
		static public SFML.Graphics.Color GetColor(XmlNode node, string attributeName, SFML.Graphics.Color def) {
			try {
				var buff = XmlUtilities.GetIntArray(node, attributeName);
				var res = new SFML.Graphics.Color((byte)buff[0], (byte)buff[1], (byte)buff[2]);
				if (buff.Length > 3)
					res.A = (byte)buff[3];
				return res;
			} catch {
				return def;
			}
		}

		/// <summary>
		/// Gets an XML node attribute as a floting point value.
		/// </summary>
		/// <returns>The attribute value or the default value.</returns>
		/// <param name="node">Node.</param>
		/// <param name="attributeName">Attribute name.</param>
		/// <param name="def">Default value that will be returned on errors.</param>
		static public float GetFloat(XmlNode node, string attributeName, float def=0f) {
			try {
				return (float)Convert.ToDouble(node.Attributes.GetNamedItem(attributeName).Value, CultureInfo.InvariantCulture.NumberFormat);
			} catch (Exception ex) {
				Logger.Info ("XmlUtilities", "GetFloat", ex.ToString ());
				return def;
			}
		}

		/// <summary>
		/// Gets an XML node attribute as an integer.
		/// </summary>
		/// <returns>The attribute value or the default value.</returns>
		/// <param name="node">Node.</param>
		/// <param name="attributeName">Attribute name.</param>
		/// <param name="def">Default value that will be returned on errors.</param>
		static public int GetInt(XmlNode node, string attributeName, int def=0) {
			try {
				return Convert.ToInt32(node.Attributes.GetNamedItem(attributeName).Value);
			} catch (Exception ex) {
				Logger.Info ("XmlUtilities", "GetInt", ex.ToString ());
				return def;
			}
		}

		/// <summary>
		/// Gets an XML node attribute as an integer array. The array must be in a
		/// comma separated value format (e.g. "0,1,2,3")
		/// </summary>
		/// <returns>The attribute value or an array containing a single 0.</returns>
		/// <param name="node">Node.</param>
		/// <param name="attributeName">Attribute name.</param>
		static public int[] GetIntArray(XmlNode node, string attributeName) {
			try {
				var str = node.Attributes.GetNamedItem(attributeName).Value;
				var ints = str.Split(new char[]{' ', ',', ';'});
				var res = new int[ints.Length];
				for (var i = 0; i < ints.Length; i++) {
					res[i] = Convert.ToInt32(ints[i]);
				}
				return res;
			} catch (Exception ex) {
				Logger.Info ("XmlUtilities", "GetIntArray", ex.ToString ());
				return new int[]{0};
			}
		}

		/// <summary>
		/// Gets an XML node attribute as a string.
		/// </summary>
		/// <returns>The attribute value or the default value.</returns>
		/// <param name="node">Node.</param>
		/// <param name="attributeName">Attribute name.</param>
		/// <param name="def">Default value that will be returned on errors.</param>
		static public string GetString(XmlNode node, string attributeName, string def="") {
			try {
				return node.Attributes.GetNamedItem(attributeName).Value;
			} catch (Exception ex) {
				Logger.Info ("XmlUtilities", "GetString", ex.ToString ());
				return def;
			}
		}

		/// <summary>
		/// Gets an XML node attribute as a string array. The array must be in a
		/// comma separated value format (e.g. "a,b,c")
		/// </summary>
		/// <returns>The attribute value or an array containing a single empty string.</returns>
		/// <param name="node">Node.</param>
		/// <param name="attributeName">Attribute name.</param>
		static public string[] GetStringArray(XmlNode node, string attributeName) {
			try {
				var str = node.Attributes.GetNamedItem(attributeName).Value;
				return str.Split(new char[]{',', ';'});
			} catch (Exception ex) {
				Logger.Info ("XmlUtilities", "GetStringArray", ex.ToString ());
				return new string[]{string.Empty};
			}
		}

		/// <summary>
		/// Gets an XML node attribute as a string.
		/// </summary>
		/// <returns>The attribute value or the default value.</returns>
		/// <param name="node">Node.</param>
		/// <param name="attributeName">Attribute name.</param>
		/// <param name="def">Default value that will be returned on errors.</param>
		static public string GetValue(XmlNode node, string def="") {
			try {
				return node.Attributes.GetNamedItem("value").Value;
			} catch (Exception ex) {
				Logger.Info ("XmlUtilities", "GetValue", ex.ToString ());
				return def;
			}
		}
	}
}

