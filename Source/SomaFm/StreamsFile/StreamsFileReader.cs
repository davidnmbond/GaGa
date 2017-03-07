using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using mINI;

namespace SomaFm.StreamsFile
{
	internal class StreamsFileReader : INIReader
	{
		private readonly List<ToolStripMenuItem> _currentMenuItems;
		private readonly Dictionary<string, ToolStripMenuItem> _seenSubmenues;
		private string _currentLine;

		private int _currentLineNumber;
		private ToolStripItemCollection _currentMenuItemCollection;
		private string _filePath;
		private ContextMenuStrip _menu;
		private EventHandler _onClick;

		/// <summary>
		///    An INIReader that reads lines from a streams file
		///    adding sections and key=value pairs to a context menu
		///    as submenus and clickable items.
		/// </summary>
		public StreamsFileReader()
		{
			_filePath = null;
			_menu = null;
			_onClick = null;

			_currentMenuItems = new List<ToolStripMenuItem>();
			_currentMenuItemCollection = null;
			_seenSubmenues = new Dictionary<string, ToolStripMenuItem>();

			_currentLineNumber = 0;
			_currentLine = string.Empty;
		}

		/// <summary>
		///    Clear internal state.
		/// </summary>
		private void ResetState()
		{
			_filePath = null;
			_menu = null;
			_onClick = null;

			_currentMenuItems.Clear();
			_currentMenuItemCollection = null;
			_seenSubmenues.Clear();

			_currentLineNumber = 0;
			_currentLine = string.Empty;
		}

		/// <summary>
		///    Add the collected items to the current menu.
		/// </summary>
		private void AddCurrentMenuItems()
		{
			_currentMenuItemCollection.AddRange(_currentMenuItems.ToArray());
			_currentMenuItems.Clear();
		}

		/// <summary>
		///    Concise helper to create StreamsFileReadError exceptions.
		/// </summary>
		/// <param name="message">Error message.</param>
		private StreamsFileReadError ReadError(string message)
		{
			return new StreamsFileReadError(
				message,
				_filePath,
				_currentLine,
				_currentLineNumber
			);
		}

		/// <summary>
		///    Do not accept menus (sections) with no name.
		/// </summary>
		protected override void OnSectionEmpty()
		{
			throw ReadError("Empty menu name.");
		}

		/// <summary>
		///    Do not accept submenus (subsections) with no name.
		/// </summary>
		protected override void OnSubSectionEmpty(string path)
		{
			throw ReadError("Empty submenu name.");
		}

		/// <summary>
		///    Do not accept streams with no name.
		/// </summary>
		protected override void OnKeyEmpty(string value)
		{
			throw ReadError("Empty stream name.");
		}

		/// <summary>
		///    Do not accept streams with no URI.
		/// </summary>
		protected override void OnValueEmpty(string key)
		{
			throw ReadError("Empty stream URI.");
		}

		/// <summary>
		///    Syntax errors.
		/// </summary>
		protected override void OnUnknown(string line)
		{
			throw ReadError("Invalid syntax.");
		}

		/// <summary>
		///    On an empty line, add collected items
		///    and go back to the menu root.
		/// </summary>
		protected override void OnEmpty()
		{
			AddCurrentMenuItems();
			_currentMenuItemCollection = _menu.Items;
		}

		/// <summary>
		///    On a new section, add collected items
		///    and go back to the menu root.
		/// </summary>
		protected override void OnSection(string section)
		{
			AddCurrentMenuItems();
			_currentMenuItemCollection = _menu.Items;
		}

		/// <summary>
		///    On a subsection, add collected items
		///    create a submenu and descend into it.
		/// </summary>
		protected override void OnSubSection(string subsection, string path)
		{
			ToolStripMenuItem submenu;
			_seenSubmenues.TryGetValue(path, out submenu);

			// not seen, create and add as a submenu to the current menu
			// otherwise it's a duplicate and has already been added:
			if (submenu == null)
			{
				submenu = new ToolStripMenuItem(subsection);
				_seenSubmenues.Add(path, submenu);
				_currentMenuItems.Add(submenu);
			}

			AddCurrentMenuItems();
			_currentMenuItemCollection = submenu.DropDownItems;
		}

		/// <summary>
		///    Add key=value pairs as clickable menu items.
		///    The URI is stored in the item .Tag property.
		/// </summary>
		protected override void OnKeyValue(string key, string value)
		{
			var item = new ToolStripMenuItem(key, null, _onClick);

			try
			{
				item.Tag = new Uri(value);
			}
			catch (UriFormatException exception)
			{
				throw ReadError(exception.Message);
			}

			_currentMenuItems.Add(item);
		}

		/// <summary>
		///    Read a streams file adding submenus and items to a context menu.
		/// </summary>
		/// <param name="filepath">Path to the streams file to read lines from.</param>
		/// <param name="menu">Target context menu.</param>
		/// <param name="onClick">Click event to attach to menu items.</param>
		public void Read(string filepath, ContextMenuStrip menu, EventHandler onClick)
		{
			_filePath = filepath;
			_menu = menu;
			_onClick = onClick;

			try
			{
				// start at the menu root:
				_currentMenuItemCollection = menu.Items;

				foreach (var line in File.ReadLines(filepath))
				{
					_currentLineNumber++;
					_currentLine = line;
					ReadLine(line);
				}

				// add pending items for the last submenu:
				AddCurrentMenuItems();
			}
			finally
			{
				ResetState();
			}
		}
	}
}