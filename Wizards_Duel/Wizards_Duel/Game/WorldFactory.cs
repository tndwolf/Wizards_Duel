// Wizard's Duel, a procedural tactical RPG
// Copyright (C) 2015  Luca Carbone
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
using System.Collections.Generic;
using System.Xml;
using WizardsDuel.Utils;
using System.Text.RegularExpressions;
using WizardsDuel.Io;

namespace WizardsDuel.Game {
	public class BlockExit {
		public int X;
		public int Y;
		public string Direction;
		// N, S, W, E, no need for an enum
		public bool CanBeClosed;

		public string FlipDirection () {
			switch (this.Direction) {
				case "N":
					return "S";
				case "S":
					return "N";
				default:
					return this.Direction;
			}
		}

		public string FlipMirrorDirection () {
			switch (this.Direction) {
				case "N":
					return "S";
				case "S":
					return "N";
				case "W":
					return "E";
				case "E":
					return "W";
				default:
					return this.Direction;
			}
		}

		public string MirrorDirection () {
			switch (this.Direction) {
				case "W":
					return "E";
				case "E":
					return "W";
				default:
					return this.Direction;
			}
		}

		public string RotateDirectionCCW () {
			switch (this.Direction) {
				case "N":
					return "W";
				case "S":
					return "E";
				case "W":
					return "S";
				case "E":
					return "N";
				default:
					return this.Direction;
			}
		}

		public string RotateDirectionCW () {
			switch (this.Direction) {
				case "N":
					return "E";
				case "S":
					return "W";
				case "W":
					return "N";
				case "E":
					return "S";
				default:
					return this.Direction;
			}
		}
	}

	public class BlockObject {
		public string ID;
		public int X;
		public int Y;
		public float Probability;
		public string[] Variations;
	}

	public class ConstructionBlock {
		private char[,] block;
		private List<BlockExit> exits = new List<BlockExit> ();
		private List<BlockObject> objects = new List<BlockObject> ();

		public ConstructionBlock (string block, int width, int height, string id) {
			width = (width < 1) ? 1 : width;
			height = (height < 1) ? 1 : height;
			block = (block.Length < 1) ? "#" : block;
			this.block = new char[width, height];
			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					this.block [x, y] = block [x + width * y];
				}
			}
			ID = id;
			this.EndPoint = null;
			this.StartPoint = null;
		}

		public void AddExit (int x, int y, string direction, bool canBeClosed) {
			if (x < this.Width && x >= 0 && y < this.Height && y >= 0 &&
			    (direction == "N" || direction == "S" || direction == "W" || direction == "E")) {
				var exit = new BlockExit { X = x, Y = y, Direction = direction, CanBeClosed = canBeClosed };
				this.exits.Add (exit);
			}
		}

		public void AddObject (string oid, int x, int y, float probability, string[] variations) {
			if (x < this.Width && x >= 0 && y < this.Height && y >= 0) {
				var obj = new BlockObject { ID = oid, X = x, Y = y, Probability = probability, Variations = variations };
				this.objects.Add (obj);
			}
		}

		// TODO apply transformation (rotation, flip etc...)
		public BlockObject EndPoint { get; set; }

		public List<BlockExit> Exits {
			get { return this.exits; }
		}

		public int ExitsCount {
			get { return this.exits.Count; }
		}

		public ConstructionBlock Flip () {
			string bblock = "";
			for (int y = Height - 1; y >= 0; y--) {
				for (int x = 0; x < Width; x++) {
					bblock += this.block [x, y];
				}
			}
			var res = new ConstructionBlock (bblock, this.Width, this.Height, this.ID);
			foreach (var exit in this.exits) {
				res.AddExit (exit.X, this.Height - 1 - exit.Y, exit.FlipDirection (), exit.CanBeClosed);
			}
			foreach (var obj in this.objects) {
				if (Array.IndexOf (obj.Variations, "F") > -1)
					res.AddObject (obj.ID, obj.X, this.Height - 1 - obj.Y, obj.Probability, new string[]{ });
			}
			return res;
		}

		public ConstructionBlock FlipMirror () {
			string bblock = "";
			for (int y = Height - 1; y >= 0; y--) {
				for (int x = Width - 1; x >= 0; x--) {
					bblock += this.block [x, y];
				}
			}
			var res = new ConstructionBlock (bblock, this.Width, this.Height, this.ID);
			foreach (var exit in this.exits) {
				res.AddExit (this.Width - 1 - exit.X, this.Height - 1 - exit.Y, exit.FlipMirrorDirection (), exit.CanBeClosed);
			}
			foreach (var obj in this.objects) {
				if (Array.IndexOf (obj.Variations, "FM") > -1)
					res.AddObject (obj.ID, this.Width - 1 - obj.X, this.Height - 1 - obj.Y, obj.Probability, new string[]{ });
			}
			return res;
		}

		public string ID { get; set; }

		public BlockExit GetAccess (string exitDirection) {
			foreach (var exit in this.exits) {
				if (exitDirection == "N" && exit.Direction == "S")
					return exit;
				else if (exitDirection == "S" && exit.Direction == "N")
					return exit;
				else if (exitDirection == "W" && exit.Direction == "E")
					return exit;
				else if (exitDirection == "E" && exit.Direction == "W")
					return exit;
			}
			return null;
		}

		public string GetTile (int x, int y) {
			return this.block [x, y].ToString ();
		}

		public int Height { 
			get { return this.block.GetLength (1); }
		}

		public ConstructionBlock Mirror () {
			string bblock = "";
			for (int y = 0; y < Height; y++) {
				for (int x = Width - 1; x >= 0; x--) {
					bblock += this.block [x, y];
				}
			}
			var res = new ConstructionBlock (bblock, this.Width, this.Height, this.ID);
			foreach (var exit in this.exits) {
				res.AddExit (this.Width - 1 - exit.X, exit.Y, exit.MirrorDirection (), exit.CanBeClosed);
			}
			foreach (var obj in this.objects) {
				if (Array.IndexOf (obj.Variations, "M") > -1)
					res.AddObject (obj.ID, this.Width - 1 - obj.X, obj.Y, obj.Probability, new string[]{ });
			}
			return res;
		}

		public List<BlockObject> Objects {
			get { return this.objects; }
		}

		public int Occurrencies { get; set; }

		public ConstructionBlock RotateCCW () {
			var newWidth = block.GetLength (1);
			var newHeight = block.GetLength (0);
			char[,] newMatrix = new char[newWidth, newHeight];
			for (var oy = 0; oy < newWidth; oy++) {
				for (var ox = 0; ox < newHeight; ox++) {
					var nx = oy;
					var ny = newHeight - ox - 1;
					newMatrix [nx, ny] = block [ox, oy];
				}
			}
			////////
			string bblock = "";
			for (int y = 0; y < newHeight; y++) {
				for (int x = 0; x < newWidth; x++) {
					bblock += newMatrix [x, y];
				}
			}
			var res = new ConstructionBlock (bblock, newWidth, newHeight, this.ID);
			foreach (var exit in this.exits) {
				var nx = exit.Y;
				var ny = newHeight - exit.X - 1;
				res.AddExit (nx, ny, exit.RotateDirectionCCW (), exit.CanBeClosed);
			}
			foreach (var obj in this.objects) {
				if (Array.IndexOf (obj.Variations, "CCW") > -1) {
					var nx = obj.Y;
					var ny = newHeight - obj.X - 1;
					res.AddObject (obj.ID, nx, ny, obj.Probability, new string[]{ });
				}
			}
			return res;
		}

		public ConstructionBlock RotateCW () {
			var newWidth = block.GetLength (1);
			var newHeight = block.GetLength (0);
			char[,] newMatrix = new char[newWidth, newHeight];
			for (var oy = 0; oy < newWidth; oy++) {
				for (var ox = 0; ox < newHeight; ox++) {
					var nx = newWidth - oy - 1;
					var ny = ox;
					newMatrix [nx, ny] = block [ox, oy];
				}
			}
			////////
			string bblock = "";
			for (int y = 0; y < newHeight; y++) {
				for (int x = 0; x < newWidth; x++) {
					bblock += newMatrix [x, y];
				}
			}
			var res = new ConstructionBlock (bblock, newWidth, newHeight, this.ID);
			foreach (var exit in this.exits) {
				var nx = newWidth - exit.Y - 1;
				var ny = exit.X;
				res.AddExit (nx, ny, exit.RotateDirectionCW (), exit.CanBeClosed);
			}
			foreach (var obj in this.objects) {
				if (Array.IndexOf (obj.Variations, "CW") > -1) {
					var nx = newWidth - obj.Y - 1;
					var ny = obj.X;
					res.AddObject (obj.ID, nx, ny, obj.Probability, new string[]{ });
				}
			}
			return res;
		}

		// TODO apply transformation (rotation, flip etc...)
		public BlockObject StartPoint { get; set; }

		override public string ToString () {
			var res = String.Format ("<block width=\"{0}\" height=\"{1}\">\n", Width, Height);
			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					res += this.block [x, y].ToString ();
				}
				res += "\n";
			}
			foreach (var exit in this.exits) {
				res += String.Format ("<exit x=\"{0}\" y=\"{1}\" direction=\"{2}\"/>\n", exit.X, exit.Y, exit.Direction);
			}
			foreach (var obj in this.objects) {
				res += String.Format ("<exit ref=\"{0}\" x=\"{1}\" y=\"{2}\" probability=\"{3}\"/>\n", obj.ID, obj.X, obj.Y, obj.Probability);
			}
			res += "</block>";
			return res;
		}

		public int Width { 
			get { return this.block.GetLength (0); } 
		}
	}

	public class BufferLevel {
		public const int MAX_ITERATIONS = 100;

		private int blockCount = 0;
		private ProbabilityVector<BlockExit> exits = new ProbabilityVector<BlockExit> ();
		private int maxX = 0;
		private int maxY = 0;
		private int minX = 0;
		private int minY = 0;

		public BufferLevel (int w, int h, string defaultTile) {
			this.minX = w - 1;
			this.minY = h - 1;
			var data = new string[w, h];
			for (int y = 0; y < h; y++) {
				for (int x = 0; x < w; x++) {
					data [x, y] = defaultTile;
				}
			}
			this.Data = data;
			this.IsUsed = new bool[w, h];
			this.UsedArea = 0;
			this.Objects = new List<BlockObject> ();
			this.StartCell = new BlockObject { ID = "start", Probability = 1f, X = 0, Y = 0 };
			this.EndCell = new BlockObject { ID = "end", Probability = 1f, X = 0, Y = 0 };
		}

		public bool AddBlock (ConstructionBlock next) {
			// search for a valid exit
			BlockExit access;
			BlockExit exit;
			var iter = 0;
			var x = 0;
			var y = 0;
			do {
				exit = exits.Random ();
				while (this.IsOpenExit (exit.X, exit.Y, exit.Direction) == false && iter++ < MAX_ITERATIONS) {
					if (exit.CanBeClosed) {
						// the exit was closed, we can just close it and remove it
						//this.Data [exit.X, exit.Y] = this.CloseTile;
						//this.exits.Remove (exit);
					}
					exit = exits.Random ();
				}
				access = next.GetAccess (exit.Direction);
				if (access != null) {
					x = exit.X - access.X;
					y = exit.Y - access.Y;
					x += (access.Direction == "W") ? 1 : (access.Direction == "E") ? -1 : 0;
					y += (access.Direction == "N") ? 1 : (access.Direction == "S") ? -1 : 0;
					if (this.CanPlaceBlock (next, x, y) == false)
						access = null;
				}
			} while (access == null && iter++ < MAX_ITERATIONS);
			if (iter < MAX_ITERATIONS) {
				// BEFORE placing the block remove the used exit
				this.exits.Remove (exit);
				this.PlaceBlock (next, x, y);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Places a random block inside the level, using the still open exits. If <c>false</c> is returned
		/// it means that no blocks can be added animaore (within a tolerance level).
		/// </summary>
		/// <returns><c>true</c>, if random block was added, <c>false</c> otherwise.</returns>
		/// <param name="blocks">Blocks.</param>
		public bool AddRandomBlock (ProbabilityVector<ConstructionBlock> blocks) {
			var exit = exits.Random ();
			var iter = 0;
			var x = 0;
			var y = 0;
			if (exit != null) {
				// search for a valid exit
				while (this.IsOpenExit (exit.X, exit.Y, exit.Direction) == false && iter++ < MAX_ITERATIONS) {
					if (exit.CanBeClosed) {
						// the exit was closed, we can just close it and remove it
						//this.Data [exit.X, exit.Y] = this.CloseTile;
						//this.exits.Remove (exit);
					}
					exit = exits.Random ();
				}
				// got one, now search for a block with a valid connection
				ConstructionBlock next = null;
				BlockExit access = null;
				do {
					next = blocks.Random ();
					access = next.GetAccess (exit.Direction);
					if (access != null) {
						x = exit.X - access.X;
						y = exit.Y - access.Y;
						x += (access.Direction == "W") ? 1 : (access.Direction == "E") ? -1 : 0;
						y += (access.Direction == "N") ? 1 : (access.Direction == "S") ? -1 : 0;
						if (this.CanPlaceBlock (next, x, y) == false)
							access = null;
					}
				} while (access == null && iter++ < MAX_ITERATIONS);
				// found a good block? place it
				if (iter < MAX_ITERATIONS) {
					// BEFORE placing the block remove the used exit
					this.exits.Remove (exit);
					this.PlaceBlock (next, x, y);
					return true;
				}
			}
			return false;
		}

		public bool CanPlaceBlock (ConstructionBlock block, int x, int y) {
			for (int j = y; j < y + block.Height; j++) {
				for (int i = x; i < x + block.Width; i++) {
					if (this.IsValid (i, j) == false || this.IsUsed [i, j] == true) {
						return false;
					}
				}
			}
			return true;
		}

		public void CellularAutomata (string rule, string result, int iterations = 1) {
			for (var n = 0; n < iterations; n++) {
				for (var y = 1; y < this.Height - 1; y++) {
					for (var x = 1; x < this.Width - 1; x++) {
						if (this.Objects.Find (o => o.X == x && o.Y == y) != null) {
							// skip cell if some object was on it
							continue;
						}
						if (this.Data [x - 1, y] == "." && this.Data [x + 1, y] == "." && this.Data [x, y - 1] == "#" && this.Data [x, y + 1] == "#") {
							this.Data [x, y] = ".";
						}
						else if (this.Data [x - 1, y] == "#" && this.Data [x + 1, y] == "#" && this.Data [x, y - 1] == "." && this.Data [x, y + 1] == ".") {
							this.Data [x, y] = ".";
						}
					}
				}
			}
		}

		public int CountNeighbors (int x, int y, string toCount) {
			var res = 0;
			/*
			for (var j = y-1; j < y+1; j++) {
				for (var i = x-1; i < x+1; i++) {
					if (j == y && x == i)
						continue;
					if (this.IsValid (i, j) && this.Data[i, j] == toCount) {
						res++;
					}
				}
			}//*/
			//*
			if (this.IsValid (x, y, 1)) {
				if (this.Data [x - 1, y] == toCount)
					res++;
				if (this.Data [x + 1, y] == toCount)
					res++;
				if (this.Data [x, y - 1] == toCount)
					res++;
				if (this.Data [x, y + 1] == toCount)
					res++;
			}//*/
			return res;
		}

		public void CloseExits () {
			foreach (var exit in this.exits.Dictionary.Keys) {
				if (exit.CanBeClosed) {
					this.Data [exit.X, exit.Y] = CloseTile;
				}
			}
			this.exits.Clear ();
		}

		public string CloseTile { get; set; }

		public BlockObject EndCell { get; set; }

		public bool IsOpenExit (int x, int y, string direction) {
			if (this.IsValid (x, y, 1)) {
				switch (direction) {
					case "N":
						if (this.IsUsed [x, y - 1] == false)
							return true;
						break;
					case "S":
						if (this.IsUsed [x, y + 1] == false)
							return true;
						break;
					case "W":
						if (this.IsUsed [x - 1, y] == false)
							return true;
						break;
					case "E":
						if (this.IsUsed [x + 1, y] == false)
							return true;
						break;
				}
			}
			return false;
		}

		public string[,] Data { get; set; }

		public void Dump () {
			Console.Write ("\n");
			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					if (this.Data [x, y] != null)
						Console.Write (this.Data [x, y]);
					else
						Console.Write ("X");
				}
				Console.Write ("\n");
			}
			Console.WriteLine ("Placed blocks: " + this.blockCount);
			Console.WriteLine ("Total area: " + this.UsedArea);
			Console.WriteLine ("Width: " + this.Width);
			Console.WriteLine ("Height: " + this.Height);
			Console.WriteLine ("Unused exits: " + this.exits.ItemCount);
		}

		public int Height { get { return this.Data.GetLength (1); } }

		public bool[,] IsUsed { get; set; }

		public bool IsValid (int x, int y, int tolerance = 0) {
			return (
			    x - tolerance >= 0 &&
			    x + tolerance < this.Data.GetLength (0) &&
			    y - tolerance >= 0 &&
			    y + tolerance < this.Data.GetLength (1)
			);
		}

		public List<BlockObject> Objects {
			get;
			set;
		}

		public void PlaceBlock (ConstructionBlock block, int x, int y) {
			for (int j = 0; j < block.Height; j++) {
				for (int i = 0; i < block.Width; i++) {
					//if (i + x >= 0 && i + x < maxX && j + y >= 0 && j + y < maxY && this.IsUsed [i + x, j + y] == false) {
					if (this.IsValid (i + x, j + y) && this.IsUsed [i + x, j + y] == false) {
						this.Data [i + x, j + y] = block.GetTile (i, j);
						this.IsUsed [i + x, j + y] = true;
						this.UsedArea++;
					}
				}
			}
			foreach (var exit in block.Exits) {
				var i = exit.X + x;
				var j = exit.Y + y;
				if (this.IsOpenExit (i, j, exit.Direction)) {
					var nexit = new BlockExit { X = i, Y = j, Direction = exit.Direction, CanBeClosed = exit.CanBeClosed };
					this.exits.Add (nexit);
				}
			}
			foreach (var obj in block.Objects) {
				var nobj = new BlockObject ();
				nobj.ID = obj.ID;
				nobj.Probability = obj.Probability;
				nobj.X = obj.X + x;
				nobj.Y = obj.Y + y;
				this.Objects.Add (nobj);
			}
			if (block.StartPoint != null) {
				this.StartCell.X = block.StartPoint.X + x;
				this.StartCell.Y = block.StartPoint.Y + y;
			}
			if (block.EndPoint != null) {
				this.EndCell.X = block.EndPoint.X + x;
				this.EndCell.Y = block.EndPoint.Y + y;
			}
			this.blockCount++;
			minX = (x < minX) ? (x > 0) ? x : 0 : minX;
			minY = (y < minY) ? (y > 0) ? y : 0 : minY;
			maxX = (x + block.Width > maxX) ? x + block.Width : maxX;
			maxY = (y + block.Height > maxY) ? y + block.Height : maxY;
		}

		public BlockObject StartCell { get; set; }

		public void Trim () {
			var halfTrimSize = 3;
			var trimSize = halfTrimSize * 2;
			var dx = this.maxX - this.minX + trimSize;
			var dy = this.maxY - this.minY + trimSize;
			var data = new string[dx, dy];
			var used = new bool[dx, dy];
			for (var y = 0; y < dy; y++) {
				for (var x = 0; x < dx; x++) {
					try {
						data [x, y] = this.Data [x + minX - halfTrimSize, y + minY - halfTrimSize];
						used [x, y] = this.IsUsed [x + minX - halfTrimSize, y + minY - halfTrimSize];
						if (y == 0 || x == 0 || y == dy - 1 || x == dx - 1) {
							data [x, y] = ".";
						}
					}
					catch (Exception ex) {
						Logger.Debug ("TestLevel", "Trim", "invalid: " + (x + minX - halfTrimSize).ToString () + " " + (y + minY - halfTrimSize).ToString ());
						Logger.Debug ("TestLevel", "Trim", "vs: " + this.Width.ToString () + " " + this.Height.ToString ());
					}
				}
			}

			foreach (var obj in this.Objects) {
				obj.X = obj.X - minX + halfTrimSize;
				obj.Y = obj.Y - minY + halfTrimSize;
			}
			this.StartCell.X = this.StartCell.X - minX + halfTrimSize;
			this.StartCell.Y = this.StartCell.Y - minY + halfTrimSize;
			this.EndCell.X = this.EndCell.X - minX + halfTrimSize;
			this.EndCell.Y = this.EndCell.Y - minY + halfTrimSize;

			this.Data = data;
			this.IsUsed = used;
			this.minX = 0;
			this.maxX = dx;
			this.minY = 0;
			this.maxY = dy;
		}

		public SFML.Graphics.Image ToImage () {
			var res = new SFML.Graphics.Image ((uint)this.Width, (uint)this.Height);
			for (uint y = 0; y < Height; y++) {
				for (uint x = 0; x < Width; x++) {
					switch (this.Data [x, y]) {
						case "#":
							res.SetPixel (x, y, SFML.Graphics.Color.Black);
							break;
						case ".":
							res.SetPixel (x, y, SFML.Graphics.Color.White);
							break;
						default:
							res.SetPixel (x, y, SFML.Graphics.Color.Cyan);
							break;
					}
				}
			}
			res.SetPixel ((uint)this.StartCell.X, (uint)this.StartCell.Y, SFML.Graphics.Color.Green);
			res.SetPixel ((uint)this.EndCell.X, (uint)this.EndCell.Y, SFML.Graphics.Color.Red);
			return res;
		}

		public int UsedArea { get; protected set; }

		public int Width { get { return this.Data.GetLength (0); } }
	}

	public class WorldFactory {
		private XmlDocument xdoc;
		private ProbabilityVector<ConstructionBlock> blocks = new ProbabilityVector<ConstructionBlock> ();
		private Dictionary<string, ConstructionBlock> blocksById = new Dictionary<string, ConstructionBlock> ();
		private List<string> endBlocks = new List<string> ();
		private List<string> startBlocks = new List<string> ();

		public void BuildBlock (XmlNode xblock) {
			var data = Regex.Replace (xblock.InnerText, @"\s+", "");
			var id = XmlUtilities.GetString (xblock, "id");
			var block = new ConstructionBlock (
				            data,
				            XmlUtilities.GetInt (xblock, "width"),
				            XmlUtilities.GetInt (xblock, "height"),
				            XmlUtilities.GetString (xblock, "id")
			            );
			var children = xblock.ChildNodes;
			for (int i = 0; i < children.Count; i++) {
				switch (children [i].Name) {
					case "end":
						block.EndPoint = new BlockObject { 
							ID = "end", 
							X = XmlUtilities.GetInt (children [i], "x"), 
							Y = XmlUtilities.GetInt (children [i], "y"), 
							Probability = 1f
						};
						break;

					case "exit":
						block.AddExit (
							XmlUtilities.GetInt (children [i], "x"),
							XmlUtilities.GetInt (children [i], "y"),
							XmlUtilities.GetString (children [i], "direction"),
							XmlUtilities.GetBool (children [i], "canBeClosed", false)
						);
						break;
				
					case "object":
						block.AddObject (
							XmlUtilities.GetString (children [i], "ref"),
							XmlUtilities.GetInt (children [i], "x"),
							XmlUtilities.GetInt (children [i], "y"),
							XmlUtilities.GetFloat (children [i], "probability"),
							XmlUtilities.GetStringArray (children [i], "variations")
						);
						break;

					case "start":
						block.StartPoint = new BlockObject { 
							ID = "start", 
							X = XmlUtilities.GetInt (children [i], "x"), 
							Y = XmlUtilities.GetInt (children [i], "y"), 
							Probability = 1f
						};
						break;

					default:
						break;
				}
			}
			var pb = XmlUtilities.GetInt (xblock, "occurs");
			this.blocks.Add (block, pb);
			this.blocksById.Add (block.ID, block);
			Logger.Debug ("WorldFactory", "BuildBlock", "Built block:\n" + block.ToString ());
			var variations = XmlUtilities.GetStringArray (xblock, "variations");
			if (Array.IndexOf (variations, "CCW") >= 0) {
				var ccwblock = block.RotateCCW ();
				this.blocks.Add (ccwblock, pb);
				Logger.Debug ("WorldFactory", "BuildBlock", "Built block CCW:\n" + ccwblock.ToString ());
			}
			if (Array.IndexOf (variations, "CW") >= 0) {
				var cwblock = block.RotateCW ();
				this.blocks.Add (cwblock, pb);
				Logger.Debug ("WorldFactory", "BuildBlock", "Built block CW:\n" + cwblock.ToString ());
			}
			if (Array.IndexOf (variations, "F") >= 0) {
				var fblock = block.Flip ();
				this.blocks.Add (fblock, pb);
				Logger.Debug ("WorldFactory", "BuildBlock", "Built block F:\n" + fblock.ToString ());
			}
			if (Array.IndexOf (variations, "FM") >= 0) {
				var fmblock = block.FlipMirror ();
				this.blocks.Add (fmblock, pb);
				Logger.Debug ("WorldFactory", "BuildBlock", "Built block FM:\n" + fmblock.ToString ());
			}
			if (Array.IndexOf (variations, "M") >= 0) {
				var mblock = block.Mirror ();
				this.blocks.Add (mblock, pb);
				Logger.Debug ("WorldFactory", "BuildBlock", "Built block M:\n" + mblock.ToString ());
			}
		}

		public AreaAI BuildAI (XmlNode xblock) {
			var res = new AreaAI ();
			// Enemies
			var enemies = this.xdoc.SelectNodes ("//enemy");
			for (int e = 0; e < enemies.Count; e++) {
				var bp = new EnemyBlueprint (
					         XmlUtilities.GetString (enemies [e], "blueprint"),
					         XmlUtilities.GetInt (enemies [e], "threat")
				         );
				var type = XmlUtilities.GetString (enemies [e], "type");
				switch (type) {
					case "MOB":
						bp.EnemyType = EnemyType.MOB;
						break;
					case "LIGHTNING_BRUISER":
						bp.EnemyType = EnemyType.LIGHTNING_BRUISER;
						break;
					case "GLASS_CANNON":
						bp.EnemyType = EnemyType.GLASS_CANNON;
						break;
					case "MIGHTY_GLACIER":
						bp.EnemyType = EnemyType.MIGHTY_GLACIER;
						break;
					case "STONE_WALL":
						bp.EnemyType = EnemyType.STONE_WALL;
						break;
					case "CASTER":
						bp.EnemyType = EnemyType.CASTER;
						break;
					default:
						break;
				}
				res.enemyBlueprints.Add (bp);
			}
			var xenemies = this.xdoc.SelectSingleNode ("//enemies");
			res.progression = XmlUtilities.GetIntArray (xenemies, "threatProgression");
			res.MaxThreatLevel = XmlUtilities.GetInt (xenemies, "maxThreat", 255);
			res.AlwaysIncreaseThreat = XmlUtilities.GetBool (xenemies, "alwaysIncreaseThreat");
			// Music
			var music = IoManager.LoadMusic (XmlUtilities.GetString (this.xdoc.SelectSingleNode ("//backgroundmusic"), "file"));
			var xloops = this.xdoc.SelectNodes ("//loop");
			for (int i = 0; i < xloops.Count; i++) {
				music.AddLoop (
					XmlUtilities.GetString (xloops [i], "name"),
					XmlUtilities.GetInt (xloops [i], "start"),
					XmlUtilities.GetInt (xloops [i], "end")
				);
				var loop = new MusicLoop { 
					ID = XmlUtilities.GetString (xloops [i], "name"), 
					MaxThreat = XmlUtilities.GetInt (xloops [i], "maxThreat")
				};
				res.MusicLoops.Add (loop);
			}
			res.MusicLoops.Sort ((l1, l2) => l1.MaxThreat.CompareTo (l2.MaxThreat));
			return res;
		}

		public string DefaultTile { get; set; }

		/// <summary>
		/// Gets one of the possible end block at random.
		/// </summary>
		/// <value>The end block.</value>
		protected string EndBlock {
			get {
				return this.endBlocks [Simulator.Instance.Random (this.endBlocks.Count)];
			}
		}

		public string[,] Generate (World res) {
			res.AI = BuildAI (this.xdoc.DocumentElement);
			// Fill the metadata
			/*var enemies = this.xdoc.SelectNodes("//enemy");
			for (int e = 0; e < enemies.Count; e++) {
				var bp = new EnemyBlueprint (
					XmlUtilities.GetString(enemies[e], "blueprint"),
					XmlUtilities.GetInt(enemies[e], "threat")
				);
				var type = XmlUtilities.GetString (enemies [e], "type");
				switch (type) {
				case "MOB":
					bp.EnemyType = EnemyType.MOB;
					break;
				case "LIGHTNING_BRUISER":
					bp.EnemyType = EnemyType.LIGHTNING_BRUISER;
					break;
				case "GLASS_CANNON":
					bp.EnemyType = EnemyType.GLASS_CANNON;
					break;
				case "MIGHTY_GLACIER":
					bp.EnemyType = EnemyType.MIGHTY_GLACIER;
					break;
				case "STONE_WALL":
					bp.EnemyType = EnemyType.STONE_WALL;
					break;
				case "CASTER":
					bp.EnemyType = EnemyType.CASTER;
					break;
				default:
					break;
				}
				res.enemyBlueprints.Add (bp);
			}
			res.AI = new AreaAI ();
			res.AI.progression = XmlUtilities.GetIntArray(this.xdoc.SelectSingleNode("//enemies"), "threatProgression");*/

			// Generate the map
			BufferLevel level = null;
			while (level == null) {
				var iter = 0;
				level = new BufferLevel (MaxWidth, MaxHeight, this.DefaultTile);
				level.CloseTile = this.DefaultTile;
				// Place the first block
				var last = this.blocksById [this.StartBlock];
				var x = Simulator.Instance.Random (MaxWidth, 1);
				var y = Simulator.Instance.Random (MaxHeight, 1);
				level.PlaceBlock (last, x, y);
				// place the other blocks
				while (level.UsedArea < this.MinArea && iter < 1000) {
					if (level.AddRandomBlock (this.blocks) == false) {
						level = null;
						break;
					}
				}
				if (level != null && level.AddBlock (this.blocksById [this.EndBlock]) == false) {
					level = null;
					Logger.Debug ("WorldFactory", "Generate", "Invalid level, regenerating...");
				}
			}
			level.CloseExits ();
			level.Trim ();
			level.CellularAutomata ("asd", "res", 1);
			//level.Dump ();
			var img = level.ToImage ();
			img.SaveToFile (Logger.LOGS_DIRECTORY + "level.png");

			res.SetMap (level.Data);
			res.EndCell = new SFML.Window.Vector2i (level.EndCell.X, level.EndCell.Y);
			res.StartCell = new SFML.Window.Vector2i (level.StartCell.X, level.StartCell.Y);
			var o = 0;
			foreach (var obj in level.Objects) {
				if (Simulator.Instance.Random () < obj.Probability) {
					Logger.Debug ("WorldFactory", "Generate", "Crating object " + obj.ID + " at " + obj.X + "," + obj.Y);
					Simulator.Instance.CreateObject ("g_" + o.ToString (), obj.ID, obj.X, obj.Y);
					o++;
				}
			}

			return level.Data;
		}

		public void Initialize (string levelDefinitionFile) {
			this.xdoc = new XmlDocument ();
			this.xdoc.Load (levelDefinitionFile);
			try {
				var xlevel = this.xdoc.SelectSingleNode ("//level");
				this.MaxWidth = XmlUtilities.GetInt (xlevel, "maxWidth");
				this.MaxHeight = XmlUtilities.GetInt (xlevel, "maxHeight");
				this.MinArea = XmlUtilities.GetInt (xlevel, "minArea");
				this.endBlocks = new List<string> (XmlUtilities.GetStringArray (xlevel, "end"));
				this.startBlocks = new List<string> (XmlUtilities.GetStringArray (xlevel, "start"));
				var xblocks = this.xdoc.SelectNodes ("//block");
				for (var i = 0; i < xblocks.Count; i++) {
					this.BuildBlock (xblocks [i]);
				}
				var xtiles = this.xdoc.SelectSingleNode ("//tiles");
				this.DefaultTile = XmlUtilities.GetString (xtiles, "default");
			}
			catch (Exception ex) {
				Logger.Error ("WorldFactory", "Initialize", ex.ToString ());
			}
		}

		protected int MaxHeight { get; set; }

		protected int MaxWidth { get; set; }

		protected int MinArea { get; set; }

		/// <summary>
		/// Gets one of the possible start block at random.
		/// </summary>
		/// <value>The start block.</value>
		protected string StartBlock {
			get {
				return this.startBlocks [Simulator.Instance.Random (this.startBlocks.Count)];
			}
		}
	}
}

