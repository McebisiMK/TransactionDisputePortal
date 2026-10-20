using AutoMapper;
using TransactionDisputePortal.Application.Common.Mappings;

namespace TransactionDisputePortal.UnitTests.Common;

[TestFixture]
public class MappingProfileTests
{
    [Test]
    public void MappingProfile_Given_All_Mappings_In_The_Application_Assembly_Should_Be_Valid()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var configuration = new MapperConfiguration(config => config.AddProfile<MappingProfile>());

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => configuration.AssertConfigurationIsValid();

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().NotThrow();
    }
}
