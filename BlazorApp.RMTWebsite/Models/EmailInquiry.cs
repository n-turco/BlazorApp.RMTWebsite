using Microsoft.AspNetCore.Identity.UI.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace BlazorApp.RMTWebsite.Models
{
    public class EmailInquiry : IEmailSender
    {
        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Must be a valid email address.")]  
        public string EmailAddress { get; set; } = string.Empty;
        [Required(ErrorMessage = "Subject line is required.")]
        [StringLength(30, ErrorMessage = "Subject line is too long.")]
        public string EmailSubject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(1000, ErrorMessage = "Message is too long, must be less than 1000 characters.")]
        public string EmailContent { get; set; } = string.Empty;
        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(30, ErrorMessage = "First name is too long.")]
        public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(30, ErrorMessage = "Last name is too long.")]
        public string LastName { get; set; } = string.Empty;

        // Implement the interface method
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Minimal implementation to satisfy the interface.
            // Replace with real sending logic as needed.
            return Task.CompletedTask;
        }
    }
}
