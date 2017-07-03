using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SomaFm.LowKey
{
	/// <summary>
	///    The LowKey keyboard hooker.
	/// </summary>
	public class KeyboardHook : IDisposable
	{
		/// Private Windows API declarations
		private const int VkShift = 0x10;

		private const int VkControl = 0x11;
		private const int VkMenu = 0x12;

		private const int WhKeyboardLl = 13;

		private const int WmSyskeydown = 0x0104;
		private const int WmSyskeyup = 0x0105;
		private const int WmKeydown = 0x0100;
		private const int WmKeyup = 0x0101;

		/// <summary>
		///    Needed to avoid the delegate being garbage-collected.
		/// </summary>
		private static Hookproc _hookedCallback;

		/// <summary>
		///    Hooker instance.
		/// </summary>
		private static KeyboardHook _instance;

		/// <summary>
		///    Current dispatcher.
		/// </summary>
		private readonly Dispatcher _dispatcher;

		/// <summary>
		///    Virtual key code -> set of modifiers for all the hot keys.
		/// </summary>
		private readonly Dictionary<int, HashSet<Keys>> _hotKeys;

		/// <summary>
		///    A map from hot keys to a boolean indicating whether
		///    we should forward the key press to further applications.
		/// </summary>
		private readonly Dictionary<Hotkey, bool> _hotKeysForward;

		/// <summary>
		///    A map from hot keys to names.
		/// </summary>
		private readonly Dictionary<Hotkey, string> _hotKeysToNames;

		/// <summary>
		///    A map from names to hot keys.
		/// </summary>
		private readonly Dictionary<string, Hotkey> _namesToHotKeys;

		/// <summary>
		///    Hook ID.
		///    Will be IntPtr.Zero when not currently hooked.
		/// </summary>
		private IntPtr _hookId;

		/// <summary>
		///    Create a new keyboard hooker instance.
		/// </summary>
		private KeyboardHook()
		{
			_hotKeys = new Dictionary<int, HashSet<Keys>>();

			_hotKeysToNames = new Dictionary<Hotkey, string>();
			_namesToHotKeys = new Dictionary<string, Hotkey>();
			_hotKeysForward = new Dictionary<Hotkey, bool>();

			_dispatcher = Dispatcher.CurrentDispatcher;
			_hookId = IntPtr.Zero;
			_hookedCallback = Callback;
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

				if ((GetAsyncKeyState(VkMenu) & 0x8000) != 0)
					modifiers |= Keys.Alt;

				if ((GetAsyncKeyState(VkControl) & 0x8000) != 0)
					modifiers |= Keys.Control;

				if ((GetAsyncKeyState(VkShift) & 0x8000) != 0)
					modifiers |= Keys.Shift;

				return modifiers;
			}
		}

		/// Public interface
		/// <summary>
		///    Get the hooker instance.
		/// </summary>
		public static KeyboardHook Hooker => _instance ?? (_instance = new KeyboardHook());

		/// <summary>
		///    Determine whether the hook is currently active.
		/// </summary>
		public bool IsHooked => _hookId != IntPtr.Zero;

		/// <summary>
		///    Dispose the hooker.
		/// </summary>
		public void Dispose()
		{
			if (_hookId != IntPtr.Zero)
			{
				Unhook();
			}

			_instance = null;
		}

		/// Events
		/// <summary>
		///    Fired when a registered hot key is released.
		/// </summary>
		// ReSharper disable once EventNeverSubscribedTo.Global
		public event EventHandler<KeyboardHookEventArgs> HotkeyUp;

		/// <summary>
		///    Fired when a registered hot key is pressed.
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
		///    Add the specified hot key to the hooker.
		/// </summary>
		/// <param name="name">
		///    Hot key name.
		/// </param>
		/// <param name="key">
		///    Base key.
		/// </param>
		/// <param name="modifiers">
		///    A bitwise combination of additional modifiers
		///    e.g: Keys.Control | Keys.Alt.
		/// </param>
		/// <param name="forward">
		///    Whether the key press should be forwarded to
		///    other applications.
		/// </param>
		public void Add(string name, Keys key, Keys modifiers = Keys.None, bool forward = false)
		{
			// check name:
			if (name == null)
				throw new KeyboardHookException("Invalid hot key name.");

			if (_namesToHotKeys.ContainsKey(name))
				throw new KeyboardHookException($"Duplicate hot key name: {name}.");

			// check key code and modifiers:
			var vkCode = (int) key;

			// known base key:
			if (_hotKeys.ContainsKey(vkCode))
			{
				// check that modifiers are new:
				var currentModifiers = _hotKeys[vkCode];
				if (currentModifiers.Contains(modifiers))
				{
					var previousHotkey = new Hotkey(key, modifiers);
					throw new KeyboardHookException(
						$"Hot key: {name} already registered as: {_hotKeysToNames[previousHotkey]}."
					);
				}

				currentModifiers.Add(modifiers);
			}
			// new base key:
			else
			{
				_hotKeys[vkCode] = new HashSet<Keys> {modifiers};
			}

			// add it to the lookup dictionaries:
			var hotkey = new Hotkey(key, modifiers);

			_hotKeysToNames[hotkey] = name;
			_namesToHotKeys[name] = hotkey;
			_hotKeysForward[hotkey] = forward;
		}

		/// <summary>
		///    Remove the specified hot key.
		/// </summary>
		/// <param name="name">
		///    Hot key name that was specified when calling Add().
		/// </param>
		private void Remove(string name)
		{
			// check the name:
			if (name == null)
				throw new KeyboardHookException("Invalid hot key name.");

			if (!_namesToHotKeys.ContainsKey(name))
				throw new KeyboardHookException($"Unknown hot key name: {name}.");

			var hotkey = _namesToHotKeys[name];

			// remove from all dictionaries:
			var vkCode = (int) hotkey.Key;
			var modifiers = hotkey.Modifiers;

			_hotKeys[vkCode].Remove(modifiers);
			_hotKeysToNames.Remove(hotkey);
			_namesToHotKeys.Remove(name);
			_hotKeysForward.Remove(hotkey);
		}

		/// <summary>
		///    Remove all the registered hot keys.
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public void Clear()
		{
			_hotKeys.Clear();
			_hotKeysToNames.Clear();
			_namesToHotKeys.Clear();
			_hotKeysForward.Clear();
		}

		/// <summary>
		///    Modify a hot key binding.
		/// </summary>
		/// <param name="name">
		///    Hot key name that was specified when calling Add().
		/// </param>
		/// <param name="key">
		///    New base key.
		/// </param>
		/// <param name="modifiers">
		///    New modifiers.
		/// </param>
		/// <param name="forward">
		///    Whether the key press should be forwarded to
		///    other applications.
		/// </param>
		// ReSharper disable once UnusedMember.Global
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
			if (_hookId != IntPtr.Zero)
			{
				throw new KeyboardHookException("Keyboard hook already active. Call Unhook() first.");
			}

			using (var process = Process.GetCurrentProcess())
			{
				using (var module = process.MainModule)
				{
					var hMod = GetModuleHandle(module.ModuleName);
					_hookId = SetWindowsHookEx(WhKeyboardLl, _hookedCallback, hMod, 0);

					// when SetWindowsHookEx fails, the result is NULL:
					if (_hookId == IntPtr.Zero)
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
			if (_hookId == IntPtr.Zero)
			{
				throw new KeyboardHookException("Keyboard hook not currently active. Call Hook() first.");
			}

			// when UnhookWindowsHookEx fails, the result is false:
			if (!UnhookWindowsHookEx(_hookId))
			{
				throw new KeyboardHookException("UnhookWindowsHookEx() failed: " + LastWin32Error());
			}

			_hookId = IntPtr.Zero;
		}

		/// Actual hooker callback
		/// <summary>
		///    Callback that intercepts key presses.
		/// </summary>
		private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			// assume the hot key won't match and will be forwarded:
			var forward = true;

			if (nCode >= 0)
			{
				var msg = wParam.ToInt32();

				// we care about key up / key down messages:
				if (msg == WmKeyup || msg == WmSyskeyup || msg == WmKeydown || msg == WmSyskeydown)
				{
					// the virtual key code is the first KBDLLHOOKSTRUCT member:
					var vkCode = Marshal.ReadInt32(lParam);

					// base key matches?
					if (_hotKeys.ContainsKey(vkCode))
					{
						var modifiers = PressedModifiers;

						// modifiers match?
						if (_hotKeys[vkCode].Contains(modifiers))
						{
							var key = (Keys) vkCode;
							var hotkey = new Hotkey(key, modifiers);
							var name = _hotKeysToNames[hotkey];

							// override forward with the current hot key option:
							forward = _hotKeysForward[hotkey];

							var e = new KeyboardHookEventArgs(name, key, modifiers);

							// call the appropriate event handler using the current dispatcher:
							if (msg == WmKeyup || msg == WmSyskeyup)
							{
								if (HotkeyUp != null)
								{
									_dispatcher.BeginInvoke(HotkeyUp, _instance, e);
								}
							}
							else
							{
								if (HotkeyDown != null)
								{
									_dispatcher.BeginInvoke(HotkeyDown, _instance, e);
								}
							}
						}
					}
				}
			}

			// forward or return a dummy value other than 0:
			if (forward)
			{
				return CallNextHookEx(_hookId, nCode, wParam, lParam);
			}
			return new IntPtr(1);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, Hookproc lpfn, IntPtr hMod, uint dwThreadId);

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

		private delegate IntPtr Hookproc(int nCode, IntPtr wParam, IntPtr lParam);
	}
}