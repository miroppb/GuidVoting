using Dapper.Contrib.Extensions;

namespace GuidVoting.Models
{
	[Table("vote_guids")]
	public class VoteGuids
	{
		[Key]
		public int Id { get; set; }
		public int EventId { get; set; }
		public Guid Guid { get; set; }
	}
}
