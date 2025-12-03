using OakIdeas.GenericRepository.Models;

namespace OakIdeas.GenericRepository.Tests.Models
{
    public class SoftDeletableCustomer : SoftDeletableEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    public class SoftDeletableCustomerGuid : SoftDeletableEntity<System.Guid>
    {
        public string Name { get; set; } = string.Empty;
    }
}
