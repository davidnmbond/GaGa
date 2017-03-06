using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SomaFm.Controls
{
	[DesignerCategory("")]
	internal class ToolStripLabeledTrackBar : ToolStripControlHost
	{
		public readonly Label Label;
		public readonly TrackBar TrackBar;

		public ToolStripLabeledTrackBar() : base(new Panel())
		{
			var panel = (Panel) Control;

			Label = new Label
			{
				Location = new Point(0, 0)
			};

			TrackBar = new TrackBar
			{
				Location = new Point(0, Label.Bottom),
				AutoSize = false,
				TickStyle = TickStyle.None
			};


			// no tickstyle, make the height smaller and the width
			// a bit larger to compensate:
			TrackBar.Height = (int) (TrackBar.PreferredSize.Height * 0.65);
			TrackBar.Width = (int) (TrackBar.PreferredSize.Width * 1.25);

			// the label and panel follow the trackbar width:
			Label.Width = TrackBar.Width;
			Width = TrackBar.Width;

			panel.Controls.Add(Label);
			panel.Controls.Add(TrackBar);
		}
	}
}