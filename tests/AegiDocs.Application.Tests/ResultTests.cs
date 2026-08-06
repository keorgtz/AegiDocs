namespace AegiDocs.Application.Tests;

public sealed class ResultTests
{
    public static TheoryData<ApplicationErrorCode> ErrorCodes =>
    [
        ApplicationErrorCode.Validation,
        ApplicationErrorCode.InputOutput,
        ApplicationErrorCode.Capture,
        ApplicationErrorCode.Permission,
        ApplicationErrorCode.Cancelled,
        ApplicationErrorCode.Unexpected,
    ];

    [Fact]
    public void SuccessCreatesSuccessfulResultWithoutError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureCreatesFailedResultWithError()
    {
        var error = CreateError(ApplicationErrorCode.Validation);

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void GenericSuccessCreatesSuccessfulResultWithValue()
    {
        var result = Result<string>.Success("Tutorial created");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("Tutorial created", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GenericFailureCreatesFailedResultWithoutValue()
    {
        var error = CreateError(ApplicationErrorCode.Capture);

        var result = Result<string>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Same(error, result.Error);
    }

    [Theory]
    [MemberData(nameof(ErrorCodes))]
    public void ApplicationErrorPreservesEverySupportedErrorCode(ApplicationErrorCode code)
    {
        var error = new ApplicationError(code, "No fue posible completar la operación.", "A safe detail.");

        Assert.Equal(code, error.Code);
        Assert.Equal("No fue posible completar la operación.", error.UserMessage);
        Assert.Equal("A safe detail.", error.TechnicalDetail);
    }

    [Fact]
    public void ApplicationErrorAllowsOmittingTechnicalDetail()
    {
        var error = new ApplicationError(ApplicationErrorCode.Permission, "Se requiere autorización.");

        Assert.Null(error.TechnicalDetail);
    }

    [Fact]
    public void FailureFactoriesRejectMissingError()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }

    [Fact]
    public void GenericSuccessRejectsMissingValue()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    [Fact]
    public void ApplicationErrorRejectsMissingUserMessage()
    {
        Assert.Throws<ArgumentException>(() => new ApplicationError(ApplicationErrorCode.Validation, " "));
    }

    [Fact]
    public void ApplicationErrorRejectsUndefinedErrorCode()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ApplicationError((ApplicationErrorCode)999, "No fue posible completar la operación."));
    }

    private static ApplicationError CreateError(ApplicationErrorCode code) =>
        new(code, "No fue posible completar la operación.");
}
