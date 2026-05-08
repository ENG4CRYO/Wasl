using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Core.Entities.BaseEntity
{
    public interface IEntity<TId>
    {
        TId Id { get; set; }
    }
}
