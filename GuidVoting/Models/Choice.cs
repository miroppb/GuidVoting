using Dapper.Contrib.Extensions;

namespace GuidVoting.Models
{
	[Table("choices")]
	public class Choice
	{
		[Key]
		public int Id { get; set; }
		public int QuestionId { get; set; }
		public string ChoiceText { get; set; }
	}
}
