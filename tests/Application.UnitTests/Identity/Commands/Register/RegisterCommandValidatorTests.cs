using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Identity.Commands.Register;

namespace SimpleChat.Application.UnitTests.Identity.Commands.Register;

public class RegisterCommandValidatorTests
{
    private RegisterCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new RegisterCommandValidator();
    }

    [Test]
    public async Task ShouldPassValidation_WhenAllFieldsValid()
    {
        var command = new RegisterCommand("Jane Smith", "jane@example.com", "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShouldFailValidation_WhenDisplayNameEmpty()
    {
        var command = new RegisterCommand("", "jane@example.com", "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name is required.");
    }

    [Test]
    public async Task ShouldFailValidation_WhenDisplayNameExceedsMaxLength()
    {
        var command = new RegisterCommand(new string('a', 257), "jane@example.com", "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Test]
    public async Task ShouldFailValidation_WhenEmailEmpty()
    {
        var command = new RegisterCommand("Jane", "", "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Test]
    public async Task ShouldFailValidation_WhenEmailInvalid()
    {
        var command = new RegisterCommand("Jane", "not-an-email", "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Please enter a valid email.");
    }

    [Test]
    public async Task ShouldFailValidation_WhenEmailExceedsMaxLength()
    {
        var email = new string('a', 251) + "@b.c";
        var command = new RegisterCommand("Jane", email, "password123");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public async Task ShouldFailValidation_WhenPasswordEmpty()
    {
        var command = new RegisterCommand("Jane", "jane@example.com", "");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Test]
    public async Task ShouldFailValidation_WhenPasswordTooShort()
    {
        var command = new RegisterCommand("Jane", "jane@example.com", "1234567");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Test]
    public async Task ShouldPassValidation_WhenPasswordExactly8Characters()
    {
        var command = new RegisterCommand("Jane", "jane@example.com", "12345678");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
