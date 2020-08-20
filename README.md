# GenericRepository
![OakIdeas.GenericRepository - Deploy](https://github.com/oakcool/OakIdeas.GenericRepository/workflows/OakIdeas.GenericRepository%20-%20Deploy/badge.svg)
A very simple and generic implementation of the repository pattern for CRUD operations.

## "Features"

- Generic interface
- EntityBase
- Memory Repository (Dictionary based)


## Usage

```C#
	public class Customer : EntityBase
	{
		public string Name { get; set; }
	}

	class Program
	{
		static void Main(string[] args)
		{   
		    // Create an instance of repositoy for Customers
		    var CustomerManager = new MemoryDataManager<Customer>();         

		    // Create (C)
		    var MyCustomer = CustomerManager.Insert(new Customer() { Name = name }).Result;

		    MyCustomer = null;

		    // Retrieve (R)
		    MyCustomer = CustomerManager.Get(x => x.Name == name).Result.FirstOrDefault();

		    // Update (U)
		    MyCustomer.Name = "Name Changed";
		    MyCustomer = CustomerManager.Update(MyCustomer).Result;

		    // Delete (D)
		    var deleted = CustomerManager.Delete(MyCustomer).Result;

		}
    	}
```
