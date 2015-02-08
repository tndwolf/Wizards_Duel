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
using WizardsDuel.Game;

namespace WizardsDuel.Utils
{
	public class ProbabilityVector<T>
	{
		private Dictionary<T, int> items = new Dictionary<T, int>();
		private int totalOccurrencies;

		public void Add(T item, int occurrencies = 1) {
			if (occurrencies < 1) {
				return;
			} else if (this.items.ContainsKey (item)) {
				this.items [item] += occurrencies;
			} else {
				this.items [item] = occurrencies;
			}
			this.totalOccurrencies += occurrencies;
		}

		public void Clear() {
			this.items.Clear ();
			this.totalOccurrencies = 0;
		}

		/// <summary>
		/// Returns the vector as a dictionary of (item, occurrencies) pairs
		/// </summary>
		/// <value>The dictionary.</value>
		public Dictionary<T, int> Dictionary {
			get { return this.items; }
		}

		/// <summary>
		/// Returns the number of items, not thir occurrencies
		/// </summary>
		/// <value>The item count.</value>
		public int ItemCount {
			get { return this.items.Keys.Count; }
		}

		/// <summary>
		/// Returns a random instance contained by the vector, or default(T) if the
		/// container is empty
		/// </summary>
		public T Random() {
			var reference = Simulator.Instance.Random(this.totalOccurrencies);
			var counter = 0;
			foreach (var pair in this.items) {
				counter += pair.Value;
				if (reference < counter) {
					return pair.Key;
				}
			}
			// this cannot happen if at least one item is defined
			return default(T);
		}

		public void Remove(T item) {
			if (this.items.ContainsKey (item)) {
				this.totalOccurrencies -= this.items [item];
				this.items.Remove (item);
			}
		}
	}
}

