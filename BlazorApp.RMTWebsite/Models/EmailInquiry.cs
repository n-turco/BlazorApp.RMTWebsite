using Microsoft.AspNetCore.Identity.UI.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace BlazorApp.RMTWebsite.Models
{
    public class EmailInquiry : IEmailSender
    {
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; } = "";
        [Required]
        public string EmailSubject { get; set; } = "";

        [Required]
        public string EmailContent { get; set; } = "";
        [Required]
        public string FirstName { get; set; } = "";
        [Required]
        public string LastName { get; set; } = "";

        // Implement the interface method
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Minimal implementation to satisfy the interface.
            // Replace with real sending logic as needed.
            return Task.CompletedTask;
        }
    }
}
