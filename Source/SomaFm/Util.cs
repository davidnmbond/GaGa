using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SomaFm
{
	internal static partial class Util
	{
		/// <summary>
		///    Get the current mouse position.
		/// </summary>
		public static System.Drawing.Point MousePosition
		{
			get
			{
				GetCursorPos(out Point pt);
				return new System.Drawing.Point(pt.X, pt.Y);
			}
		}
		/// NotifyIcon extensions
		/// <summary>
		///    Show the context menu for the icon at the given location.
		/// </summary>
		public static void ShowContextMenuStrip(this NotifyIcon notifyIcon, System.Drawing.Point position)
		{
			var menu = notifyIcon.ContextMenuStrip;

			// bail out if there is no menu:
			if (menu == null)
				return;

			// we must make it a foreground window
			// otherwise, an icon is shown in the task bar:
			SetForegroundWindow(new HandleRef(menu, menu.Handle));

			// ContextMenuStrip.Show(x, y) doesn't overlap the task bar
			// we need "ShowInTaskbar" via reflection:
			var mi = typeof(ContextMenuStrip).GetMethod("ShowInTaskbar",
				BindingFlags.Instance | BindingFlags.NonPublic);

			mi.Invoke(menu, new object[] {position.X, position.Y});
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCursorPos(out Point pt);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetForegroundWindow(HandleRef hWnd);
	}
}