using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Domain.Shared
{
    public abstract class Entity<TId>
    {
        public TId Id { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }

        protected Entity(TId id)
        {
            Id = id;
            CreatedDate = DateTime.UtcNow;
        }

        protected Entity() { }
    }
}
