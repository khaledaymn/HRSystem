using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRSystem.DTO.AuthenticationDTOs
{
    public class ResetPasswordDTO
    {
        [Required(ErrorMessage = "Passwird is Required")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Email is Required")]
        [EmailAddress(ErrorMessage = "Email not in correct formate")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Token is Required")]
        public string Token { get; set; }
    }
}
