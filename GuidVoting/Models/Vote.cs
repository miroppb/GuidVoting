using Dapper.Contrib.Extensions;

namespace GuidVoting.Models
{
	[Table("votes")]
	public class Vote
	{
		[Key]
		public int Id { get; set; }
		public int QuestionId { get; set; }
		public int ChoiceId { get; set; }
		public Guid Guid { get; set; }
	}
}
