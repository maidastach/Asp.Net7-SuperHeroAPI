using System.ComponentModel.DataAnnotations;

namespace SuperHeroAuth.Models
{
    public class SuperHero
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Place { get; set; }
    }
}
