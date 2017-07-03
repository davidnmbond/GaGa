using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace SomaFm.NotifyIconPlayer
{
	internal class Player
	{
		private readonly Icon[] _bufferingIcons;

		private readonly DispatcherTimer _bufferingIconTimer;

		private readonly Icon _idleIcon;
		private readonly NotifyIcon _notifyIcon;
		private readonly MediaPlayer _player;
		private readonly Icon _playingIcon;
		private readonly Icon _playingMutedIcon;
		private int _currentBufferingIcon;

		/// <summary>
		///    A media player that takes control of a NotifyIcon icon,
		///    tooltip and balloon to display status.
		/// </summary>
		/// <param name="icon">
		///    The notify icon to use to display status.
		/// </param>
		public Player(NotifyIcon icon)
		{
			_notifyIcon = icon;

			_player = new MediaPlayer();
			_player.BufferingStarted += OnBufferingStarted;
			_player.BufferingEnded += OnBufferingEnded;
			_player.MediaEnded += OnMediaEnded;
			_player.MediaFailed += OnMediaFailed;

			_idleIcon = Util.ResourceAsIcon("SomaFm.NotifyIconPlayer.Resources.Idle.ico");
			_playingIcon = Util.ResourceAsIcon("SomaFm.NotifyIconPlayer.Resources.Playing.ico");
			_playingMutedIcon = Util.ResourceAsIcon("SomaFm.NotifyIconPlayer.Resources.Playing-muted.ico");

			_bufferingIcons = new[]
			{
				Util.ResourceAsIcon("SomaFm.NotifyIconPlayer.Resources.Buffering1.ico"),
				Util.ResourceAsIcon("SomaFm.NotifyIconPlayer.Resources.Buffering2.ico"),
				Util.ResourceAsIcon("SomaFm.NotifyIconPlayer.Resources.Buffering3.ico"),
				Util.ResourceAsIcon("SomaFm.NotifyIconPlayer.Resources.Buffering4.ico")
			};

			_bufferingIconTimer = new DispatcherTimer(DispatcherPriority.Background)
			{
				Interval = TimeSpan.FromMilliseconds(300)
			};
			_bufferingIconTimer.Tick += OnBufferingIconTimerTick;
			_currentBufferingIcon = 0;

			Source = null;
			IsIdle = true;

			UpdateIcon();
		}

		/// <summary>
		///    Determine whether the player is currently idle.
		/// </summary>
		public bool IsIdle { get; private set; }

		/// <summary>
		///    Get the current player stream.
		/// </summary>
		public PlayerStream Source { get; private set; }

		/// <summary>
		///    Get or set the player balance.
		/// </summary>
		public double Balance
		{
			// ReSharper disable once UnusedMember.Global
			get => _player.Balance;
			set => _player.Balance = value;
		}

		/// <summary>
		///    Get or set the player volume.
		/// </summary>
		public double Volume
		{
			// ReSharper disable once UnusedMember.Global
			get => _player.Volume;
			set => _player.Volume = value;
		}

		/// Icon handling
		/// <summary>
		///    Updates the NotifyIcon icon and tooltip text
		///    depending on the current player state.
		/// </summary>
		private void UpdateIcon()
		{
			Icon icon;
			string text;

			// player state
			if (IsIdle)
			{
				icon = _idleIcon;
				text = "Idle";
			}
			else if (_player.IsMuted)
			{
				icon = _playingMutedIcon;
				text = "Playing (muted)";
			}
			else
			{
				icon = _playingIcon;
				text = "Playing";
			}

			// separator:
			text += " - ";

			// source state:
			if (Source == null)
			{
				text += "No stream selected";
			}
			else
			{
				text += Source.Name;
			}

			_notifyIcon.Icon = icon;
			_notifyIcon.SetToolTipText(text);
		}

		/// Player
		/// <summary>
		///    Open and play the current source stream.
		///    Un-mutes the player.
		/// </summary>
		public void Play()
		{
			// do nothing if there is no current source:
			if (Source == null)
				return;

			_player.Open(Source.Uri);
			_player.Play();
			_player.IsMuted = false;

			IsIdle = false;
			UpdateIcon();
		}

		/// <summary>
		///    Stop playing and close the current stream.
		///    Un-mutes the player.
		/// </summary>
		public void Stop()
		{
			// do nothing if already idle:
			if (IsIdle)
				return;

			// corner case:
			// if we only call .Stop(), the player continues downloading
			// from on-line streams, but .Close() calls _mediaState.Init()
			// changing the balance/volume, so save and restore them:
			var balance = _player.Balance;
			var volume = _player.Volume;

			_player.Stop();
			_player.Close();
			_player.IsMuted = false;

			_player.Balance = balance;
			_player.Volume = volume;

			_bufferingIconTimer.Stop();
			_currentBufferingIcon = 0;

			IsIdle = true;
			UpdateIcon();
		}

		/// <summary>
		///    Set a given stream as current and play it.
		///    Unmutes the player.
		/// </summary>
		/// <param name="stream">Source stream to play.</param>
		public void Play(PlayerStream stream)
		{
			Source = stream;
			Play();
		}

		/// <summary>
		///    Stop playing and set a given stream as current.
		///    Unmutes the player.
		/// </summary>
		/// <param name="stream">Source stream to set as current.</param>
		public void Select(PlayerStream stream)
		{
			Stop();
			Source = stream;
			UpdateIcon();
		}

		/// <summary>
		///    Mute the player.
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		public void Mute()
		{
			// do nothing if idle or already muted:
			if (IsIdle || _player.IsMuted)
				return;

			_player.IsMuted = true;
			UpdateIcon();
		}

		/// <summary>
		///    Unmute the player.
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		public void UnMute()
		{
			// do nothing if idle or not muted:
			if (IsIdle || !_player.IsMuted)
				return;

			_player.IsMuted = false;
			UpdateIcon();
		}

		/// <summary>
		///    Toggle between idle/playing.
		/// </summary>
		public void TogglePlay()
		{
			if (IsIdle)
			{
				Play();
			}
			else
			{
				Stop();
			}
		}

		/// <summary>
		///    Toggle between muted/unmuted.
		/// </summary>
		public void ToggleMute()
		{
			if (_player.IsMuted)
			{
				UnMute();
			}
			else
			{
				Mute();
			}
		}

		/// Buffering animation
		/// <summary>
		///    Start watching the buffering state to update our icon.
		/// </summary>
		private void OnBufferingStarted(object sender, EventArgs e)
		{
			_bufferingIconTimer.Start();
		}

		/// <summary>
		///    Stop watching the buffering state.
		/// </summary>
		private void OnBufferingEnded(object sender, EventArgs e)
		{
			_bufferingIconTimer.Stop();
			_currentBufferingIcon = 0;
			UpdateIcon();
		}

		/// <summary>
		///    Override the current icon with an animation while buffering.
		/// </summary>
		private void OnBufferingIconTimerTick(object sender, EventArgs e)
		{
			// change the icon when NOT muted
			// the mute icon has priority over the buffering icons:
			if (!_player.IsMuted)
			{
				_notifyIcon.Icon = _bufferingIcons[_currentBufferingIcon];
			}

			// keep the animation always running:
			_currentBufferingIcon++;
			if (_currentBufferingIcon == _bufferingIcons.Length)
			{
				_currentBufferingIcon = 0;
			}
		}

		/// Media events
		/// <summary>
		///    Update state when media ended.
		/// </summary>
		private void OnMediaEnded(object sender, EventArgs e)
		{
			Stop();
		}

		/// <summary>
		///    Update state when media failed. Show an error balloon with details.
		/// </summary>
		private void OnMediaFailed(object sender, ExceptionEventArgs e)
		{
			Stop();

			_notifyIcon.ShowBalloonTip(10, $"Unable to play: {Source.Name}", $"{e.ErrorException.Message}\n{Source.Uri}", ToolTipIcon.Error);
		}
	}
}