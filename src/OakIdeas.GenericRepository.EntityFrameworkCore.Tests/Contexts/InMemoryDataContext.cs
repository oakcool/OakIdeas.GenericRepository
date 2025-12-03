using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts
{
	public class InMemoryDataContext : DbContext
	{
		public DbSet<Customer> Customers { get; set; }
		public DbSet<Product> Products { get; set; }

		public InMemoryDataContext() : base()
		{
		}

		public InMemoryDataContext(DbContextOptions<InMemoryDataContext> options) : base(options)
		{
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseInMemoryDatabase("CustomerDB");
			}
		}
	}
}
