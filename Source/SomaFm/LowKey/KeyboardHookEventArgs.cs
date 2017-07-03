using System;
using System.Windows.Forms;

namespace SomaFm.LowKey
{
	/// <summary>
	///    Gives information about a hot key when an event is fired.
	/// </summary>
	public class KeyboardHookEventArgs : EventArgs
	{
		// ReSharper disable once MemberCanBePrivate.Global
		// ReSharper disable once NotAccessedField.Global
		public readonly Keys Key;

		// ReSharper disable once MemberCanBePrivate.Global
		// ReSharper disable once NotAccessedField.Global
		public readonly Keys Modifiers;
		public readonly string Name;

		/// <summary>
		///    Information about the current hot key pressed.
		/// </summary>
		/// <param name="name">Hot key name.</param>
		/// <param name="key">Base key that was pressed when the event was fired.</param>
		/// <param name="modifiers">Modifiers pressed.</param>
		public KeyboardHookEventArgs(string name, Keys key, Keys modifiers)
		{
			Key = key;
			Modifiers = modifiers;
			Name = name;
		}
	}
}