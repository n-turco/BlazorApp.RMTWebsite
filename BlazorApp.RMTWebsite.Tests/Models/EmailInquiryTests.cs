using System.ComponentModel.DataAnnotations;
using BlazorApp.RMTWebsite.Models;

namespace BlazorApp.RMTWebsite.Tests.Models;

/// <summary>
/// Unit tests for the validation rules on <see cref="EmailInquiry"/>. These are the same
/// [Required], [EmailAddress] and [StringLength] rules the contact form's DataAnnotationsValidator
/// runs, so testing them here tests what visitors see, without rendering a page.
/// </summary>
public class EmailInquiryTests
{
    /// <summary>A fully valid inquiry. Each test changes one field to break one rule.</summary>
    private static EmailInquiry CreateValidInquiry() => new()
    {
        EmailAddress = "visitor@example.test",
        EmailSubject = "Booking question",
        EmailContent = "Do you have any Saturday appointments?",
    };

    /// <summary>
    /// Runs every DataAnnotations rule on the model, as Blazor does on submit,
    /// and returns the errors found (empty when the model is valid).
    /// </summary>
    private static List<ValidationResult> Validate(EmailInquiry inquiry)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(inquiry, new ValidationContext(inquiry), results, validateAllProperties: true);
        return results;
    }

    // Test the correct input
    [Fact]
    public void Validate_AllFieldsValid_HasNoErrors()
    {
        // Arrange
        var inquiry = CreateValidInquiry();

        // Act
        var errors = Validate(inquiry);

        // Assert
        Assert.Empty(errors);
    }

    // Test a missing email returns correct error message
    [Fact]
    public void Validate_MissingEmailAddress_HasEmailRequiredError()
    {
        // Arrange
        var inquiry = CreateValidInquiry();
        inquiry.EmailAddress = "";

        // Act
        var errors = Validate(inquiry);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Email Address is required.", error.ErrorMessage);
    }

    // Test for an invalid email address
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.test")]
    [InlineData("visitor@")]
    [InlineData("a@b@example.test")]
    public void Validate_InvalidEmailAddress_HasEmailFormatError(string emailAddress)
    {
        // Arrange
        var inquiry = CreateValidInquiry();
        inquiry.EmailAddress = emailAddress;

        // Act
        var errors = Validate(inquiry);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Must be a valid email address.", error.ErrorMessage);
    }

    // Test for a missing subject title
    [Fact]
    public void Validate_MissingSubject_HasSubjectRequiredError()
    {
        // Arrange
        var inquiry = CreateValidInquiry();
        inquiry.EmailSubject = "";

        // Act
        var errors = Validate(inquiry);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Subject line is required.", error.ErrorMessage);
    }

    // Test for a subject line longer than 30 characters
    [Fact]
    public void Validate_SubjectOver30Characters_HasSubjectTooLongError()
    {
        // Arrange
        var inquiry = CreateValidInquiry();
        inquiry.EmailSubject = new string('a', 31);

        // Act
        var errors = Validate(inquiry);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Subject line is too long.", error.ErrorMessage);
    }

    // Test for exactly 30 character string
    [Fact]
    public void Validate_SubjectExactly30Characters_HasNoErrors()
    {
        // Arrange
        var inquiry = CreateValidInquiry();
        inquiry.EmailSubject = new string('a', 30);

        // Act
        var errors = Validate(inquiry);

        // Assert
        Assert.Empty(errors);
    }

    // Test for empty message content
    [Fact]
    public void Validate_MissingMessage_HasMessageRequiredError()
    {
        // Arrange
        var inquiry = CreateValidInquiry();
        inquiry.EmailContent = "";

        // Act
        var errors = Validate(inquiry);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Message is required.", error.ErrorMessage);
    }

    // Test for too many characters in email content
    [Fact]
    public void Validate_MessageOver1000Characters_HasMessageTooLongError()
    {
        // Arrange
        var inquiry = CreateValidInquiry();
        inquiry.EmailContent = new string('a', 1001);

        // Act
        var errors = Validate(inquiry);

        // Assert
        var error = Assert.Single(errors);
        Assert.Equal("Message is too long, must be less than 1000 characters.", error.ErrorMessage);
    }

    // Test for exactly 1000 characters
    [Fact]
    public void Validate_MessageExactly1000Characters_HasNoErrors()
    {
        // Arrange
        var inquiry = CreateValidInquiry();
        inquiry.EmailContent = new string('a', 1000);

        // Act
        var errors = Validate(inquiry);

        // Assert
        Assert.Empty(errors);
    }
}
