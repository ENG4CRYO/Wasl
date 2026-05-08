using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Core.Entities.BaseEntity
{
    public abstract class BaseAuditableEntity<TId> : IEntity<TId>
    {
        public TId Id { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
