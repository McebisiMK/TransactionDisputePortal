using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionDisputePortal.Application.Common.Behaviors;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Validators;
using ValidationException = TransactionDisputePortal.Application.Common.Exceptions.ValidationException;

namespace TransactionDisputePortal.UnitTests.Common;

[TestFixture]
public class ValidationBehaviorTests
{
    public sealed record TestRequest(string Name, bool Exists);

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Name)
                .NotEmpty();

            RuleFor(request => request.Exists)
                .Equal(true)
                .WithErrorCode(ValidationErrorCodes.NotFound)
                .WithMessage("Entity \"Test\" (1) was not found.");
        }
    }

    [Test]
    public async Task Handle_Given_A_Valid_Request_Should_Call_The_Handler()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var validationBehavior = CreateValidationBehavior();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validationBehavior.Handle(new TestRequest("name", Exists: true), _ => Task.FromResult("handled"), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().Be("handled");
    }

    [Test]
    public async Task Handle_Given_A_Validation_Failure_Should_Throw_Validation_Exception()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var validationBehavior = CreateValidationBehavior();

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => validationBehavior.Handle(new TestRequest("", Exists: true), _ => Task.FromResult("handled"), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainKey(nameof(TestRequest.Name));
    }

    [Test]
    public async Task Handle_Given_A_Not_Found_Failure_Should_Throw_Not_Found_Exception_Even_With_Other_Failures()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var validationBehavior = CreateValidationBehavior();

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => validationBehavior.Handle(new TestRequest("", Exists: false), _ => Task.FromResult("handled"), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Entity \"Test\" (1) was not found.");
    }

    private static ValidationBehavior<TestRequest, string> CreateValidationBehavior()
    {
        return new ValidationBehavior<TestRequest, string>([new TestRequestValidator()], NullLogger<ValidationBehavior<TestRequest, string>>.Instance);
    }
}
