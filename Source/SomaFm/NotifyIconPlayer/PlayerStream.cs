using System;

namespace SomaFm.NotifyIconPlayer
{
	[Serializable]
	internal class PlayerStream
	{
		/// <summary>
		///    Stream name.
		/// </summary>
		public readonly string Name;

		/// <summary>
		///    Streaming URI.
		/// </summary>
		public readonly Uri Uri;

		/// <summary>
		///    Represents a named media stream.
		/// </summary>
		/// <param name="name">Stream name.</param>
		/// <param name="uri">Streaming URI.</param>
		public PlayerStream(string name, Uri uri)
		{
			Name = name;
			Uri = uri;
		}
	}
}