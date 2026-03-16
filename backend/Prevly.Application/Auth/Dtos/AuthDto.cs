using System.ComponentModel.DataAnnotations;

namespace Prevly.Application.Auth.Dtos
{
    public class AuthDto
    {
        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
