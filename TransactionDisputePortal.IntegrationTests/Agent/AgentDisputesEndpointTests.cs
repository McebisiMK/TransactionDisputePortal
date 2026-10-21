using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TransactionDisputePortal.Application.Common.Security;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.IntegrationTests.TestSupport;

namespace TransactionDisputePortal.IntegrationTests.Agent;

[TestFixture]
public class AgentDisputesEndpointTests
{
    private const string ProblemContentType = "application/problem+json";

    private ApiFactory factory = null!;
    private Customer customer = null!;
    private HttpClient customerClient = null!;
    private HttpClient agentClient = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        factory = new ApiFactory();

        customer = new Customer { ExternalUserId = Guid.NewGuid(), FullName = "Agent Test Customer", Email = "agent.test.customer@disputeportal.local" };
        customer.Accounts.Add(new Account { Customer = customer, AccountNumber = "4111222233334444", Type = AccountType.CreditCard });
        await factory.SeedAsync(customer);

        customerClient = factory.CreateClientFor(customer.ExternalUserId);
        agentClient = factory.CreateClientFor(Guid.NewGuid(), Roles.Agent);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        customerClient.Dispose();
        agentClient.Dispose();
        factory.Dispose();
    }

    [Test]
    public async Task Get_Queue_Should_Return_Open_Disputes_Oldest_First_Without_Closed_Ones()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var older = await CreateDispute();
        var newer = await CreateDispute();
        var withdrawn = await CreateDispute();
        using var withdrawResponse = await customerClient.PostAsync($"/api/v1/disputes/{withdrawn}/withdraw", content: null);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await agentClient.GetAsync("/api/v1/agent/disputes?pageSize=100");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await ReadJson(actual);
        var items = body.RootElement.GetProperty("items").EnumerateArray().ToList();
        var ids = items.Select(item => item.GetProperty("id").GetGuid()).ToList();
        ids.Should().NotContain(withdrawn);
        ids.IndexOf(older).Should().BeLessThan(ids.IndexOf(newer));
        items.Single(item => item.GetProperty("id").GetGuid() == older).GetProperty("customerName").GetString().Should().Be("Agent Test Customer");
    }

    [Test]
    public async Task Get_Queue_Given_A_Customer_Should_Return_Forbidden()
    {
        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await customerClient.GetAsync("/api/v1/agent/disputes");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Patch_Status_Given_A_Customer_Should_Return_Forbidden_And_Leave_The_Dispute_Unchanged()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var disputeId = await CreateDispute();

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await customerClient.PatchAsJsonAsync($"/api/v1/agent/disputes/{disputeId}/status", new { status = "Approved", comment = "Approve myself.", rowVersion = "" });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await GetCustomerDispute(disputeId)).GetProperty("status").GetString().Should().Be("Submitted");
    }

    [Test]
    public async Task Get_Customer_Disputes_Given_An_Agent_Should_Return_Forbidden()
    {
        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await agentClient.GetAsync("/api/v1/disputes");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Patch_Status_Should_Review_Then_Resolve_The_Dispute_And_The_Customer_Should_See_Every_Change()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var disputeId = await CreateDispute();
        var rowVersion = await GetRowVersion(disputeId);

        //------------------------------------- Act     ------------------------------------------------------
        using var reviewed = await agentClient.PatchAsJsonAsync($"/api/v1/agent/disputes/{disputeId}/status",
            new { status = "UnderReview", comment = "Requested records from the merchant.", rowVersion });
        using var reviewedBody = await ReadJson(reviewed);

        using var resolved = await agentClient.PatchAsJsonAsync($"/api/v1/agent/disputes/{disputeId}/status",
            new { status = "Approved", comment = "Refunded to your account.", rowVersion = reviewedBody.RootElement.GetProperty("rowVersion").GetString() });

        //------------------------------------- Assert  ------------------------------------------------------
        reviewed.StatusCode.Should().Be(HttpStatusCode.OK);
        resolved.StatusCode.Should().Be(HttpStatusCode.OK);

        var dispute = await GetCustomerDispute(disputeId);
        dispute.GetProperty("status").GetString().Should().Be("Approved");

        var timeline = dispute.GetProperty("timeline").EnumerateArray().ToList();
        timeline.Select(entry => entry.GetProperty("toStatus").GetString()).Should().Equal("Submitted", "UnderReview", "Approved");
        timeline[1].GetProperty("changedBy").GetString().Should().Be("integration.agent");
        timeline[2].GetProperty("comment").GetString().Should().Be("Refunded to your account.");
    }

    [Test]
    public async Task Patch_Status_Given_A_Stale_Row_Version_Should_Return_Conflict()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var disputeId = await CreateDispute();

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await agentClient.PatchAsJsonAsync($"/api/v1/agent/disputes/{disputeId}/status",
            new { status = "UnderReview", rowVersion = Convert.ToBase64String([0, 0, 0, 0, 0, 0, 7, 209]) });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Conflict);
        actual.Content.Headers.ContentType!.MediaType.Should().Be(ProblemContentType);
        (await GetCustomerDispute(disputeId)).GetProperty("status").GetString().Should().Be("Submitted");
    }

    [Test]
    public async Task Patch_Status_Given_A_Change_The_Current_Status_Does_Not_Allow_Should_Return_Conflict()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var disputeId = await CreateDispute();
        var rowVersion = await GetRowVersion(disputeId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await agentClient.PatchAsJsonAsync($"/api/v1/agent/disputes/{disputeId}/status", new { status = "Approved", comment = "Refunded.", rowVersion });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("detail").GetString().Should().EndWith("is Submitted and can only be resolved while it is UnderReview.");
    }

    [Test]
    public async Task Patch_Status_Given_A_Resolution_Without_A_Comment_Should_Return_Validation_Problem()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var disputeId = await CreateDispute();
        var rowVersion = await GetRowVersion(disputeId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await agentClient.PatchAsJsonAsync($"/api/v1/agent/disputes/{disputeId}/status", new { status = "Rejected", rowVersion });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("fieldErrors")[0].GetProperty("field").GetString().Should().Be("Comment");
    }

    [Test]
    public async Task Patch_Status_Given_An_Unknown_Dispute_Should_Return_Not_Found()
    {
        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await agentClient.PatchAsJsonAsync($"/api/v1/agent/disputes/{Guid.NewGuid()}/status", new { status = "UnderReview", rowVersion = "" });

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Get_Dispute_By_Id_Given_An_Agent_Should_Return_The_Customer_And_Timeline()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var disputeId = await CreateDispute();

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await agentClient.GetAsync($"/api/v1/agent/disputes/{disputeId}");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("customerName").GetString().Should().Be("Agent Test Customer");
        body.RootElement.GetProperty("transaction").GetProperty("accountNumber").GetString().Should().Be("******4444");
        body.RootElement.GetProperty("timeline").GetArrayLength().Should().Be(1);
        body.RootElement.TryGetProperty("rowVersion", out _).Should().BeTrue();
    }

    private async Task<Guid> CreateDispute()
    {
        var transaction = new Transaction
        {
            AccountId = customer.Accounts[0].Id,
            Reference = $"TXN-{Guid.NewGuid():N}"[..20],
            Amount = 199.99m,
            Currency = "ZAR",
            Merchant = "Woolworths Food",
            Category = TransactionCategory.Groceries,
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            Status = TransactionStatus.Posted
        };
        await factory.SeedAsync(transaction);

        using var response = await customerClient.PostAsJsonAsync("/api/v1/disputes", new { transactionId = transaction.Id, reason = "DuplicateCharge" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var body = await ReadJson(response);

        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<string> GetRowVersion(Guid disputeId)
    {
        using var response = await agentClient.GetAsync($"/api/v1/agent/disputes/{disputeId}");
        using var body = await ReadJson(response);

        return body.RootElement.GetProperty("rowVersion").GetString()!;
    }

    private async Task<JsonElement> GetCustomerDispute(Guid disputeId)
    {
        using var response = await customerClient.GetAsync($"/api/v1/disputes/{disputeId}");
        using var body = await ReadJson(response);

        return body.RootElement.Clone();
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
