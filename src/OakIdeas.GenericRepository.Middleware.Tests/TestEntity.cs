using OakIdeas.GenericRepository.Models;

namespace OakIdeas.GenericRepository.Middleware.Tests;

public class TestEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
