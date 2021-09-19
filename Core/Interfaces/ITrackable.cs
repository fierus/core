using System;

namespace Core.Interfaces
{
    public interface ITrackable
    {
        Guid CreatedById { get; set; }
        DateTime CreatedAt { get; set; }
        Guid? ModifiedById { get; set; }
        DateTime? ModifiedAt { get; set; }
    }
}