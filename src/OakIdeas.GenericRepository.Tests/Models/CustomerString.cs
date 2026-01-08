
using OakIdeas.GenericRepository.Models;
using System;
namespace OakIdeas.GenericRepository.Tests.Models
{
	public class CustomerString : EntityBase<string>
	{
		public string Name { get; set; } = string.Empty;
	}
}
