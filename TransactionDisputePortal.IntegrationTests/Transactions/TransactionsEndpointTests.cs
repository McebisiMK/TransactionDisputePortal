using System.Net;
using System.Text.Json;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.IntegrationTests.TestSupport;

namespace TransactionDisputePortal.IntegrationTests.Transactions;

[TestFixture]
public class TransactionsEndpointTests
{
    private const string ProblemContentType = "application/problem+json";

    private ApiFactory factory = null!;
    private Customer customer = null!;
    private Transaction ownTransaction = null!;
    private Transaction otherCustomersTransaction = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        factory = new ApiFactory();

        customer = CreateCustomer("Integration Customer", "4111222233334444", out ownTransaction);
        var otherCustomer = CreateCustomer("Other Customer", "5500111122223333", out otherCustomersTransaction);

        await factory.SeedAsync(customer, otherCustomer);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => factory.Dispose();

    [Test]
    public async Task Get_Transactions_Given_No_Token_Should_Return_Unauthorized()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClient();

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync("/api/v1/transactions");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestCase("status=Bogus", "Status")]
    [TestCase("fromDate=notadate", "FromDate")]
    [TestCase("accountId=nope", "AccountId")]
    [TestCase("pageNumber=abc", "PageNumber")]
    public async Task Get_Transactions_Given_A_Value_That_Cannot_Be_Bound_Should_Return_Validation_Problem(string queryString, string field)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync($"/api/v1/transactions?{queryString}");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        actual.Content.Headers.ContentType!.MediaType.Should().Be(ProblemContentType);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
        body.RootElement.GetProperty("fieldErrors")[0].GetProperty("field").GetString().Should().Be(field);
    }

    [Test]
    public async Task Get_Transactions_Given_Binding_And_Validator_Failures_Should_Return_The_Same_Problem_Shape()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var bindingFailure = await client.GetAsync("/api/v1/transactions?status=Bogus");
        using var validatorFailure = await client.GetAsync("/api/v1/transactions?pageSize=0");

        //------------------------------------- Assert  ------------------------------------------------------
        validatorFailure.StatusCode.Should().Be(bindingFailure.StatusCode);
        validatorFailure.Content.Headers.ContentType!.MediaType.Should().Be(bindingFailure.Content.Headers.ContentType!.MediaType);

        using var bindingBody = await ReadJson(bindingFailure);
        using var validatorBody = await ReadJson(validatorFailure);
        PropertyNames(bindingBody).Should().Equal(PropertyNames(validatorBody));
        bindingBody.RootElement.GetProperty("title").GetString().Should().Be(validatorBody.RootElement.GetProperty("title").GetString());
        bindingBody.RootElement.GetProperty("type").GetString().Should().Be(validatorBody.RootElement.GetProperty("type").GetString());
    }

    [Test]
    public async Task Get_Transaction_By_Id_Given_The_Customers_Own_Transaction_Should_Return_It_With_A_Masked_Account_Number()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync($"/api/v1/transactions/{ownTransaction.Id}");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("reference").GetString().Should().Be(ownTransaction.Reference);
        body.RootElement.GetProperty("accountNumber").GetString().Should().Be("******4444");
    }

    [Test]
    public async Task Get_Transaction_By_Id_Given_Another_Customers_Transaction_Should_Return_Not_Found()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync($"/api/v1/transactions/{otherCustomersTransaction.Id}");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.NotFound);
        actual.Content.Headers.ContentType!.MediaType.Should().Be(ProblemContentType);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("title").GetString().Should().Be("Not Found");
        body.RootElement.GetProperty("detail").GetString().Should().Be($"Entity \"Transaction\" ({otherCustomersTransaction.Id}) was not found.");
        body.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Test]
    public async Task Get_Transaction_By_Id_Given_An_Empty_Id_Should_Return_Validation_Problem()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync($"/api/v1/transactions/{Guid.Empty}");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static Customer CreateCustomer(string fullName, string accountNumber, out Transaction transaction)
    {
        var customer = new Customer
        {
            ExternalUserId = Guid.NewGuid(),
            FullName = fullName,
            Email = $"{fullName.Replace(' ', '.').ToLowerInvariant()}@disputeportal.local"
        };
        var account = new Account { Customer = customer, AccountNumber = accountNumber, Type = AccountType.CreditCard };
        transaction = new Transaction
        {
            Account = account,
            Reference = $"TXN-{Guid.NewGuid():N}"[..20],
            Amount = 199.99m,
            Currency = "ZAR",
            Merchant = "Woolworths Food",
            Category = TransactionCategory.Groceries,
            TransactionDate = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc),
            Status = TransactionStatus.Posted
        };
        account.Transactions.Add(transaction);
        customer.Accounts.Add(account);

        return customer;
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static IEnumerable<string> PropertyNames(JsonDocument document) => document.RootElement.EnumerateObject().Select(property => property.Name).Order();
}
