namespace HRSystem.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class RoleCreateDTO
    {
        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters.")]
        public string RoleName { get; set; }
    }

}
