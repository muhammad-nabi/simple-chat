using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Identity.Commands.RefreshToken;

namespace SimpleChat.Application.UnitTests.Identity.Commands.RefreshToken;

public class RefreshTokenCommandValidatorTests
{
    private RefreshTokenCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new RefreshTokenCommandValidator();
    }

    [Test]
    public async Task ShouldPassValidation_WhenRefreshTokenProvided()
    {
        var command = new RefreshTokenCommand("valid-refresh-token-base64");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShouldFailValidation_WhenRefreshTokenEmpty()
    {
        var command = new RefreshTokenCommand("");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
