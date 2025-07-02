using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

public class DocumentShareView : BaseEntity
{
    public Guid ShareId { get; set; }
    public virtual DocumentShare Share { get; set; }

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    public string ViewerIP { get; set; }
    public string UserAgent { get; set; }
}