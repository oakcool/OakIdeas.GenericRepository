using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts
{
	public class InMemoryDataContext : DbContext
	{
		public DbSet<Customer> Customers { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<SoftDeletableCustomer> SoftDeletableCustomers { get; set; }

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

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Customer>()
				.HasKey(c => c.ID);
			modelBuilder.Entity<Customer>()
				.Property(c => c.ID)
				.ValueGeneratedOnAdd();
			modelBuilder.Entity<SoftDeletableCustomer>()
				.HasKey(c => c.ID);
			modelBuilder.Entity<SoftDeletableCustomer>()
				.Property(c => c.ID)
				.ValueGeneratedOnAdd();
			base.OnModelCreating(modelBuilder);
		}
	}
}
