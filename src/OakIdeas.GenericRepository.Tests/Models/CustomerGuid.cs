using OakIdeas.GenericRepository.Models;
using System;

namespace OakIdeas.GenericRepository.Tests.Models
{
	public class CustomerGuid : EntityBase<Guid>
	{
		public string Name { get; set; } = string.Empty;
	}
}
