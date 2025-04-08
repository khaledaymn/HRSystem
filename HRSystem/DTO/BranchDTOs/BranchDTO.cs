namespace HRSystem.DTO.BranchDTOs
{
    public class BranchDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
