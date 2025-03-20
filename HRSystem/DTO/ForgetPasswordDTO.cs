using System.ComponentModel.DataAnnotations;

namespace HRSystem.DTO
{
    public class ForgetPasswordDTO
    {
        [Required(ErrorMessage = "This Field Required")]
        [EmailAddress(ErrorMessage = "Invalid Mail")]
        public string Email { get; set; }
    }
}
