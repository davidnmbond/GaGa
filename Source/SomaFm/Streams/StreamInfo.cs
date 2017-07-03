using System.Runtime.Serialization;

namespace SomaFm.Streams
{
	[DataContract]
	public class StreamInfo
	{
		[DataMember(Name="group")]
		public string Group { get; set; }

		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "url")]
		public string Url { get; set; }
	}
}
