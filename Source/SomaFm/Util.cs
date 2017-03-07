using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace SomaFm
{
	internal static class Util
	{
		/// OS information
		/// <summary>
		///    Get the path for the directory that contains
		///    the current application executable.
		/// </summary>
		public static string ApplicationFolder => Path.GetDirectoryName(Application.ExecutablePath);

		/// <summary>
		///    Get the current mouse position.
		/// </summary>
		public static Point MousePosition
		{
			get
			{
				POINT pt;
				GetCursorPos(out pt);
				return new Point(pt.X, pt.Y);
			}
		}

		/// IO
		/// <summary>
		///    Serialize an object to a binary file.
		/// </summary>
		/// <param name="value">object to serialize.</param>
		/// <param name="filepath">Destination path.</param>
		public static void Serialize(object value, string filepath)
		{
			var formatter = new BinaryFormatter();
			using (var fs = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write))
			{
				formatter.Serialize(fs, value);
			}
		}

		/// <summary>
		///    Deserialize an object from a binary file.
		/// </summary>
		/// <param name="filepath">File path.</param>
		public static object Deserialize(string filepath)
		{
			var formatter = new BinaryFormatter();
			using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
			{
				return formatter.Deserialize(fs);
			}
		}

		/// MessageBoxes
		/// <summary>
		///    Show a MessageBox with Yes and No buttons.
		///    Return true when Yes is clicked, false otherwise.
		/// </summary>
		/// <param name="text">MessageBox text.</param>
		/// <param name="caption">MessageBox caption.</param>
		public static bool MessageBoxYesNo(string text, string caption)
		{
			return MessageBox.Show(text, caption, MessageBoxButtons.YesNo) == DialogResult.Yes;
		}

		/// NotifyIcon extensions
		/// <summary>
		///    Show the context menu for the icon at the given location.
		/// </summary>
		public static void ShowContextMenuStrip(this NotifyIcon notifyIcon, Point position)
		{
			var menu = notifyIcon.ContextMenuStrip;

			// bail out if there is no menu:
			if (menu == null)
				return;

			// we must make it a foreground window
			// otherwise, an icon is shown in the taskbar:
			SetForegroundWindow(new HandleRef(menu, menu.Handle));

			// ContextMenuStrip.Show(x, y) doesn't overlap the taskbar
			// we need "ShowInTaskbar" via reflection:
			var mi = typeof(ContextMenuStrip).GetMethod("ShowInTaskbar",
				BindingFlags.Instance | BindingFlags.NonPublic);

			mi.Invoke(menu, new object[] {position.X, position.Y});
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCursorPos(out POINT pt);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetForegroundWindow(HandleRef hWnd);

		/// Private Windows API declarations
		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			public readonly int X;
			public readonly int Y;

			public POINT(int x, int y)
			{
				X = x;
				Y = y;
			}
		}
	}
}