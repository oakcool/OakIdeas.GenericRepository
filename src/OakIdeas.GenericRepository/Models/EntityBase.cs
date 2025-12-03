using System;
using System.Collections.Generic;
using System.Text;

namespace OakIdeas.GenericRepository.Models
{
    /// <summary>
    /// Base class for entities with an integer primary key.
    /// </summary>
	public abstract class EntityBase
	{
        /// <summary>
        /// Gets or sets the primary key identifier.
        /// </summary>
		public int ID { get; set; }
	}
}
