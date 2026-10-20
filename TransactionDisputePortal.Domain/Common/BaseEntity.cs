namespace TransactionDisputePortal.Domain.Common;

public abstract class BaseEntity
{
    private readonly List<DomainEvent> domainEvents = [];

    public Guid Id { get; set; } = Guid.NewGuid();

    public IReadOnlyCollection<DomainEvent> DomainEvents => domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        domainEvents.Clear();
    }
}
