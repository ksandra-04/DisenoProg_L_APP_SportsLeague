using SportsLeague.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SportsLeague.API.DTOs.Request
{
    public class SponsorRequestDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [MaxLength(200)]
        public string ContactEmail { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? WebsiteUrl { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        public SponsorCategory Category { get; set; }
    }
}