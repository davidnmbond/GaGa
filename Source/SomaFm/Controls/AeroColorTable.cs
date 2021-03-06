using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SomaFm.Controls
{
	internal class AeroColorTable : ProfessionalColorTable
	{
		private Color _lastAeroColor;
		private Color _menuItemBorder;
		private Color _menuItemSelected;

		/// <summary>
		///    A color table that tries to match the current aero theme
		///    when rendering selected menu items.
		/// </summary>
		public AeroColorTable()
		{
			_lastAeroColor = Color.Empty;
			_menuItemSelected = base.MenuItemSelected;
			_menuItemBorder = base.MenuItemBorder;
		}

		/// <summary>
		///    Gets the solid color to use when a ToolStripMenuItem is selected.
		/// </summary>
		public override Color MenuItemSelected => _menuItemSelected;

		/// <summary>
		///    Gets the border color to use with ToolStripMenuItem.
		/// </summary>
		public override Color MenuItemBorder => _menuItemBorder;

		/// <summary>
		///    Update the colors to match the current aero theme.
		/// </summary>
		public void UpdateColors()
		{
			Color ret;
			try
			{
				var value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorizationColor", null);

				ret = value == null ? Color.Empty : Color.FromArgb((int) value);
			}
			// unable to read registry or invalid value:
			catch (Exception)
			{
				ret = Color.Empty;
			}
			var aeroColor = ret;

			// unable to read it, fallback to default colors:
			if (aeroColor == Color.Empty)
			{
				_menuItemSelected = base.MenuItemSelected;
				_menuItemBorder = base.MenuItemBorder;
				return;
			}
			// recalculate when needed:
			if (aeroColor == _lastAeroColor) return;
			_lastAeroColor = aeroColor;
			RecalculateColors(aeroColor);
		}

		/// <summary>
		///    Recalculate our color values from a given base color.
		/// </summary>
		private void RecalculateColors(Color baseColor)
		{
			var a = (double) baseColor.A;
			var r = (double) baseColor.R;
			var g = (double) baseColor.G;
			var b = (double) baseColor.B;

			// too low alpha for clear visibility, darken it:
			if (a < 30)
			{
				a = 30;
			}

			// we want an opaque color, so remove alpha
			// but keep the current color value:
			// c = c * (alpha / 255) + (255 * (1 - (alpha / 255)))
			r = r * (a / 255) + (255 * (1 - (a / 255)));
			g = g * (a / 255) + (255 * (1 - (a / 255)));
			b = b * (a / 255) + (255 * (1 - (a / 255)));

			// we don't want colors too close to white
			// those would be indistinguishable from the background:
			if ((r > 220) && (g > 220) && (b > 220))
			{
				r = g = b = 220;
			}

			// we don't want color too close to black
			// those would obscure the text:
			if ((r < 50) && (g < 50) && (b < 50))
			{
				r = g = b = 50;
			}

			r = Clamp(r, 0, 255);
			g = Clamp(g, 0, 255);
			b = Clamp(b, 0, 255);

			var color = Color.FromArgb((int) r, (int) g, (int) b);

			// right now, both the selected menu and the border use
			// the same color but we could also darken the border a bit:
			_menuItemSelected = color;
			_menuItemBorder = color;
		}
		/// Math
		/// <summary>
		///    Clamp a value inclusively between min and max.
		/// </summary>
		/// <param name="value">Input value.</param>
		/// <param name="min">Minimum value.</param>
		/// <param name="max">Maximum value.</param>
		private static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
		{
			if (value.CompareTo(min) < 0)
			{
				return min;
			}

			if (value.CompareTo(max) > 0)
			{
				return max;
			}

			return value;
		}
	}
}