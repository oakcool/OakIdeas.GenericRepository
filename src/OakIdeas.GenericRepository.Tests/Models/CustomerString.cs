using OakIdeas.GenericRepository.Models;

namespace OakIdeas.GenericRepository.Tests.Models
{
	public class CustomerString : EntityBase<string>
	{
		public string Name { get; set; }
	}
}
