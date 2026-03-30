using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Identity.Commands.Login;

namespace SimpleChat.Application.UnitTests.Identity.Commands.Login;

public class LoginCommandValidatorTests
{
    private LoginCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new LoginCommandValidator();
    }

    [Test]
    public async Task ShouldPassValidation_WhenAllFieldsValid()
    {
        var command = new LoginCommand("jane@example.com", "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShouldFailValidation_WhenEmailEmpty()
    {
        var command = new LoginCommand("", "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Test]
    public async Task ShouldFailValidation_WhenEmailInvalid()
    {
        var command = new LoginCommand("not-an-email", "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Please enter a valid email.");
    }

    [Test]
    public async Task ShouldFailValidation_WhenPasswordEmpty()
    {
        var command = new LoginCommand("jane@example.com", "");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Test]
    public async Task ShouldPassValidation_WhenPasswordShort()
    {
        // Login validator should NOT enforce password policy (avoids leaking password requirements)
        var command = new LoginCommand("jane@example.com", "short");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
