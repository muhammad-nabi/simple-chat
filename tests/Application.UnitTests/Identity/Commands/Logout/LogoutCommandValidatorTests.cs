using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Identity.Commands.Logout;

namespace SimpleChat.Application.UnitTests.Identity.Commands.Logout;

public class LogoutCommandValidatorTests
{
    private LogoutCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new LogoutCommandValidator();
    }

    [Test]
    public async Task ShouldPassValidation_WhenRefreshTokenProvided()
    {
        var command = new LogoutCommand("valid-refresh-token-base64");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShouldFailValidation_WhenRefreshTokenEmpty()
    {
        var command = new LogoutCommand("");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
