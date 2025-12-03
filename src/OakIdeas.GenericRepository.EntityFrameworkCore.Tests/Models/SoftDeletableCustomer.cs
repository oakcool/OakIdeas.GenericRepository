using OakIdeas.GenericRepository.Models;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models
{
    public class SoftDeletableCustomer : SoftDeletableEntity
    {
        public string Name { get; set; } = string.Empty;
    }
}
