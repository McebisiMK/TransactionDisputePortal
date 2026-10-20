using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Common.Validators;

namespace TransactionDisputePortal.UnitTests.Common;

[TestFixture]
public class PagedQueryValidatorTests
{
    private sealed record TestPagedQuery : PagedQuery;

    [Test]
    public void Validate_Given_Default_Paging_Should_Be_Valid()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new TestPagedQuery();
        var pagedQueryValidator = CreatePagedQueryValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = pagedQueryValidator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeTrue();
    }

    [TestCase(1)]
    [TestCase(PagedQuery.MaxPageSize)]
    public void Validate_Given_Page_Size_Within_Cap_Should_Be_Valid(int pageSize)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new TestPagedQuery { PageSize = pageSize };
        var pagedQueryValidator = CreatePagedQueryValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = pagedQueryValidator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(PagedQuery.MaxPageSize + 1)]
    public void Validate_Given_Page_Size_Outside_Cap_Should_Return_Page_Size_Error(int pageSize)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new TestPagedQuery { PageSize = pageSize };
        var pagedQueryValidator = CreatePagedQueryValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = pagedQueryValidator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(PagedQuery.PageSize));
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void Validate_Given_Page_Number_Below_One_Should_Return_Page_Number_Error(int pageNumber)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var query = new TestPagedQuery { PageNumber = pageNumber };
        var pagedQueryValidator = CreatePagedQueryValidator();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = pagedQueryValidator.Validate(query);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(PagedQuery.PageNumber));
    }

    private static PagedQueryValidator CreatePagedQueryValidator()
    {
        return new PagedQueryValidator();
    }
}
