using System;
using System.Windows.Forms;

namespace SomaFm.Libraries.LowKey
{
	/// <summary>
	///    Gives information about a hotkey when an event is fired.
	/// </summary>
	public class KeyboardHookEventArgs : EventArgs
	{
		public readonly Keys Key;
		public readonly Keys Modifiers;
		public readonly string Name;

		/// <summary>
		///    Information about the current hotkey pressed.
		/// </summary>
		/// <param name="name">
		///    Hotkey name.
		/// </param>
		/// <param name="key">
		///    Base key that was pressed when the event was fired.
		/// </param>
		/// <param name="modifiers">
		///    Modifiers pressed.
		/// </param>
		public KeyboardHookEventArgs(string name, Keys key, Keys modifiers)
		{
			Key = key;
			Modifiers = modifiers;
			Name = name;
		}
	}
}