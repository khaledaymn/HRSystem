using System.ComponentModel.DataAnnotations;
namespace HRSystem.DTO.BranchDTOs
{

    public class AddBranchDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public double Radius { get; set; }
    }

}
