using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models
{
	public class Customer : EntityBase
	{
		public string Name { get; set; }
		public List<Product> Products { get; set; } = new List<Product>();
	}
}
