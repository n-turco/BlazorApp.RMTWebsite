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

    // Worked example: follow this pattern for the tests listed below.
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

    // TODO (your turn): add these tests using the pattern above.
    //  1. Validate_MissingEmailAddress_HasEmailRequiredError
    //       Set EmailAddress = "" and assert the error message is "Email Address is required."
    //       Tip: Assert.Single(errors) returns the only error, so you can check its ErrorMessage.
    //  2. Validate_InvalidEmailAddress_HasEmailFormatError
    //       Try "not-an-email". Bonus: turn it into a [Theory] with a few bad addresses.
    //  3. Validate_MissingSubject_HasSubjectRequiredError
    //  4. Validate_SubjectOver30Characters_HasSubjectTooLongError
    //       new string('a', 31) makes a 31-character string.
    //  5. Validate_SubjectExactly30Characters_HasNoErrors
    //       A boundary test: limits are where off-by-one bugs hide.
    //  6. Validate_MissingMessage_HasMessageRequiredError
    //  7. Validate_MessageOver1000Characters_HasMessageTooLongError
    //  8. Validate_MessageExactly1000Characters_HasNoErrors
}
