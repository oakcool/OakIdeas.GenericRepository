using OakIdeas.GenericRepository.Models;

namespace OakIdeas.GenericRepository.Tests.Models
{
	public class CustomerLong : EntityBase<long>
	{
		public string Name { get; set; } = string.Empty;
	}
}
