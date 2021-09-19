using Core.Extensions;
using Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace Core
{
    public abstract class EntityBase : IEntityBase
    {
        [Key]
        public Guid Id { get; set; }

        public virtual bool IsNew()
        {
            return Id.IsNullOrEmpty();
        }
    }
}