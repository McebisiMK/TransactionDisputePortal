using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactions;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.UnitTests.Features.Transactions;

[TestFixture]
public class GetTransactionsQueryValidatorTests
{
    [Test]
    public void Validate_Given_All_Filters_Are_Valid_Should_Be_Valid()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new GetTransactionsQuery
        {
            FromDate = new DateOnly(2026, 9, 1),
            ToDate = new DateOnly(2026, 9, 1),
            Search = "Takealot",
            Status = TransactionStatus.Posted,
            AccountId = Guid.NewGuid()
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
        var query = new GetTransactionsQuery { FromDate = new DateOnly(2026, 9, 2), ToDate = new DateOnly(2026, 9, 1) };
        var validator = CreateValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = validator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetTransactionsQuery.ToDate));
    }

    [Test]
    public void Validate_Given_A_Search_Term_Over_The_Maximum_Length_Should_Return_Search_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new GetTransactionsQuery { Search = new string('a', GetTransactionsQuery.SearchMaxLength + 1) };
        var validator = CreateValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = validator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetTransactionsQuery.Search));
    }

    [Test]
    public void Validate_Given_An_Undefined_Status_Should_Return_Status_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new GetTransactionsQuery { Status = (TransactionStatus)99 };
        var validator = CreateValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = validator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetTransactionsQuery.Status));
    }

    [Test]
    public void Validate_Given_A_Page_Size_Over_The_Cap_Should_Return_Page_Size_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new GetTransactionsQuery { PageSize = PagedQuery.MaxPageSize + 1 };
        var validator = CreateValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = validator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(PagedQuery.PageSize));
    }

    private static GetTransactionsQueryValidator CreateValidator()
    {
        return new GetTransactionsQueryValidator();
    }
}
