using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SomaFm.Libraries.LowKey
{
	/// <summary>
	///    The LowKey keyboard hooker.
	/// </summary>
	public class KeyboardHook : IDisposable
	{
		/// Private Windows API declarations
		private const int VK_SHIFT = 0x10;

		private const int VK_CONTROL = 0x11;
		private const int VK_MENU = 0x12;

		private const int WH_KEYBOARD_LL = 13;

		private const int WM_SYSKEYDOWN = 0x0104;
		private const int WM_SYSKEYUP = 0x0105;
		private const int WM_KEYDOWN = 0x0100;
		private const int WM_KEYUP = 0x0101;

		/// <summary>
		///    Needed to avoid the delegate being garbage-collected.
		/// </summary>
		private static HOOKPROC hookedCallback;

		/// <summary>
		///    Hooker instance.
		/// </summary>
		private static KeyboardHook instance;

		/// <summary>
		///    Current dispatcher.
		/// </summary>
		private readonly Dispatcher dispatcher;

		/// <summary>
		///    Virtual key code -> set of modifiers for all the hotkeys.
		/// </summary>
		private readonly Dictionary<int, HashSet<Keys>> hotkeys;

		/// <summary>
		///    A map from hotkeys to a boolean indicating whether
		///    we should forward the keypress to further applications.
		/// </summary>
		private readonly Dictionary<Hotkey, bool> hotkeysForward;

		/// <summary>
		///    A map from hotkeys to names.
		/// </summary>
		private readonly Dictionary<Hotkey, string> hotkeysToNames;

		/// <summary>
		///    A map from names to hotkeys.
		/// </summary>
		private readonly Dictionary<string, Hotkey> namesToHotkeys;

		/// <summary>
		///    Hook ID.
		///    Will be IntPtr.Zero when not currently hooked.
		/// </summary>
		private IntPtr hookID;

		/// <summary>
		///    Create a new keyboard hooker instance.
		/// </summary>
		private KeyboardHook()
		{
			hotkeys = new Dictionary<int, HashSet<Keys>>();

			hotkeysToNames = new Dictionary<Hotkey, string>();
			namesToHotkeys = new Dictionary<string, Hotkey>();
			hotkeysForward = new Dictionary<Hotkey, bool>();

			dispatcher = Dispatcher.CurrentDispatcher;
			hookID = IntPtr.Zero;
			hookedCallback = Callback;
		}

		/// <summary>
		///    Determine which modifiers (Keys.Alt, Keys.Control, Keys.Shift)
		///    are currently pressed.
		/// </summary>
		private static Keys PressedModifiers
		{
			get
			{
				var modifiers = Keys.None;

				if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
					modifiers |= Keys.Alt;

				if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
					modifiers |= Keys.Control;

				if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
					modifiers |= Keys.Shift;

				return modifiers;
			}
		}

		/// Public interface
		/// <summary>
		///    Get the hooker instance.
		/// </summary>
		public static KeyboardHook Hooker => instance ?? (instance = new KeyboardHook());

		/// <summary>
		///    Determine whether the hook is currently active.
		/// </summary>
		public bool IsHooked => hookID != IntPtr.Zero;

		/// <summary>
		///    Dispose the hooker.
		/// </summary>
		public void Dispose()
		{
			if (hookID != IntPtr.Zero)
			{
				Unhook();
			}

			instance = null;
		}

		/// Events
		/// <summary>
		///    Fired when a registered hotkey is released.
		/// </summary>
		public event EventHandler<KeyboardHookEventArgs> HotkeyUp;

		/// <summary>
		///    Fired when a registered hotkey is pressed.
		/// </summary>
		public event EventHandler<KeyboardHookEventArgs> HotkeyDown;

		/// Helpers
		/// <summary>
		///    Retrieve the last Windows error as a readable string.
		/// </summary>
		private static string LastWin32Error()
		{
			return new Win32Exception(Marshal.GetLastWin32Error()).Message;
		}

		/// <summary>
		///    Add the specified hotkey to the hooker.
		/// </summary>
		/// <param name="name">
		///    Hotkey name.
		/// </param>
		/// <param name="key">
		///    Base key.
		/// </param>
		/// <param name="modifiers">
		///    A bitwise combination of additional modifiers
		///    e.g: Keys.Control | Keys.Alt.
		/// </param>
		/// <param name="forward">
		///    Whether the keypress should be forwarded to
		///    other applications.
		/// </param>
		public void Add(string name, Keys key, Keys modifiers = Keys.None, bool forward = false)
		{
			// check name:
			if (name == null)
				throw new KeyboardHookException("Invalid hotkey name.");

			if (namesToHotkeys.ContainsKey(name))
				throw new KeyboardHookException($"Duplicate hotkey name: {name}.");

			// check key code and modifiers:
			var vkCode = (int) key;

			// known base key:
			if (hotkeys.ContainsKey(vkCode))
			{
				// check that modifiers are new:
				var currentModifiers = hotkeys[vkCode];
				if (currentModifiers.Contains(modifiers))
				{
					var previousHotkey = new Hotkey(key, modifiers);
					throw new KeyboardHookException(
						$"Hotkey: {name} already registered as: {hotkeysToNames[previousHotkey]}."
					);
				}

				currentModifiers.Add(modifiers);
			}
			// new base key:
			else
			{
				hotkeys[vkCode] = new HashSet<Keys> {modifiers};
			}

			// add it to the lookup dicts:
			var hotkey = new Hotkey(key, modifiers);

			hotkeysToNames[hotkey] = name;
			namesToHotkeys[name] = hotkey;
			hotkeysForward[hotkey] = forward;
		}

		/// <summary>
		///    Remove the specified hotkey.
		/// </summary>
		/// <param name="name">
		///    Hotkey name that was specified when calling Add().
		/// </param>
		public void Remove(string name)
		{
			// check the name:
			if (name == null)
				throw new KeyboardHookException("Invalid hotkey name.");

			if (!namesToHotkeys.ContainsKey(name))
				throw new KeyboardHookException($"Unknown hotkey name: {name}.");

			var hotkey = namesToHotkeys[name];

			// remove from all dicts:
			var vkCode = (int) hotkey.Key;
			var modifiers = hotkey.Modifiers;

			hotkeys[vkCode].Remove(modifiers);
			hotkeysToNames.Remove(hotkey);
			namesToHotkeys.Remove(name);
			hotkeysForward.Remove(hotkey);
		}

		/// <summary>
		///    Remove all the registered hotkeys.
		/// </summary>
		public void Clear()
		{
			hotkeys.Clear();
			hotkeysToNames.Clear();
			namesToHotkeys.Clear();
			hotkeysForward.Clear();
		}

		/// <summary>
		///    Modify a hotkey binding.
		/// </summary>
		/// <param name="name">
		///    Hotkey name that was specified when calling Add().
		/// </param>
		/// <param name="key">
		///    New base key.
		/// </param>
		/// <param name="modifiers">
		///    New modifiers.
		/// </param>
		/// <param name="forward">
		///    Whether the keypress should be forwarded to
		///    other applications.
		/// </param>
		public void Rebind(string name, Keys key, Keys modifiers = Keys.None, bool forward = false)
		{
			Remove(name);
			Add(name, key, modifiers, forward);
		}

		/// <summary>
		///    Start looking for key presses.
		/// </summary>
		public void Hook()
		{
			// don't hook twice:
			if (hookID != IntPtr.Zero)
			{
				throw new KeyboardHookException("Keyboard hook already active. Call Unhook() first.");
			}

			using (var process = Process.GetCurrentProcess())
			{
				using (var module = process.MainModule)
				{
					var hMod = GetModuleHandle(module.ModuleName);
					hookID = SetWindowsHookEx(WH_KEYBOARD_LL, hookedCallback, hMod, 0);

					// when SetWindowsHookEx fails, the result is NULL:
					if (hookID == IntPtr.Zero)
					{
						throw new KeyboardHookException("SetWindowsHookEx() failed: " + LastWin32Error());
					}
				}
			}
		}

		/// <summary>
		///    Stop looking for key presses.
		/// </summary>
		public void Unhook()
		{
			// not hooked:
			if (hookID == IntPtr.Zero)
			{
				throw new KeyboardHookException("Keyboard hook not currently active. Call Hook() first.");
			}

			// when UnhookWindowsHookEx fails, the result is false:
			if (!UnhookWindowsHookEx(hookID))
			{
				throw new KeyboardHookException("UnhookWindowsHookEx() failed: " + LastWin32Error());
			}

			hookID = IntPtr.Zero;
		}

		/// Actual hooker callback
		/// <summary>
		///    Callback that intercepts key presses.
		/// </summary>
		private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			// assume the hotkey won't match and will be forwarded:
			var forward = true;

			if (nCode >= 0)
			{
				var msg = wParam.ToInt32();

				// we care about keyup/keydown messages:
				if (msg == WM_KEYUP || msg == WM_SYSKEYUP || msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
				{
					// the virtual key code is the first KBDLLHOOKSTRUCT member:
					var vkCode = Marshal.ReadInt32(lParam);

					// base key matches?
					if (hotkeys.ContainsKey(vkCode))
					{
						var modifiers = PressedModifiers;

						// modifiers match?
						if (hotkeys[vkCode].Contains(modifiers))
						{
							var key = (Keys) vkCode;
							var hotkey = new Hotkey(key, modifiers);
							var name = hotkeysToNames[hotkey];

							// override forward with the current hotkey option:
							forward = hotkeysForward[hotkey];

							var e = new KeyboardHookEventArgs(name, key, modifiers);

							// call the appropriate event handler using the current dispatcher:
							if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
							{
								if (HotkeyUp != null)
								{
									dispatcher.BeginInvoke(HotkeyUp, instance, e);
								}
							}
							else
							{
								if (HotkeyDown != null)
								{
									dispatcher.BeginInvoke(HotkeyDown, instance, e);
								}
							}
						}
					}
				}
			}

			// forward or return a dummy value other than 0:
			if (forward)
			{
				return CallNextHookEx(hookID, nCode, wParam, lParam);
			}
			return new IntPtr(1);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, HOOKPROC lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern short GetAsyncKeyState(int vKey);

		/// Private data
		private struct Hotkey
		{
			public readonly Keys Key;
			public readonly Keys Modifiers;

			/// <summary>
			///    Represents a combination of a base key and additional modifiers.
			/// </summary>
			/// <param name="key">
			///    Base key.
			/// </param>
			/// <param name="modifiers">
			///    A bitwise combination of additional modifiers
			///    e.g: Keys.Control | Keys.Alt.
			/// </param>
			public Hotkey(Keys key, Keys modifiers = Keys.None)
			{
				Key = key;
				Modifiers = modifiers;
			}
		}

		private delegate IntPtr HOOKPROC(int nCode, IntPtr wParam, IntPtr lParam);
	}
}