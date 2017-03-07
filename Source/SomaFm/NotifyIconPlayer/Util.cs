using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SomaFm.NotifyIconPlayer
{
	internal static class Util
	{
		/// Resources
		/// <summary>
		///    Load an embedded resource as an icon.
		/// </summary>
		/// <param name="resource">Resource name, including namespace.</param>
		public static Icon ResourceAsIcon(string resource)
		{
			var assembly = Assembly.GetExecutingAssembly();
			using (var stream = assembly.GetManifestResourceStream(resource))
			{
				return new Icon(stream);
			}
		}

		/// NotifyIcon extensions
		/// <summary>
		///    Safely change the icon tooltip text.
		///    Strings longer than 63 characters are trimmed to 60 characters
		///    with a "..." suffix.
		/// </summary>
		/// <param name="notifyIcon">The notify icon</param>
		/// <param name="text">Tooltip text to display.</param>
		public static void SetToolTipText(this NotifyIcon notifyIcon, string text)
		{
			if (text.Length > 63)
			{
				notifyIcon.Text = text.Substring(0, 60) + "...";
			}
			else
			{
				notifyIcon.Text = text;
			}
		}
	}
}