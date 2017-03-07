using System;

namespace SomaFm.StreamsFile
{
	internal class StreamsFileReadError : Exception
	{
		/// <summary>
		///    Path to the file that triggered the error.
		/// </summary>
		public readonly string FilePath;

		/// <summary>
		///    Text for the incorrect line.
		/// </summary>
		public readonly string Line;

		/// <summary>
		///    Line number where the error happened.
		/// </summary>
		public readonly int LineNumber;

		/// <summary>
		///    Raised by StreamsFileReader on a reading error.
		/// </summary>
		/// <param name="message">Error message.</param>
		/// <param name="filepath">Path to the file that triggered the error.</param>
		/// <param name="line">Text for the incorrect line.</param>
		/// <param name="linenumber">Line number where the error happened.</param>
		public StreamsFileReadError(string message, string filepath, string line, int linenumber) : base(message)
		{
			FilePath = filepath;
			Line = line;
			LineNumber = linenumber;
		}
	}
}