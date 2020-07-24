using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models
{
	public class Product : EntityBase
	{
		public string Name { get; set; }
	}
}
