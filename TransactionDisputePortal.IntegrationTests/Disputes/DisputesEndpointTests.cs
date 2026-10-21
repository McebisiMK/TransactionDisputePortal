using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.IntegrationTests.TestSupport;

namespace TransactionDisputePortal.IntegrationTests.Disputes;

[TestFixture]
public class DisputesEndpointTests
{
    private const string ProblemContentType = "application/problem+json";

    private ApiFactory factory = null!;
    private Customer customer = null!;
    private Customer otherCustomer = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        factory = new ApiFactory();

        customer = CreateCustomer("Integration Customer");
        otherCustomer = CreateCustomer("Other Customer");

        await factory.SeedAsync(customer, otherCustomer);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => factory.Dispose();

    [Test]
    public async Task Post_Dispute_Given_A_Disputable_Transaction_Should_Return_Created_With_A_Location_That_Returns_The_Dispute()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(customer);
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "DuplicateCharge" });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Created);

        using var created = await ReadJson(actual);
        var disputeId = created.RootElement.GetProperty("id").GetGuid();
        created.RootElement.GetProperty("reference").GetString().Should().MatchRegex(@"^DSP-\d{4}-\d{6}$");
        created.RootElement.GetProperty("status").GetString().Should().Be("Submitted");
        actual.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/disputes/{disputeId}");

        using var fetched = await client.GetAsync(actual.Headers.Location);
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        using var fetchedBody = await ReadJson(fetched);
        fetchedBody.RootElement.GetProperty("id").GetGuid().Should().Be(disputeId);
    }

    [Test]
    public async Task Post_Dispute_Given_A_Dispute_Was_Created_Should_Show_The_Transaction_As_Not_Disputable()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(customer);
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        using var created = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "DuplicateCharge" });
        using var createdBody = await ReadJson(created);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync($"/api/v1/transactions/{transaction.Id}");

        //------------------------------------- Assert  ------------------------------------------------------
        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("isDisputable").GetBoolean().Should().BeFalse();
        body.RootElement.GetProperty("openDisputeId").GetGuid().Should().Be(createdBody.RootElement.GetProperty("id").GetGuid());
    }

    [Test]
    public async Task Post_Dispute_Given_The_Transaction_Already_Has_An_Open_Dispute_Should_Return_Conflict()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(customer);
        using var client = factory.CreateClientFor(customer.ExternalUserId);
        using var first = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "DuplicateCharge" });

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "IncorrectAmount" });

        //------------------------------------- Assert  ------------------------------------------------------
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        actual.StatusCode.Should().Be(HttpStatusCode.Conflict);
        actual.Content.Headers.ContentType!.MediaType.Should().Be(ProblemContentType);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("detail").GetString().Should().Be($"Transaction {transaction.Reference} already has an open dispute.");
    }

    [Test]
    public async Task Post_Dispute_Given_A_Pending_Transaction_Should_Return_Conflict()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(customer, status: TransactionStatus.Pending);
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "DuplicateCharge" });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Post_Dispute_Given_Another_Customers_Transaction_Should_Return_Not_Found()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(otherCustomer);
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "DuplicateCharge" });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Post_Dispute_Given_Reason_Other_Without_A_Description_Should_Return_Validation_Problem()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(customer);
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "Other" });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("fieldErrors")[0].GetProperty("field").GetString().Should().Be("Description");
    }

    [Test]
    public async Task Post_Dispute_Given_An_Unknown_Reason_Should_Return_Validation_Problem()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(customer);
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "Bogus" });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        actual.Content.Headers.ContentType!.MediaType.Should().Be(ProblemContentType);
    }

    [Test]
    public async Task Get_Dispute_By_Id_Given_Another_Customers_Dispute_Should_Return_Not_Found()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(otherCustomer);
        using var otherClient = factory.CreateClientFor(otherCustomer.ExternalUserId);
        using var created = await otherClient.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "DuplicateCharge" });

        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync(created.Headers.Location);

        //------------------------------------- Assert  ------------------------------------------------------
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        actual.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Get_Dispute_By_Id_Should_Return_The_Transaction_Summary_And_Timeline()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = await SeedTransaction(customer);
        using var client = factory.CreateClientFor(customer.ExternalUserId);
        var disputeId = await CreateDispute(client, transaction);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync($"/api/v1/disputes/{disputeId}");

        //------------------------------------- Assert  ------------------------------------------------------
        using var body = await ReadJson(actual);
        var transactionSummary = body.RootElement.GetProperty("transaction");
        transactionSummary.GetProperty("reference").GetString().Should().Be(transaction.Reference);
        transactionSummary.GetProperty("accountNumber").GetString().Should().Be("******4444");

        var entry = body.RootElement.GetProperty("timeline").EnumerateArray().Should().ContainSingle().Subject;
        entry.GetProperty("fromStatus").ValueKind.Should().Be(JsonValueKind.Null);
        entry.GetProperty("toStatus").GetString().Should().Be("Submitted");
    }

    private static async Task<Guid> CreateDispute(HttpClient client, Transaction transaction)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "DuplicateCharge" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var body = await ReadJson(response);

        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static Customer CreateCustomer(string fullName)
    {
        var customer = new Customer
        {
            ExternalUserId = Guid.NewGuid(),
            FullName = fullName,
            Email = $"{fullName.Replace(' ', '.').ToLowerInvariant()}@disputeportal.local"
        };
        customer.Accounts.Add(new Account { Customer = customer, AccountNumber = "4111222233334444", Type = AccountType.CreditCard });

        return customer;
    }

    private async Task<Transaction> SeedTransaction(Customer owner, TransactionStatus status = TransactionStatus.Posted)
    {
        var transaction = new Transaction
        {
            AccountId = owner.Accounts[0].Id,
            Reference = $"TXN-{Guid.NewGuid():N}"[..20],
            Amount = 199.99m,
            Currency = "ZAR",
            Merchant = "Woolworths Food",
            Category = TransactionCategory.Groceries,
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            Status = status
        };

        await factory.SeedAsync(transaction);

        return transaction;
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
