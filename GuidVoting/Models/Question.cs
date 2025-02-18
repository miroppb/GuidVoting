using Dapper.Contrib.Extensions;

namespace GuidVoting.Models
{
	[Table("questions")]
	public class Question
	{
		[Key]
		public int Id { get; set; }
		public string Text { get; set; }
		public int EventId { get; set; } // Foreign key linking to an Event
	}
}
