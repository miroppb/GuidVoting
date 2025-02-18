using Dapper.Contrib.Extensions;

namespace GuidVoting.Models
{
	[Table("events")]
	public class Event
	{
		[Key]
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime DateCreated { get; set; }
	}
}
