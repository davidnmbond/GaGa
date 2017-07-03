using System;
using System.Windows.Forms;

namespace SomaFm
{
	internal static class Program
	{
		private static SomaFm _somaFm;

		/// <summary>
		///    The main entry point for the application.
		/// </summary>
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
			_somaFm = new SomaFm();
			Application.Run(_somaFm);
		}

		/// <summary>
		/// Permits clean exit/disposal when the thread is being killed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnProcessExit(object sender, EventArgs e)
		{
			_somaFm.Dispose();
		}
	}
}