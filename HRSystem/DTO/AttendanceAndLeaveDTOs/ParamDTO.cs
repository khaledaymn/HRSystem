namespace HRSystem.DTO.AttendanceDTOs
{
    public class ParamDTO
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100; 
        public int? Year { get; set; }          
        public int? Month { get; set; }         
    }
}
