namespace HRSystem.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class CreateOfficialVacationDTO
    {
        [Required(ErrorMessage = "Vacation name is required")]
        public string VacationName { get; set; } = default!;
        [Required(ErrorMessage = "Vacation day is required")]
        public DateTime VacationDay { get; set; }
    }
}
