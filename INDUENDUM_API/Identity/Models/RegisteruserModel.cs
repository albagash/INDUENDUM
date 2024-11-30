using System.ComponentModel.DataAnnotations;

namespace INDUENDUM_API.Identity.Models
{
    public class RegisteruserModel
    {
        [Required(ErrorMessage = "Email-i është i detyrueshëm.")]
        [EmailAddress(ErrorMessage = "Ju lutemi vendosni një email të vlefshëm.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fjalëkalimi është i detyrueshëm.")]
        [MinLength(6, ErrorMessage = "Fjalëkalimi duhet të ketë të paktën 6 karaktere.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Emri i plotë është i detyrueshëm.")]
        public string FullName { get; set; } = string.Empty;

        public string? Role { get; set; } // Roli opsional (default = Consumer)
    }
}
