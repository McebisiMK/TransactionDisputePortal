using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Infrastructure.Services;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Infrastructure;

[TestFixture]
public class DomainEventDispatcherTests
{
    private sealed record OtherTestEvent : DomainEvent;

    [Test]
    public async Task DispatchAsync_Given_Multiple_Domain_Events_Should_Publish_Each_Wrapped_In_A_Typed_Notification_In_Order()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var createdEvent = new TestEntityCreated(Guid.NewGuid());
        var otherEvent = new OtherTestEvent();
        var expectedFirstNotification = new DomainEventNotification<TestEntityCreated>(createdEvent);
        var expectedSecondNotification = new DomainEventNotification<OtherTestEvent>(otherEvent);

        var publisher = Substitute.For<IPublisher>();

        var domainEventDispatcher = CreateDomainEventDispatcher(publisher);

        //------------------------------------- Act     ------------------------------------------------------
        await domainEventDispatcher.DispatchAsync([createdEvent, otherEvent], CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        Received.InOrder(() =>
        {
            publisher.Publish(Arg.Is<INotification>(notification => notification.Equals(expectedFirstNotification)), Arg.Any<CancellationToken>());
            publisher.Publish(Arg.Is<INotification>(notification => notification.Equals(expectedSecondNotification)), Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task DispatchAsync_Given_No_Domain_Events_Should_Not_Publish_Anything()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var publisher = Substitute.For<IPublisher>();

        var domainEventDispatcher = CreateDomainEventDispatcher(publisher);

        //------------------------------------- Act     ------------------------------------------------------
        await domainEventDispatcher.DispatchAsync([], CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await publisher.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    private static DomainEventDispatcher CreateDomainEventDispatcher(IPublisher publisher)
    {
        return new DomainEventDispatcher(publisher, NullLogger<DomainEventDispatcher>.Instance);
    }
}
