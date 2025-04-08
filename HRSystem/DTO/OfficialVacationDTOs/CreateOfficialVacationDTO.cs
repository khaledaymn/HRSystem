using System.ComponentModel.DataAnnotations;
namespace HRSystem.DTO.OfficialVacationDTOs
{
    public class CreateOfficialVacationDTO
    {
        [Required(ErrorMessage = "Vacation name is required")]
        public string VacationName { get; set; } = default!;
        [Required(ErrorMessage = "Vacation day is required")]
        public string VacationDay { get; set; }
    }
}
