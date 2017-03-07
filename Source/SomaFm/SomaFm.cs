using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SomaFm.Controls;
using SomaFm.Libraries.LowKey;
using SomaFm.NotifyIconPlayer;
using SomaFm.StreamsFile;

namespace SomaFm
{
	internal class SomaFm : ApplicationContext
	{
		// audio submenu:
		private readonly ToolStripMenuItem _audioMenuItem;
		private readonly ToolStripLabeledTrackBar _balanceTrackBar;
		// gui components:
		private readonly Container _container;

		// constant menu items:
		private readonly ToolStripMenuItem _dynamicMenuMarker;
		private readonly ToolStripMenuItem _editItem;
		private readonly ToolStripMenuItem _errorOpenItem;
		private readonly ToolStripMenuItem _errorReadItem;
		private readonly ToolStripMenuItem _exitItem;
		private readonly NotifyIcon _notifyIcon;
		private readonly ToolStripMenuItem _optionsEnableAutoPlayItem;
		private readonly ToolStripMenuItem _optionsEnableMultimediaKeysItem;

		// options submenu:
		private readonly ToolStripMenuItem _optionsMenuItem;

		// player:
		private readonly Player _player;
		private readonly SomaFmSettings _settings;

		// settings:
		private readonly string _settingsFilepath;
		private readonly StreamsFileLoader _streamsFileLoader;

		// streams:
		private readonly string _streamsFilepath;
		private readonly ToolStripAeroRenderer _toolStripRenderer;
		private readonly ToolStripLabeledTrackBar _volumeTrackBar;

		/// <summary>
		///    SomaFm implementation.
		/// </summary>
		/// <param name="settingsFilepath">
		///    Path to the settings file to use.
		/// </param>
		/// <param name="streamsFilepath">
		///    Path to the streams file to use.
		/// </param>
		public SomaFm(string settingsFilepath, string streamsFilepath)
		{
			// gui components:
			_container = new Container();
			_toolStripRenderer = new ToolStripAeroRenderer();

			_notifyIcon = new NotifyIcon(_container)
			{
				ContextMenuStrip = new ContextMenuStrip {Renderer = _toolStripRenderer},
				Visible = true
			};

			// settings:
			_settingsFilepath = settingsFilepath;
			_settings = SettingsLoad();

			// streams:
			_streamsFilepath = streamsFilepath;
			_streamsFileLoader = new StreamsFileLoader(streamsFilepath);

			// player:
			_player = new Player(_notifyIcon);

			// constant menu items:
			_dynamicMenuMarker = new ToolStripMenuItem
			{
				Visible = false
			};

			_errorOpenItem = new ToolStripMenuItem
			{
				Text = "Error opening streams file (click for details)"
			};

			_errorReadItem = new ToolStripMenuItem
			{
				Text = "Error reading streams file (click for details)"
			};

			_editItem = new ToolStripMenuItem
			{
				Text = "&Edit streams file"
			};

			_exitItem = new ToolStripMenuItem
			{
				Text = "E&xit"
			};

			// audio submenu:
			_audioMenuItem = new ToolStripMenuItem
			{
				Text = "Audio"
			};

			_balanceTrackBar = new ToolStripLabeledTrackBar();
			_balanceTrackBar.Label.Text = "Balance";
			_balanceTrackBar.TrackBar.Minimum = -10;
			_balanceTrackBar.TrackBar.Maximum = 10;

			_volumeTrackBar = new ToolStripLabeledTrackBar();
			_volumeTrackBar.Label.Text = "Volume";
			_volumeTrackBar.TrackBar.Minimum = 0;
			_volumeTrackBar.TrackBar.Maximum = 20;

			// adjust the backcolor to the renderer:
			var back = _toolStripRenderer.ColorTable.ToolStripDropDownBackground;

			_balanceTrackBar.BackColor = back;
			_balanceTrackBar.Label.BackColor = back;
			_balanceTrackBar.TrackBar.BackColor = back;
			_volumeTrackBar.BackColor = back;
			_volumeTrackBar.Label.BackColor = back;
			_volumeTrackBar.TrackBar.BackColor = back;

			_audioMenuItem.DropDownItems.Add(_balanceTrackBar);
			_audioMenuItem.DropDownItems.Add(_volumeTrackBar);

			// options submenu:
			_optionsMenuItem = new ToolStripMenuItem
			{
				Text = "Options"
			};

			_optionsEnableAutoPlayItem = new ToolStripMenuItem
			{
				Text = "Enable auto play on startup"
			};

			_optionsEnableMultimediaKeysItem = new ToolStripMenuItem
			{
				Text = "Enable multimedia keys"
			};

			_optionsMenuItem.DropDownItems.Add(_optionsEnableAutoPlayItem);
			_optionsMenuItem.DropDownItems.Add(_optionsEnableMultimediaKeysItem);

			// add multimedia keys:
			KeyboardHook.Hooker.Add("Toggle Play", Keys.MediaPlayPause);
			KeyboardHook.Hooker.Add("Stop", Keys.MediaStop);
			KeyboardHook.Hooker.Add("Toggle Mute", Keys.VolumeMute);
			KeyboardHook.Hooker.Add("Volume Up", Keys.VolumeUp);
			KeyboardHook.Hooker.Add("Volume Down", Keys.VolumeDown);

			// apply settings before wiring events:
			_balanceTrackBar.TrackBar.Value = _settings.LastBalanceTrackBarValue;
			_volumeTrackBar.TrackBar.Value = _settings.LastVolumeTrackBarValue;

			BalanceUpdate();
			VolumeUpdate();

			_player.Select(_settings.LastPlayerStream);

			_optionsEnableAutoPlayItem.Checked = _settings.OptionsEnableAutoPlayChecked;
			_optionsEnableMultimediaKeysItem.Checked = _settings.OptionsEnableMultimediaKeysChecked;

			// wire events:
			_notifyIcon.ContextMenuStrip.Opening += OnMenuOpening;
			_notifyIcon.MouseClick += OnIconMouseClick;

			_errorOpenItem.Click += OnErrorOpenItemClick;
			_errorReadItem.Click += OnErrorReadItemClick;
			_editItem.Click += OnEditItemClick;
			_exitItem.Click += OnExitItemClick;

			_balanceTrackBar.TrackBar.ValueChanged += OnBalanceTrackBarChanged;
			_volumeTrackBar.TrackBar.ValueChanged += OnVolumeTrackBarChanged;

			_optionsEnableAutoPlayItem.CheckOnClick = true;
			_optionsEnableMultimediaKeysItem.Click += OnEnableMultimediaKeysItemClicked;

			KeyboardHook.Hooker.HotkeyDown += OnHotkeyDown;

			// handle options:
			if (_optionsEnableAutoPlayItem.Checked)
			{
				_player.Play();
			}

			if (_optionsEnableMultimediaKeysItem.Checked)
			{
				MultimediaKeysHook();
			}
		}

		/// Streams file
		/// <summary>
		///    Open the streams file with the default program
		///    associated to the extension.
		/// </summary>
		private void StreamsFileOpen()
		{
			try
			{
				Process.Start(_streamsFilepath);
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "Error opening streams file");
			}
		}

		/// Settings file
		/// <summary>
		///    Load the settings from our settings filepath if it exists.
		///    Return default settings otherwise.
		/// </summary>
		private SomaFmSettings SettingsLoad()
		{
			try
			{
				if (File.Exists(_settingsFilepath))
				{
					return (SomaFmSettings) Util.Deserialize(_settingsFilepath);
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(
					"Unable to load settings: \n" + $"{_settingsFilepath} \n\n" + "This usually means that the file is corrupt, empty \n" + "or incompatible with the current SomaFm version. \n\n" + "Exception message: \n" + $"{exception.Message} \n",
					"Error loading settings file");
			}

			// unable to load or doesn't exist, use defaults:
			return new SomaFmSettings();
		}

		/// <summary>
		///    Save the current settings.
		/// </summary>
		private void SettingsSave()
		{
			try
			{
				Util.Serialize(_settings, _settingsFilepath);
			}
			catch (Exception exception)
			{
				MessageBox.Show(
					"Unable to save settings: \n" + $"{_settingsFilepath} \n\n" + "Exception message: \n" + $"{exception.Message} \n",
					"Error saving settings file");
			}
		}

		/// Menu updating
		/// <summary>
		///    Clear the current menu items, disposing
		///    the dynamic items.
		/// </summary>
		private void MenuClear()
		{
			// collect dynamic items up to the marker:
			var disposable = new List<ToolStripItem>();

			foreach (ToolStripItem item in _notifyIcon.ContextMenuStrip.Items)
			{
				if (item == _dynamicMenuMarker)
				{
					break;
				}
				disposable.Add(item);
			}

			// dispose them:
			foreach (var item in disposable)
			{
				item.Dispose();
			}

			disposable.Clear();
			_notifyIcon.ContextMenuStrip.Items.Clear();

			// at this point, all the menu items are dead
			// perform GC to make memory usage as deterministic/predictable
			// as possible:
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		/// <summary>
		///    Reload the context menu.
		/// </summary>
		private void MenuUpdate()
		{
			var menu = _notifyIcon.ContextMenuStrip;

			try
			{
				MenuClear();
				_streamsFileLoader.LoadTo(menu, OnStreamItemClick);
				menu.Items.Add(_dynamicMenuMarker);

				_editItem.Enabled = true;
			}
			catch (StreamsFileReadError exception)
			{
				MenuClear();
				menu.Items.Add(_dynamicMenuMarker);
				menu.Items.Add(_errorReadItem);

				_errorReadItem.Tag = exception;
				_editItem.Enabled = true;
			}
			catch (Exception exception)
			{
				MenuClear();
				menu.Items.Add(_dynamicMenuMarker);
				menu.Items.Add(_errorOpenItem);

				_errorOpenItem.Tag = exception;
				_editItem.Enabled = false;
			}

			menu.Items.Add(_editItem);
			menu.Items.Add("-");
			menu.Items.Add(_audioMenuItem);
			menu.Items.Add(_optionsMenuItem);
			menu.Items.Add(_exitItem);
		}

		/// <summary>
		///    When the menu is about to be opened, reload from the streams file
		///    and update the renderer colors when needed.
		/// </summary>
		private void OnMenuOpening(object sender, CancelEventArgs e)
		{
			// get the mouse position *before* doing anything
			// because it can move while we are reloading the menu:
			var position = Util.MousePosition;

			// suspend/resume layout before/after reloading:
			_notifyIcon.ContextMenuStrip.SuspendLayout();

			if (_streamsFileLoader.MustReload())
			{
				MenuUpdate();
			}

			_toolStripRenderer.UpdateColors();

			_notifyIcon.ContextMenuStrip.ResumeLayout();
			e.Cancel = false;
			_notifyIcon.ShowContextMenuStrip(position);
		}

		/// Balance and volume updating
		/// <summary>
		///    Update the balance label
		///    and send the current value to the player.
		/// </summary>
		private void BalanceUpdate()
		{
			double current = _balanceTrackBar.TrackBar.Value;
			double maximum = _balanceTrackBar.TrackBar.Maximum;

			var balance = current / maximum;
			var percent = balance * 100;

			_balanceTrackBar.Label.Text = "Balance  " + percent;
			_player.Balance = balance;
		}

		/// <summary>
		///    Update the volume label
		///    and send the current value to the player.
		/// </summary>
		private void VolumeUpdate()
		{
			double current = _volumeTrackBar.TrackBar.Value;
			double maximum = _volumeTrackBar.TrackBar.Maximum;

			var volume = current / maximum;
			var percent = volume * 100;

			_volumeTrackBar.Label.Text = "Volume  " + percent;
			_player.Volume = volume;
		}

		/// Multimedia keys hook
		/// <summary>
		///    Start the multimedia keys hook.
		/// </summary>
		private void MultimediaKeysHook()
		{
			try
			{
				KeyboardHook.Hooker.Hook();
				_optionsEnableMultimediaKeysItem.Checked = true;
			}
			catch (KeyboardHookException exception)
			{
				_optionsEnableMultimediaKeysItem.Checked = KeyboardHook.Hooker.IsHooked;

				MessageBox.Show(exception.Message, "Error hooking multimedia keys", MessageBoxButtons.OK);
			}
		}

		/// <summary>
		///    Stop the multimedia keys hook.
		/// </summary>
		/// <param name="quiet">Ignore exceptions instead of showing a message.</param>
		private void MultimediaKeysUnhook(bool quiet = false)
		{
			try
			{
				KeyboardHook.Hooker.Unhook();
				_optionsEnableMultimediaKeysItem.Checked = false;
			}
			catch (KeyboardHookException exception)
			{
				_optionsEnableMultimediaKeysItem.Checked = KeyboardHook.Hooker.IsHooked;

				if (!quiet)
				{
					MessageBox.Show(exception.Message, "Error unhooking multimedia keys", MessageBoxButtons.OK);
				}
			}
		}

		/// Events: icon
		/// <summary>
		///    Toggle play with the left mouse button.
		///    When no stream has been selected, show the context menu instead.
		/// </summary>
		private void OnIconLeftMouseClick()
		{
			if (_player.Source == null)
			{
				_notifyIcon.ShowContextMenuStrip(Util.MousePosition);
			}
			else
			{
				_player.TogglePlay();
			}
		}

		/// <summary>
		///    Toggle mute with the wheel button.
		///    When not playing, show the context menu instead.
		/// </summary>
		private void OnIconMiddleMouseClick()
		{
			if (_player.IsIdle)
			{
				_notifyIcon.ShowContextMenuStrip(Util.MousePosition);
			}
			else
			{
				_player.ToggleMute();
			}
		}

		/// <summary>
		///    Allow control via mouse.
		/// </summary>
		private void OnIconMouseClick(object sender, MouseEventArgs e)
		{
			switch (e.Button)
			{
				case MouseButtons.Left:
					OnIconLeftMouseClick();
					break;

				case MouseButtons.Middle:
					OnIconMiddleMouseClick();
					break;

				case MouseButtons.Right:
				case MouseButtons.XButton1:
				case MouseButtons.XButton2:
					break;
			}
		}

		/// Events: multimedia keys
		private void OnHotkeyDown(object sender, KeyboardHookEventArgs e)
		{
			switch (e.Name)
			{
				case "Toggle Play":
				{
					_player.TogglePlay();
					break;
				}
				case "Stop":
				{
					_player.Stop();
					break;
				}
				case "Toggle Mute":
				{
					_player.ToggleMute();
					break;
				}
				case "Volume Up":
				{
					var volume = _volumeTrackBar.TrackBar.Value;
					var volumeMax = _volumeTrackBar.TrackBar.Maximum;
					var volumeStep = _volumeTrackBar.TrackBar.SmallChange;
					_volumeTrackBar.TrackBar.Value = Math.Min(volume + volumeStep, volumeMax);
					break;
				}
				case "Volume Down":
				{
					var volume = _volumeTrackBar.TrackBar.Value;
					var volumeMin = _volumeTrackBar.TrackBar.Minimum;
					var volumeStep = _volumeTrackBar.TrackBar.SmallChange;
					_volumeTrackBar.TrackBar.Value = Math.Max(volume - volumeStep, volumeMin);
					break;
				}
			}
		}

		/// Events: menu
		/// <summary>
		///    Stream clicked, play it.
		/// </summary>
		private void OnStreamItemClick(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem) sender;
			var stream = new PlayerStream(item.Text, (Uri) item.Tag);

			_player.Play(stream);
		}

		/// <summary>
		///    Opening error clicked, show details.
		/// </summary>
		private void OnErrorOpenItemClick(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem) sender;
			var exception = (Exception) item.Tag;

			var text = exception.Message;
			var caption = "Error opening streams file";
			MessageBox.Show(text, caption, MessageBoxButtons.OK);
		}

		/// <summary>
		///    Reading error clicked, show details. Suggest editing.
		/// </summary>
		private void OnErrorReadItemClick(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem) sender;
			var exception = (StreamsFileReadError) item.Tag;

			var text = $"{exception.FilePath} \n" + $"Error at line {exception.LineNumber}: {exception.Message} \n\n" + $"{exception.Line} \n\n" + "Do you want to open the streams file now?";

			var caption = "Error reading streams file";
			if (Util.MessageBoxYesNo(text, caption))
			{
				StreamsFileOpen();
			}
		}

		/// <summary>
		///    Edit clicked, open the streams file.
		/// </summary>
		private void OnEditItemClick(object sender, EventArgs e)
		{
			StreamsFileOpen();
		}

		/// <summary>
		///    Balance changed, update label and send new value to the player.
		/// </summary>
		private void OnBalanceTrackBarChanged(object sender, EventArgs e)
		{
			BalanceUpdate();
		}

		/// <summary>
		///    Volume changed, update label and send new value to the player.
		/// </summary>
		private void OnVolumeTrackBarChanged(object sender, EventArgs e)
		{
			VolumeUpdate();
		}

		/// <summary>
		///    Multimedia keys clicked, toggle on or off.
		/// </summary>
		private void OnEnableMultimediaKeysItemClicked(object sender, EventArgs e)
		{
			if (_optionsEnableMultimediaKeysItem.Checked)
			{
				MultimediaKeysUnhook();
			}
			else
			{
				MultimediaKeysHook();
			}
		}

		/// <summary>
		///    Exit clicked, stop playing, save settings, hide icon and exit.
		/// </summary>
		private void OnExitItemClick(object sender, EventArgs e)
		{
			_player.Stop();

			_settings.LastBalanceTrackBarValue = _balanceTrackBar.TrackBar.Value;
			_settings.LastVolumeTrackBarValue = _volumeTrackBar.TrackBar.Value;
			_settings.LastPlayerStream = _player.Source;
			_settings.OptionsEnableAutoPlayChecked = _optionsEnableAutoPlayItem.Checked;
			_settings.OptionsEnableMultimediaKeysChecked = _optionsEnableMultimediaKeysItem.Checked;
			SettingsSave();

			// unhook, but don't be annoying with error messages on shutdown:
			if (_optionsEnableMultimediaKeysItem.Checked)
			{
				MultimediaKeysUnhook(true);
			}

			_notifyIcon.Visible = false;
			Application.Exit();
		}
	}
}