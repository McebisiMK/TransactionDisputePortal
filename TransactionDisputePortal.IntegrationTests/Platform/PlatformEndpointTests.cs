using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.IntegrationTests.TestSupport;

namespace TransactionDisputePortal.IntegrationTests.Platform;

[TestFixture]
public class PlatformEndpointTests
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const int WritePermitLimit = 2;

    private ApiFactory factory = null!;
    private Customer customer = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        factory = new ApiFactory(WritePermitLimit);

        customer = new Customer { ExternalUserId = Guid.NewGuid(), FullName = "Platform Customer", Email = "platform.customer@disputeportal.local" };
        customer.Accounts.Add(new Account { Customer = customer, AccountNumber = "4111222233335555", Type = AccountType.Cheque });
        await factory.SeedAsync(customer);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        factory.Dispose();
    }

    [Test]
    public async Task Get_Liveness_Should_Return_Healthy_Without_A_Token()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClient();

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync("/health/live");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Test]
    public async Task Get_Readiness_Given_Keycloak_Is_Unreachable_Should_Return_Service_Unavailable_With_Each_Check()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClient();

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync("/health");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        using var body = await ReadJson(actual);
        var checks = body.RootElement.GetProperty("checks").EnumerateArray()
            .ToDictionary(check => check.GetProperty("name").GetString()!, check => check.GetProperty("status").GetString());

        body.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        checks.Should().Contain("database", "Healthy").And.Contain("keycloak", "Unhealthy");
    }

    [Test]
    public async Task Get_Api_Should_Return_Security_Headers()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync("/api/v1/transactions");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.OK);
        actual.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        actual.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        actual.Headers.GetValues("Referrer-Policy").Should().Equal("no-referrer");
        actual.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'none'");
        actual.Headers.CacheControl!.NoStore.Should().BeTrue();
        actual.Headers.Contains("Server").Should().BeFalse();
    }

    [Test]
    public async Task Get_Api_Given_A_Correlation_Id_Should_Echo_It_Back()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/transactions");
        request.Headers.Add(CorrelationIdHeader, "checkout-1234");

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.SendAsync(request);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Headers.GetValues(CorrelationIdHeader).Should().Equal("checkout-1234");
    }

    [TestCase(null)]
    [TestCase("not a valid id <script>")]
    public async Task Get_Api_Given_No_Or_An_Invalid_Correlation_Id_Should_Generate_One(string? incoming)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/transactions");
        if (incoming is not null)
            request.Headers.TryAddWithoutValidation(CorrelationIdHeader, incoming);

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.SendAsync(request);

        //------------------------------------- Assert  ------------------------------------------------------
        var correlationId = actual.Headers.GetValues(CorrelationIdHeader).Single();
        correlationId.Should().NotBeNullOrWhiteSpace().And.NotBe(incoming).And.MatchRegex("^[a-zA-Z0-9]+$");
    }

    [Test]
    public async Task Get_Api_Given_An_Error_Should_Include_The_Correlation_Id_In_The_Problem()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(customer.ExternalUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/transactions/{Guid.NewGuid()}");
        request.Headers.Add(CorrelationIdHeader, "trace-me");

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.SendAsync(request);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var body = await ReadJson(actual);
        body.RootElement.GetProperty("correlationId").GetString().Should().Be("trace-me");
    }

    [Test]
    public async Task Post_Dispute_Given_Too_Many_Writes_In_The_Window_Should_Return_Too_Many_Requests()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(Guid.NewGuid());
        var payload = new { transactionId = Guid.NewGuid(), reason = "DuplicateCharge" };

        for (var attempt = 0; attempt < WritePermitLimit; attempt++)
        {
            using var allowed = await client.PostAsJsonAsync("/api/v1/disputes", payload);
            allowed.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.PostAsJsonAsync("/api/v1/disputes", payload);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        actual.Headers.RetryAfter.Should().NotBeNull();
        actual.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
}
