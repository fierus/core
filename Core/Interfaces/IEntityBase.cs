using System;

namespace Core.Interfaces
{
    public interface IEntityBase
    {
        Guid Id { get; set; }

        bool IsNew();
    }
}