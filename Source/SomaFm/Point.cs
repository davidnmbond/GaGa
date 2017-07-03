using System.Runtime.InteropServices;

namespace SomaFm
{
	internal static partial class Util
	{
		/// Private Windows API declarations
		[StructLayout(LayoutKind.Sequential)]
		private struct Point
		{
			public readonly int X;
			public readonly int Y;

			//public Point(int x, int y)
			//{
			//	X = x;
			//	Y = y;
			//}
		}
	}
}