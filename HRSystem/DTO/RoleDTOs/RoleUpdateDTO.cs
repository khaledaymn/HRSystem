using System.ComponentModel.DataAnnotations;

namespace HRSystem.DTO.RoleDTOs
{
    public class RoleUpdateDTO
    {
        [Required(ErrorMessage = "Role ID is required.")]
        public string RoleId { get; set; }

        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters.")]
        public string RoleName { get; set; }
    }

}
