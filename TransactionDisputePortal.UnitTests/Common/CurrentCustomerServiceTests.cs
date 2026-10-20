using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Services;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Common;

[TestFixture]
public class CurrentCustomerServiceTests
{
    [Test]
    public async Task GetCustomerIdAsync_Given_The_User_Is_Linked_To_A_Customer_Should_Return_The_Customer_Id()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        var customer = TestData.CreateCustomer();
        context.Customers.AddRange(customer, TestData.CreateCustomer("Other Customer"));
        await context.SaveChangesAsync();

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(customer.ExternalUserId);

        var currentCustomerService = CreateCurrentCustomerService(currentUserService, context);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await currentCustomerService.GetCustomerIdAsync(CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().Be(customer.Id);
    }

    [Test]
    public async Task GetCustomerIdAsync_Given_It_Was_Already_Resolved_Should_Not_Query_The_Database_Again()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        var customer = TestData.CreateCustomer();
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(customer.ExternalUserId);

        var currentCustomerService = CreateCurrentCustomerService(currentUserService, context);
        await currentCustomerService.GetCustomerIdAsync(CancellationToken.None);

        context.Customers.Remove(customer);
        await context.SaveChangesAsync();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await currentCustomerService.GetCustomerIdAsync(CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().Be(customer.Id);
    }

    [Test]
    public async Task GetCustomerIdAsync_Given_The_Token_Has_No_User_Id_Should_Throw_Forbidden()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns((Guid?)null);

        var currentCustomerService = CreateCurrentCustomerService(currentUserService, context);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => currentCustomerService.GetCustomerIdAsync(CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetCustomerIdAsync_Given_The_User_Is_Not_Linked_To_A_Customer_Should_Throw_Forbidden()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.Add(TestData.CreateCustomer());
        await context.SaveChangesAsync();

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.NewGuid());

        var currentCustomerService = CreateCurrentCustomerService(currentUserService, context);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => currentCustomerService.GetCustomerIdAsync(CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private static CurrentCustomerService CreateCurrentCustomerService(ICurrentUserService currentUserService, IApplicationDbContext context)
    {
        return new CurrentCustomerService(context, currentUserService);
    }
}
