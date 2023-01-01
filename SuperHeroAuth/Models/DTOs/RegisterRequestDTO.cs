using System.ComponentModel.DataAnnotations;

namespace SuperHeroAuth.Models.DTOs
{
    public class RegisterRequestDTO
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public bool IsAdmin { get; set; }
    }
}
