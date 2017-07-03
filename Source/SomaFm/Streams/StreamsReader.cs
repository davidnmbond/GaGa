using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using SomaFm.Properties;

namespace SomaFm.Streams
{
	internal static class StreamsReader
	{
		/// <summary>
		///    Read a streams file adding sub-menus and items to a context menu.
		/// </summary>
		/// <param name="menu">Target context menu.</param>
		/// <param name="onClick">Click event to attach to menu items.</param>
		public static void Read(ContextMenuStrip menu, EventHandler onClick)
		{
			// start at the menu root:
			var currentMenuItemCollection = menu.Items;
			var seenSubmenues = new Dictionary<string, ToolStripMenuItem>();

			foreach (var jsonLine in Settings.Default.Streams)
			{
				var streamInfo = JsonConvert.DeserializeObject<StreamInfo>(jsonLine);
				seenSubmenues.TryGetValue(streamInfo.Group, out ToolStripMenuItem submenu);

				// not seen, create and add as a sub-menu to the current menu
				// otherwise it's a duplicate and has already been added:
				if (submenu == null)
				{
					submenu = new ToolStripMenuItem(streamInfo.Group);
					seenSubmenues.Add(streamInfo.Group, submenu);
				}

				submenu.DropDownItems.Add(new ToolStripMenuItem(streamInfo.Name, null, onClick) { Tag = new Uri(streamInfo.Url) });
			}

			// ReSharper disable once CoVariantArrayConversion
			currentMenuItemCollection.AddRange(seenSubmenues.Values.ToArray());
		}
	}
}