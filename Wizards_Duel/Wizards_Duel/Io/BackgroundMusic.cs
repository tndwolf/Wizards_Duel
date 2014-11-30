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
using System.Collections.Generic;
using SFML.Audio;
using WizardsDuel.Utils;

namespace WizardsDuel.Io
{
	public class BackgroundMusic {
		protected struct Loop {
			public Loop(TimeSpan start, TimeSpan end) {
				this.start = start;
				this.end = end;
			}

			public TimeSpan start;
			public TimeSpan end;
		}

		public const string MAIN_LOOP = "MAIN_LOOP";

		string fileName;
		Dictionary<string, Loop> loops = new Dictionary<string, Loop>();
		Sound music;
		string nextLoop;
		long endLoopTime = 0;
		long timeRef = 0;

		public BackgroundMusic (Sound music, string fileName) {
			this.music = music;
			this.fileName = fileName;
			music.Loop = false;
			this.AddLoop (MAIN_LOOP, 0, 100);//(int)music.Duration.TotalMilliseconds);
			this.nextLoop = MAIN_LOOP;
		}

		public void AddLoop(string name, int start, int end) {
			var loop = new Loop (TimeSpan.FromMilliseconds(start), TimeSpan.FromMilliseconds(end - start));
			this.loops[name] = loop;
		}

		public string FileName {
			get { return this.fileName; }
		}

		public void Play(/*long refTime*/) {
			/// TODO
			/*var loop = this.loops [this.nextLoop];
			this.music.PlayingOffset = loop.start;
			this.nextLoopTime = (long)loop.end.TotalMilliseconds;
			this.music.Play ();
			Logger.Info ("BackgroundMusic", "Play","Playing loop " + this.nextLoop);*/
			//this.startLoopTime = refTime;
		}

		public void Play(string name, long refTime) {
			Loop next;
			if (this.loops.TryGetValue (name, out next)) {
				this.nextLoop = name;
				Logger.Debug ("BackgroundMusic", "Play", "Next ms " + next.end.TotalMilliseconds.ToString());
				this.endLoopTime = refTime + (long)next.end.TotalMilliseconds;
				Logger.Debug ("BackgroundMusic", "Play", "Set next change in " + this.endLoopTime.ToString());
				this.music.PlayingOffset = next.start;
				this.music.Play ();
				Logger.Info ("BackgroundMusic", "Play", "Playing loop " + this.nextLoop + " from " + next.start.ToString());
			} else {
				Logger.Warning ("BackgroundMusic", "Play", "No loop found named " + name);
			}
		}

		public void SetNextLoop(string name) {
			Loop next;
			if (this.loops.TryGetValue(name, out next)) {
				this.nextLoop = name;
				Logger.Debug ("BackgroundMusic", "SetNextLoop", "Set next change in " + this.endLoopTime.ToString());
			}
		}

		public void Update(long refTime) {
			//Logger.Debug ("BackgroundMusic", "Update", this.timeRef.ToString() + " vs " + this.endLoopTime.ToString());
			var delta = refTime - this.endLoopTime;
			Logger.Debug ("BackgroundMusic", "Update", refTime.ToString() + " vs " + this.endLoopTime.ToString());
			if (delta >= 0) {
				var loop = this.loops [this.nextLoop];
				this.music.PlayingOffset = loop.start;
				this.timeRef = delta;
				this.endLoopTime = refTime + (long)loop.end.TotalMilliseconds;
				Logger.Debug ("BackgroundMusic", "Update", "Playing loop " + this.nextLoop + " from " + loop.start.ToString());
				Logger.Debug ("BackgroundMusic", "Update", "Tstart " + this.timeRef.ToString() + " Tend " + this.endLoopTime.ToString());
			}
		}
	}
}
