using System;
using SomaFm.NotifyIconPlayer;

namespace SomaFm
{
	[Serializable]
	internal class SomaFmSettings
	{
		/// <summary>
		///    Last balance value set in the audio menu.
		/// </summary>
		public int LastBalanceTrackBarValue;

		/// <summary>
		///    Last stream played.
		/// </summary>
		public PlayerStream LastPlayerStream;

		/// <summary>
		///    Last volume value set in the audio menu.
		/// </summary>
		public int LastVolumeTrackBarValue;

		/// <summary>
		///    Whether the enable auto play options is checked.
		/// </summary>
		public bool OptionsEnableAutoPlayChecked;

		/// <summary>
		///    Whether the multimedia keys option is checked.
		/// </summary>
		public bool OptionsEnableMultimediaKeysChecked;

		/// <summary>
		///    Stores program settings.
		/// </summary>
		public SomaFmSettings()
		{
			LastBalanceTrackBarValue = 0;
			LastVolumeTrackBarValue = 10;
			LastPlayerStream = null;
			OptionsEnableAutoPlayChecked = false;
			OptionsEnableMultimediaKeysChecked = true;
		}
	}
}