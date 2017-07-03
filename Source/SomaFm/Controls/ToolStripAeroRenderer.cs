using System.Windows.Forms;

namespace SomaFm.Controls
{
	internal class ToolStripAeroRenderer : ToolStripProfessionalRenderer
	{
		/// <summary>
		///    A renderer that tries to match the current aero theme
		///    when drawing selected menu items.
		/// </summary>
		public ToolStripAeroRenderer() : base(new AeroColorTable())
		{
		}

		/// <summary>
		///    Update the colors to match the current aero theme.
		/// </summary>
		public void UpdateColors()
		{
			((AeroColorTable) ColorTable).UpdateColors();
		}
	}
}