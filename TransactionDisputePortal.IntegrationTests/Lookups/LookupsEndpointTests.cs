using System.Net;
using System.Text.Json;
using TransactionDisputePortal.Application.Common.Security;
using TransactionDisputePortal.IntegrationTests.TestSupport;

namespace TransactionDisputePortal.IntegrationTests.Lookups;

[TestFixture]
public class LookupsEndpointTests
{
    private ApiFactory factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        factory = new ApiFactory();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        factory.Dispose();
    }

    [Test]
    public async Task Get_Lookups_Given_No_Token_Should_Return_Unauthorized()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClient();

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync("/api/v1/lookups");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestCase(Roles.Customer)]
    [TestCase(Roles.Agent)]
    public async Task Get_Lookups_Given_A_Signed_In_User_Should_Return_Enum_Names_As_Ids_With_Display_Names(string role)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var client = factory.CreateClientFor(Guid.NewGuid(), role);
        var expectedTransactionStatuses = new Dictionary<string, string> { ["Pending"] = "Pending", ["Posted"] = "Posted" };

        //------------------------------------- Act     ------------------------------------------------------
        using var actual = await client.GetAsync("/api/v1/lookups");

        //------------------------------------- Assert  ------------------------------------------------------
        actual.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await actual.Content.ReadAsStringAsync());
        var disputeStatuses = ReadLookup(body.RootElement.GetProperty("disputeStatuses"));
        var disputeReasons = ReadLookup(body.RootElement.GetProperty("disputeReasons"));
        var transactionStatuses = ReadLookup(body.RootElement.GetProperty("transactionStatuses"));

        disputeStatuses.Should().HaveCount(5).And.Contain("UnderReview", "Under review");
        disputeReasons.Should().HaveCount(8).And.Contain("GoodsOrServicesNotReceived", "Goods or services not received");
        transactionStatuses.Should().BeEquivalentTo(expectedTransactionStatuses);
    }

    private static Dictionary<string, string> ReadLookup(JsonElement items)
    {
        return items.EnumerateArray().ToDictionary(item => item.GetProperty("id").GetString()!, item => item.GetProperty("name").GetString()!);
    }
}
