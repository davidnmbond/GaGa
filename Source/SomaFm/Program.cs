using System;
using System.IO;
using System.Windows.Forms;

namespace SomaFm
{
	internal class Program
	{
		/// <summary>
		///    The main entry point for the application.
		/// </summary>
		[STAThread]
		private static void Main()
		{
			// Default path for the settings and the streams file:
			var currentFolder = Util.ApplicationFolder;
			var settingsFilepath = Path.Combine(currentFolder, "SomaFm.dat");
			var streamsFilepath = Path.Combine(currentFolder, "Streams.ini");

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new SomaFm(settingsFilepath, streamsFilepath));
		}
	}
}