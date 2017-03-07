using System;
using System.IO;
using System.Reflection;

namespace SomaFm.StreamsFile
{
	internal static class Util
	{
		/// Contants
		/// <summary>
		///    GetLastWriteTime() returns this when a file is not found.
		/// </summary>
		public static readonly DateTime FileNotFoundUtc = DateTime.FromFileTimeUtc(0);

		/// Resources
		/// <summary>
		///    Copy an embedded resource to a file.
		/// </summary>
		/// <param name="resource">Resource name, including namespace.</param>
		/// <param name="filepath">Destination path.</param>
		public static void ResourceCopy(string resource, string filepath)
		{
			var assembly = Assembly.GetExecutingAssembly();
			using (var source = assembly.GetManifestResourceStream(resource))
			{
				using (var target = new FileStream(filepath, FileMode.Create, FileAccess.Write))
				{
					source.CopyTo(target);
				}
			}
		}
	}
}