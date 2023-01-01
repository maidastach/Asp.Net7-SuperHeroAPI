using System.ComponentModel.DataAnnotations;

namespace SuperHeroAuth.Models.DTOs
{
    public class LoginRequestDTO
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
