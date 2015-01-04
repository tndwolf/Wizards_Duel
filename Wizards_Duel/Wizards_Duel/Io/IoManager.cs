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

	public static class IO {
		public const string DEFAULT_FONT = "";

		private static string ASSETS_DIRECTORY = "Assets" + Path.DirectorySeparatorChar;

		public static string LIGHT_TEXTURE_ID = "shaded_circle";
		public static int LIGHT_TEXTRE_MAX_RADIUS = 128;
		public static int LIGHT_TEXTRE_MIN_RADIUS = 96;

		private static RenderWindow window;
		private static Dictionary<string, Font> fonts = new Dictionary<string, Font>();
		private static Dictionary<string, Sound> sounds = new Dictionary<string, Sound>();
		private static Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
		private static BackgroundMusic music = null;
		private static Frame root = new Frame();

		private static Inputs inputs = new Inputs();
		private static Inputs inputRes = new Inputs();
		private static Stopwatch clock = new Stopwatch ();
		private static long refTime = 0;
		private static long refreshTime = 20;

		/// <summary>
		/// Appends the widget to the drawing pipeline
		/// </summary>
		/// <param name="widget">Widget.</param>
		public static void AddWidget(Widget widget) {
			root.AddWidget (widget);
		}

		private static void CheckKeyboard() {
			if (Keyboard.IsKeyPressed (Keyboard.Key.Left) || Keyboard.IsKeyPressed (Keyboard.Key.Numpad4)) {
				IO.inputs.Command = InputCommands.LEFT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Right) || Keyboard.IsKeyPressed (Keyboard.Key.Numpad6)) {
				IO.inputs.Command = InputCommands.RIGHT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Up) || Keyboard.IsKeyPressed (Keyboard.Key.Numpad8)) {
				IO.inputs.Command = InputCommands.UP;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Down) || Keyboard.IsKeyPressed (Keyboard.Key.Numpad2)) {
				IO.inputs.Command = InputCommands.DOWN;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Numpad7)) {
				IO.inputs.Command = InputCommands.UP_LEFT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Numpad9)) {
				IO.inputs.Command = InputCommands.UP_RIGHT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Numpad1)) {
				IO.inputs.Command = InputCommands.DOWN_LEFT;
			}
			else if (Keyboard.IsKeyPressed (Keyboard.Key.Numpad3)) {
				IO.inputs.Command = InputCommands.DOWN_RIGHT;
			}
		}

		/// <summary>
		/// Creates a shaded circle with shaded from white (center) to transparent (outer edges).
		/// It uses a quadratic falloff function.
		/// </summary>
		/// <returns>The shaded circle texture.</returns>
		public static Texture CreateShadedCircle2() {
			Texture tex;
			if (IO.textures.TryGetValue (IO.LIGHT_TEXTURE_ID, out tex) == false) {
				var R_MAX = IO.LIGHT_TEXTRE_MAX_RADIUS;
				var r_min = IO.LIGHT_TEXTRE_MIN_RADIUS;
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
				IO.textures.Add (IO.LIGHT_TEXTURE_ID, tex);
			}
			return tex;
		}

		public static void DeleteWidget(Widget widget) {
			IO.root.DeleteWidget(widget);
		}

		/// <summary>
		/// Draw this instance.
		/// </summary>
		public static void Draw() {
			var delta = IO.clock.ElapsedMilliseconds - IO.refTime;
			if (delta > IO.refreshTime) {
				window.Clear ();
				root.Draw (window);
				window.Display ();
				IO.refTime = IO.clock.ElapsedMilliseconds;
				if (IO.music != null) IO.music.Update (IO.GetTime());
			}
		}

		/// <summary>
		/// Returns the milliseconds since the last call of Output.Draw()
		/// </summary>
		/// <returns>The delta.</returns>
		public static int GetDelta() {
			return (int)(IO.clock.ElapsedMilliseconds - IO.refTime);
		}

		public static Inputs GetInputs() {
			window.DispatchEvents ();
			IO.CheckKeyboard (); // KeyPressed is unrealiable, it inserts delays
			inputRes.Command = inputs.Command;
			inputRes.Unicode = inputs.Unicode;
			inputRes.MouseX = inputs.MouseX;
			inputRes.MouseY = inputs.MouseY;
			inputs.Command = InputCommands.NONE;
			inputs.Unicode = "";
			return inputRes;
		}

		/// <summary>
		/// Returns the number of milliseconds since the initialization
		/// </summary>
		/// <returns>The time.</returns>
		public static long GetTime() {
			return (long)IO.refTime;
		}

		/// <summary>
		/// Manage keypressed events
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Arguments.</param>
		private static void KeyPressed(object sender, KeyEventArgs e) {
			if (e.Code == Keyboard.Key.Left || e.Code == Keyboard.Key.Numpad4) {
				IO.inputs.Command = InputCommands.LEFT;
			}
			else if (e.Code == Keyboard.Key.Right || e.Code == Keyboard.Key.Numpad6) {
				IO.inputs.Command = InputCommands.RIGHT;
			}
			else if (e.Code == Keyboard.Key.Up || e.Code == Keyboard.Key.Numpad8) {
				IO.inputs.Command = InputCommands.UP;
			}
			else if (e.Code == Keyboard.Key.Down || e.Code == Keyboard.Key.Numpad2) {
				IO.inputs.Command = InputCommands.DOWN;
			}
			else if (e.Code == Keyboard.Key.Numpad7) {
				IO.inputs.Command = InputCommands.UP_LEFT;
			}
			else if (e.Code == Keyboard.Key.Numpad9) {
				IO.inputs.Command = InputCommands.UP_RIGHT;
			}
			else if (e.Code == Keyboard.Key.Numpad1) {
				IO.inputs.Command = InputCommands.DOWN_LEFT;
			}
			else if (e.Code == Keyboard.Key.Numpad3) {
				IO.inputs.Command = InputCommands.DOWN_RIGHT;
			}
			else if (e.Code == Keyboard.Key.Tab) {
				IO.inputs.Command = InputCommands.TOGGLE_GRID;
			}
		}

		public static void Initialize(string title, int width=800, int height=600) {
			window = new RenderWindow(new VideoMode((uint)width, (uint)height, 32), title);
			window.Closed += new EventHandler(IO.OnClosed);
			window.MouseMoved += new EventHandler<MouseMoveEventArgs> (IO.MouseMove);
			window.MouseButtonPressed += new EventHandler<MouseButtonEventArgs> (IO.MousePressed);
			window.KeyPressed += new EventHandler<KeyEventArgs>(IO.KeyPressed);
			window.TextEntered += new EventHandler<TextEventArgs> (IO.TextEntered);
			IO.FPS = 60f;
			IO.clock.Start ();
		}

		/// <summary>
		/// Loads a font from file.
		/// </summary>
		/// <returns>The font.</returns>
		/// <param name="fontFile">Font file.</param>
		/// <param name="size">The font Size, it is in fact used to set the smoothin for a specific mipmap.</param>
		public static Font LoadFont(string fontFile, int size = 24) {
			Font font;
			if (IO.fonts.TryGetValue (fontFile, out font) == false) {
				font = new Font (ASSETS_DIRECTORY + fontFile);
				IO.fonts.Add (fontFile, font);
			}
			font.GetTexture ((uint)size).Smooth = false;
			return font;
		}


		/// <summary>
		/// Loads the background music. The music is streamed from file.
		/// </summary>
		/// <param name="fileName">File name.</param>
		public static BackgroundMusic LoadMusic(string fileName) {
			//var music = new SFML.Audio.Music("Assets\\WIZARDDUEL_WD_indietheme_8bit.ogg");
			//music.Play ();
			//return null;
			try {
				if (IO.music != null && IO.music.FileName == fileName) {
					Logger.Info ("IO", "LoadMusic","Already loaded " + fileName);
				} else {
					//var mus = new Music(ASSETS_DIRECTORY + fileName);
					var b = new SoundBuffer(ASSETS_DIRECTORY + fileName);
					var mus = new Sound();
					mus.SoundBuffer = new SoundBuffer(ASSETS_DIRECTORY + fileName);
					IO.music = new BackgroundMusic(mus, fileName);
					Logger.Info ("IO", "LoadMusic","loaded music " + fileName);
				}
				return IO.music;
			} catch (Exception ex) {
				Logger.Warning ("IO", "LoadMusic", "Unable to load " + fileName + ": " + ex.ToString());
				return null;
			}
		}

		/// <summary>
		/// Loads a sound effect from file.
		/// </summary>
		/// <returns>The sfx.</returns>
		/// <param name="soundFile">Sound file.</param>
		public static Sound LoadSound(string soundFile) {
			Sound sound;
			if (IO.sounds.TryGetValue (soundFile, out sound) == false) {
				sound = new Sound ();
				sound.SoundBuffer = new SoundBuffer (ASSETS_DIRECTORY + soundFile);
				IO.sounds.Add (soundFile, sound);
			}
			return sound;
		}

		/// <summary>
		/// Loads a texture from file.
		/// </summary>
		/// <returns>The texture.</returns>
		/// <param name="texFile">Texture file.</param>
		public static Texture LoadTexture(string texFile, bool smooth = false) {
			Texture tex;
			if (IO.textures.TryGetValue (texFile, out tex) == false) {
				tex = new Texture (ASSETS_DIRECTORY + texFile);
				tex.Smooth = smooth;
				IO.textures.Add (texFile, tex);
			}
			return tex;
		}

		/// <summary>
		/// Manage mouse move events
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event.</param>
		private static void MouseMove(object sender, MouseMoveEventArgs e) {
			IO.inputs.MouseX = e.X;
			IO.inputs.MouseY = e.Y;
		}

		/// <summary>
		/// Manage mouse pressed event
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event.</param>
		private static void MousePressed(object sender, MouseButtonEventArgs e) {
			if (e.Button == Mouse.Button.Left) {
				IO.inputs.Command = InputCommands.MOUSE_LEFT;
			}
			else if (e.Button == Mouse.Button.Right) {
				IO.inputs.Command = InputCommands.MOUSE_RIGHT;
			}
		}

		/// <summary>
		/// Manage window close event
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event.</param>
		private static void OnClosed(object sender, EventArgs e) {
			IO.inputs.Command = InputCommands.QUIT;
			var window = (RenderWindow)sender;
			window.Close();
		}

		public static void PlayMusic(string loop) {
			if (IO.music != null) {
				IO.music.Play (loop, IO.GetTime());
			} else {
				Logger.Info ("IO", "PlayMusic", "No Music loaded");
			}
		}

		public static void SetNextMusicLoop(string loop) {
			if (IO.music != null) {
				IO.music.SetNextLoop (loop);
			}
		}

		/// <summary>
		/// Plays a sound. The sopund must have been loaded with LoadSound
		/// </summary>
		/// <param name="soundFile">Sound file.</param>
		public static void PlaySound(string soundFile) {
			try {
				IO.sounds[soundFile].Play();
			} catch (Exception ex) {
				Logger.Warning ("IO", "PlaySound", "Unable to play " + soundFile + ": " + ex.ToString());
			}
		}

		private static void TextEntered(object sender, TextEventArgs e) {
			Logger.Info ("IO", "TextEntered", "Text " + e.Unicode);
			IO.inputs.Unicode = e.Unicode;
			Logger.Info ("IO", "TextEntered", "Text " + IO.inputs.Unicode);
		}

		/// <summary>
		/// Sets the FPS limit
		/// </summary>
		public static float FPS {
			set { IO.refreshTime = (long)(1f / value * 1000f);}
		}

	}
}

