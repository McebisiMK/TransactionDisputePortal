using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputes;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class GetDisputesQueryValidatorTests
{
    [Test]
    public void Validate_Given_All_Filters_Are_Valid_Should_Be_Valid()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new GetDisputesQuery
        {
            FromDate = new DateOnly(2026, 9, 1),
            ToDate = new DateOnly(2026, 9, 1),
            Status = DisputeStatus.UnderReview
        };
        var validator = CreateValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = validator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_Given_The_To_Date_Is_Before_The_From_Date_Should_Return_To_Date_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new GetDisputesQuery { FromDate = new DateOnly(2026, 9, 2), ToDate = new DateOnly(2026, 9, 1) };
        var validator = CreateValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = validator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetDisputesQuery.ToDate));
    }

    [Test]
    public void Validate_Given_An_Undefined_Status_Should_Return_Status_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new GetDisputesQuery { Status = (DisputeStatus)99 };
        var validator = CreateValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = validator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetDisputesQuery.Status));
    }

    [Test]
    public void Validate_Given_A_Page_Size_Over_The_Cap_Should_Return_Page_Size_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new GetDisputesQuery { PageSize = PagedQuery.MaxPageSize + 1 };
        var validator = CreateValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = validator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(PagedQuery.PageSize));
    }

    private static GetDisputesQueryValidator CreateValidator()
    {
        return new GetDisputesQueryValidator();
    }
}
