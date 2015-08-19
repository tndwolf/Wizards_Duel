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
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using SFML.Audio;
using SFML.Graphics;
using SFML.Window;
using WizardsDuel.Utils;

namespace WizardsDuel.Io
{
	public enum InputCommands {
		NONE,
		LEFT,
		RIGHT,
		UP,
		DOWN,
		UP_LEFT,
		UP_RIGHT,
		DOWN_LEFT,
		DOWN_RIGHT,
		RETURN,
		CANCEL,
		TOGGLE_GRID,
		MOUSE_LEFT,
		MOUSE_RIGHT,
		QUIT,
		COUNT
	}

	public class Inputs {
		public InputCommands Command = InputCommands.NONE;
		public string Unicode = "";
		public float MouseX = 0f;
		public float MouseY = 0f;
	}

	public static class IoManager {
		public const string DEFAULT_FONT = "";
		public const string DEFAULT_WIDGET_ID = "DWID";

		private static string ASSETS_DIRECTORY = "Assets" + Path.DirectorySeparatorChar;

		public static string LIGHT_TEXTURE_ID = "shaded_circle";
		public static int LIGHT_TEXTRE_MAX_RADIUS = 128;
		public static int LIGHT_TEXTRE_MIN_RADIUS = 32;

		/*private static RenderWindow window;
		private static Dictionary<string, Font> fonts = new Dictionary<string, Font>();
		private static Dictionary<string, Sound> sounds = new Dictionary<string, Sound>();
		private static Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
		private static BackgroundMusic music = null;
		private static Frame root = new Frame();*/

		private static Inputs inputs = new Inputs();
		private static Inputs inputRes = new Inputs();
		/*private static Stopwatch clock = new Stopwatch ();
		private static long refTime = 0;
		private static long refreshTime = 20;*/

		static Stopwatch clock;
		static long deltaTime;
		static long frameTime;
		static Dictionary<string, Font> fonts = new Dictionary<string, Font>();
		static BackgroundMusic music;
		static Dictionary<string, Widget> namedWidgets = new Dictionary<string, Widget>();
		static long referenceTime;
		static Frame root;
		static Dictionary<string, Sound> sounds = new Dictionary<string, Sound>();
		static Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
		static RenderWindow window;

		/// <summary>
		/// Adds the widget to the drawing pipeline. If the widget is clickable or
		/// accepts text inputs it will be added to the event dispatching queue
		/// </summary>
		/// <param name="widget">The Widget.</param>
		/// <param name="wid">A unique ID for the Widget. Existing ones are overwritren</param>
		static public void AddWidget(Widget widget, string wid = DEFAULT_WIDGET_ID) {
			IoManager.root.AddWidget(widget);
			if (wid != DEFAULT_WIDGET_ID) {
				IoManager.namedWidgets.Add (wid, widget);
			}

			var clickable = widget as IClickable;
			if (clickable != null) {
				IoManager.window.MouseMoved += clickable.OnMouseMove;
				IoManager.window.MouseButtonPressed += clickable.OnMousePressed;
				IoManager.window.MouseButtonReleased += clickable.OnMouseReleased;
			}
			var textarea = widget as ITextArea;
			if (textarea != null) {
				IoManager.window.KeyPressed += textarea.OnKeyPressed;
				IoManager.window.KeyReleased += textarea.OnKeyReleased;
				IoManager.window.TextEntered += textarea.OnTextEntered; 
			}
		}

		static public string AssetDirectory { get; set; }

		static public void Clear() {
			IoManager.ClearWidgets();
			IoManager.ClearAssets();
		}

		static public void ClearAssets() {
			IoManager.fonts.Clear();
			IoManager.textures.Clear();
			IoManager.sounds.Clear();
		}

		static public void ClearWidgets() {
			foreach (var widget in IoManager.root) {
				// unsubscribe all the widgets before clearing them
				var clickable = widget as IClickable;
				if (clickable != null) {
					IoManager.window.MouseMoved -= clickable.OnMouseMove;
					IoManager.window.MouseButtonPressed -= clickable.OnMousePressed;
					IoManager.window.MouseButtonReleased -= clickable.OnMouseReleased;
				}
				var textarea = widget as ITextArea;
				if (textarea != null) {
					IoManager.window.KeyPressed -= textarea.OnKeyPressed;
					IoManager.window.KeyReleased -= textarea.OnKeyReleased;
					IoManager.window.TextEntered -= textarea.OnTextEntered; 
				}
			}
			IoManager.root.Clear();
			IoManager.namedWidgets.Clear ();
		}

		private static void CheckKeyboard() {
			if (Keyboard.IsKeyPressed (Keyboard.Key.A) || Keyboard.IsKeyPressed (Keyboard.Key.Left) || Keyboard.IsKeyPressed (Keyboard.Key.Numpad4)) {
				IoManager.inputs.Command = InputCommands.LEFT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.D) || Keyboard.IsKeyPressed (Keyboard.Key.Right) || Keyboard.IsKeyPressed (Keyboard.Key.Numpad6)) {
				IoManager.inputs.Command = InputCommands.RIGHT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.W) || Keyboard.IsKeyPressed (Keyboard.Key.Up) || Keyboard.IsKeyPressed (Keyboard.Key.Numpad8)) {
				IoManager.inputs.Command = InputCommands.UP;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.S) || Keyboard.IsKeyPressed (Keyboard.Key.Down) || Keyboard.IsKeyPressed (Keyboard.Key.Numpad2)) {
				IoManager.inputs.Command = InputCommands.DOWN;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Numpad7)) {
				IoManager.inputs.Command = InputCommands.UP_LEFT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Numpad9)) {
				IoManager.inputs.Command = InputCommands.UP_RIGHT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Numpad1)) {
				IoManager.inputs.Command = InputCommands.DOWN_LEFT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Numpad3)) {
				IoManager.inputs.Command = InputCommands.DOWN_RIGHT;
			}
		}

		/// <summary>
		/// Creates a shaded circle with shaded from white (center) to transparent (outer edges).
		/// It uses a quadratic falloff function.
		/// </summary>
		/// <returns>The shaded circle texture.</returns>
		public static Texture CreateShadedCircle() {
			Texture tex;
			if (IoManager.textures.TryGetValue (IoManager.LIGHT_TEXTURE_ID, out tex) == false) {
				var R_MAX = IoManager.LIGHT_TEXTRE_MAX_RADIUS;
				var r_min = IoManager.LIGHT_TEXTRE_MIN_RADIUS;
				var image = new Image ((uint)R_MAX * 2, (uint)R_MAX * 2, Color.Black);
				// fill with a shaded circle
				// of course this is not optimized (no simmetry, no reuse of variables...)
				// but it should be more clear this way
				for (uint y = 0; y < R_MAX * 2; y++) {
					for (uint x = 0; x < R_MAX * 2; x++) {
						var dx = (R_MAX - x);
						var dy = (R_MAX - y);
						var r = (int)Math.Sqrt (dx * dx + dy * dy);
						if (r > R_MAX) {
							r = R_MAX;
						} else if (r < r_min) {
							r = 0;
						} else {
							r = r * R_MAX / (R_MAX - r_min) - r_min * R_MAX / (R_MAX - r_min);
						}
						var c = (byte)(255 * (R_MAX-r)*(R_MAX-r) / (R_MAX * R_MAX));
						var color = new Color (255, 255, 255, c);
						image.SetPixel (x, y, color);
					}
				}
				tex = new Texture (image);
				tex.Smooth = true;
				IoManager.textures.Add (IoManager.LIGHT_TEXTURE_ID, tex);
			}
			return tex;
		}

		static public void Draw() {
			var time = IoManager.clock.ElapsedMilliseconds;
			var delta = time - IoManager.referenceTime;
			if (delta > IoManager.frameTime) {
				IoManager.window.DispatchEvents();
				IoManager.window.Clear();
				IoManager.deltaTime = delta;
				IoManager.referenceTime = time;
				IoManager.window.Draw(IoManager.root);
				IoManager.window.Display();
				if (IoManager.music != null) IoManager.music.Update (IoManager.Time);
			}
			else {
				// Wait a bit, no need to use 100% CPU
				System.Threading.Thread.Sleep((int)(IoManager.frameTime - delta));
			}
		}

		static public Font DefaultFont { 
			get { return IoManager.LoadFont(IoManager.DefaultFontId); }
		}

		static public string DefaultFontId { 
			get; 
			set;
		}

		/// <summary>
		/// Returns the number of milliseconds since the last call of Draw.
		/// </summary>
		static public int DeltaTime { 
			get { return (int)deltaTime; }
		}

		/// <summary>
		/// Sets how many times per second to update the scene
		/// </summary>
		static public int FPS { 
			set { IoManager.frameTime = 1000 / value; }
		}

		static private string GetAssetPath(string file) {
			return IoManager.AssetDirectory + System.IO.Path.DirectorySeparatorChar + file;
		}

		public static Inputs GetInputs() {
			window.DispatchEvents ();
			IoManager.CheckKeyboard (); // KeyPressed is unrealiable, it inserts delays
			inputRes.Command = inputs.Command;
			inputRes.Unicode = inputs.Unicode;
			inputRes.MouseX = inputs.MouseX;
			inputRes.MouseY = inputs.MouseY;
			inputs.Command = InputCommands.NONE;
			inputs.Unicode = "";
			return inputRes;
		}

		/// <summary>
		/// Gets a named widget.
		/// </summary>
		/// <returns>The widget.</returns>
		/// <param name="wid">Widget unique ID as set with AddWidget.</param>
		public static Widget GetWidget(string wid) {
			return IoManager.namedWidgets [wid];
		}

		/// <summary>
		/// Manage keypressed events
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Arguments.</param>
		private static void KeyPressed(object sender, KeyEventArgs e) {
			if (e.Code == Keyboard.Key.Left || e.Code == Keyboard.Key.Numpad4) {
				IoManager.inputs.Command = InputCommands.LEFT;
			}
			else if (e.Code == Keyboard.Key.Right || e.Code == Keyboard.Key.Numpad6) {
				IoManager.inputs.Command = InputCommands.RIGHT;
			}
			else if (e.Code == Keyboard.Key.Up || e.Code == Keyboard.Key.Numpad8) {
				IoManager.inputs.Command = InputCommands.UP;
			}
			else if (e.Code == Keyboard.Key.Down || e.Code == Keyboard.Key.Numpad2) {
				IoManager.inputs.Command = InputCommands.DOWN;
			}
			else if (e.Code == Keyboard.Key.Numpad7) {
				IoManager.inputs.Command = InputCommands.UP_LEFT;
			}
			else if (e.Code == Keyboard.Key.Numpad9) {
				IoManager.inputs.Command = InputCommands.UP_RIGHT;
			}
			else if (e.Code == Keyboard.Key.Numpad1) {
				IoManager.inputs.Command = InputCommands.DOWN_LEFT;
			}
			else if (e.Code == Keyboard.Key.Numpad3) {
				IoManager.inputs.Command = InputCommands.DOWN_RIGHT;
			}
			else if (e.Code == Keyboard.Key.Tab) {
				IoManager.inputs.Command = InputCommands.TOGGLE_GRID;
			}
		}

		public static void Initialize(string title, int width=800, int height=600) {
			IoManager.window = new RenderWindow(new VideoMode((uint)width, (uint)height, 32), title);
			IoManager.window.Closed += IoManager.OnClosed;
			IoManager.IsRunning = true;
			IoManager.AssetDirectory = "Assets";
			IoManager.root = new Frame();
			IoManager.clock = new Stopwatch();
			IoManager.clock.Start();
			IoManager.FPS = 60;

			//IoManager.root.X = 64;

			IoManager.window.Closed += new EventHandler(IoManager.OnClosed);
			//IoManager.window.MouseMoved += new EventHandler<MouseMoveEventArgs> (IoManager.MouseMove);
			IoManager.window.MouseButtonPressed += new EventHandler<MouseButtonEventArgs> (IoManager.MousePressed);
			IoManager.window.KeyPressed += new EventHandler<KeyEventArgs>(IoManager.KeyPressed);
			IoManager.window.TextEntered += new EventHandler<TextEventArgs> (IoManager.TextEntered);
		}

		static public bool IsRunning { 
			get; 
			set;
		}

		/// <summary>
		/// Loads a font from file.
		/// </summary>
		/// <returns>The font.</returns>
		/// <param name="fileName">Font file.</param>
		/// <param name="size">The font Size, it is in fact used to set the smoothin for a specific mipmap.</param>
		public static Font LoadFont(string fileName, int size = 24) {
			Font res;
			try {
				if (IoManager.fonts.TryGetValue (fileName, out res) == false) {
					var path = IoManager.GetAssetPath(fileName);
					res = new Font(path);
					IoManager.fonts.Add(fileName, res);
					if (IoManager.fonts.Count == 1) {
						IoManager.DefaultFontId = fileName;
					}
				}
				res.GetTexture ((uint)size).Smooth = false;
			} catch (Exception ex) {
				Logger.Warning ("IO", "LoadFont", "Unable to load " + fileName + ": " + ex.ToString());
				res = null;
			}
			return res;
		}


		/// <summary>
		/// Loads the background music. The music is streamed from file.
		/// </summary>
		/// <param name="fileName">File name.</param>
		public static BackgroundMusic LoadMusic(string fileName) {
			try {
				if (IoManager.music != null && IoManager.music.FileName == fileName) {
					Logger.Info ("IO", "LoadMusic","Already loaded " + fileName);
				} else {
					var path = IoManager.GetAssetPath(fileName);
					var mus = new Sound();
					mus.SoundBuffer = new SoundBuffer(path);
					IoManager.music = new BackgroundMusic(mus, fileName);
					Logger.Info ("IO", "LoadMusic","loaded music " + fileName);
				}
				return IoManager.music;
			} catch (Exception ex) {
				Logger.Warning ("IO", "LoadMusic", "Unable to load " + fileName + ": " + ex.ToString());
				return null;
			}
		}

		/// <summary>
		/// Loads a sound effect from file.
		/// </summary>
		/// <returns>The sfx.</returns>
		public static Sound LoadSound(string fileName) {
			Sound res;
			if (IoManager.sounds.TryGetValue(fileName, out res) == false) {
				try {
					var path = IoManager.GetAssetPath(fileName);
					res = new Sound(new SoundBuffer(path));
					res.Volume = 5;
					IoManager.sounds.Add(fileName, res);
				}
				catch (Exception ex) {
					// TODO log exception
					Console.Write(ex.ToString());
					res = null;
				}
			}
			return res;
		}

		/// <summary>
		/// Loads a texture from file.
		/// </summary>
		/// <returns>The texture.</returns>
		public static Texture LoadTexture(string fileName, bool smooth = false) {
			Texture res;
			if (IoManager.textures.TryGetValue(fileName, out res) == false) {
				try {
					var path = IoManager.GetAssetPath(fileName);
					res = new Texture(path);
					res.Smooth = smooth;
					IoManager.textures.Add(fileName, res);
				}
				catch (Exception ex) {
					// TODO log exception
					Console.Write(ex.ToString());
					res = null;
				}
			}
			return res;
		}

		/// <summary>
		/// Manage mouse move events
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event.</param>
		private static void MouseMove(object sender, MouseMoveEventArgs e) {
			IoManager.inputs.MouseX = e.X;
			IoManager.inputs.MouseY = e.Y;
		}

		/// <summary>
		/// Manage mouse pressed event
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event.</param>
		private static void MousePressed(object sender, MouseButtonEventArgs e) {
			if (e.Button == Mouse.Button.Left) {
				IoManager.inputs.Command = InputCommands.MOUSE_LEFT;
			}
			else if (e.Button == Mouse.Button.Right) {
				IoManager.inputs.Command = InputCommands.MOUSE_RIGHT;
			}
		}

		/// <summary>
		/// Manage window close event
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event.</param>
		private static void OnClosed(object sender, EventArgs e) {
			IoManager.inputs.Command = InputCommands.QUIT;
			((RenderWindow)sender).Close();
			IoManager.IsRunning = false;
		}

		public static void PlayMusic(string loop) {
			if (IoManager.music != null) {
				IoManager.music.Play (loop, IoManager.Time);
			} else {
				Logger.Info ("IO", "PlayMusic", "No Music loaded");
			}
		}

		public static void SetNextMusicLoop(string loop) {
			if (IoManager.music != null) {
				IoManager.music.SetNextLoop (loop);
			}
		}

		/// <summary>
		/// Plays a sound. The sopund must have been loaded with LoadSound
		/// </summary>
		/// <param name="soundFile">Sound file.</param>
		public static void PlaySound(string soundFile) {
			try {
				IoManager.sounds[soundFile].Play();
			} catch (Exception ex) {
				Logger.Warning ("IO", "PlaySound", "Unable to play " + soundFile + ": " + ex.ToString());
			}
		}

		public static void SetSize(int width, int height) {
			IoManager.window.Size = new Vector2u ((uint)width, (uint)height);
		}

		private static void TextEntered(object sender, TextEventArgs e) {
			Logger.Info ("IO", "TextEntered", "Text " + e.Unicode);
			IoManager.inputs.Unicode = e.Unicode;
			Logger.Info ("IO", "TextEntered", "Text " + IoManager.inputs.Unicode);
		}

		/// <summary>
		/// Returns the current reference time, that is, the number of milliseconds from
		/// start since the last call of Draw.
		/// </summary>
		public static long Time {
			get { return referenceTime; }
		}
	}
}

