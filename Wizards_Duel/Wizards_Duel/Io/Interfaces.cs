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

		void OnMouseMove(object sender, MouseMoveEventArgs args);

		void OnMousePressed(object sender, MouseButtonEventArgs args);

		void OnMouseReleased(object sender, MouseButtonEventArgs args);
	}

	public interface ITextArea: IClickable {
		bool HasFocus { get; set; }

		void OnKeyPressed(object sender, KeyEventArgs args);

		void OnKeyReleased(object sender, KeyEventArgs args);

		void OnTextEntered(object sender, TextEventArgs args);
	}
}

