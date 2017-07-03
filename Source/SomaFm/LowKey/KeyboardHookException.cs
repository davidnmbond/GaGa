using System;

namespace SomaFm.LowKey
{
	/// <summary>
	///    All the exceptions that KeyboardHook raises are of this type.
	/// </summary>
	public class KeyboardHookException : Exception
	{
		public KeyboardHookException(string message) : base(message)
		{
		}
	}
}