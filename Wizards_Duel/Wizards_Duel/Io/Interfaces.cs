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
using SFML.Window;
using SFML.Graphics;

namespace WizardsDuel.Io
{
	public interface IClickable {
		bool Enabled { get; set; }

		/// <summary>
		/// The Bounding Box is used to intercept mouse clicks. Usually
		/// the Bounging Box should be updated at each call of Draw by
		/// running:
		/// OffsetPosition = states.Transform.TransformPoint(OffsetPosition);
		/// In this way the correct position will be used when checking
		/// mouse inputs
		/// </summary>
		/// <value>The bounding box.</value>
		Vector2f OffsetPosition { get; set; }

		void OnMouseMove(object sender, MouseMoveEventArgs e);

		void OnMousePressed(object sender, MouseButtonEventArgs e);

		void OnMouseReleased(object sender, MouseButtonEventArgs e);
	}

	public interface ITextArea: IClickable {
		bool HasFocus { get; set; }

		void OnKeyPressed(object sender, KeyEventArgs e);

		void OnKeyReleased(object sender, KeyEventArgs e);

		void OnTextEntered(object sender, TextEventArgs e);
	}
}

