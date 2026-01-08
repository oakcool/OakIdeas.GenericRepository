using OakIdeas.GenericRepository.Models;
using System.Collections.Generic;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models
{
public class Customer : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = [];
}
}
