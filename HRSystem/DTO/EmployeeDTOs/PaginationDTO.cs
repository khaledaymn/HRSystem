namespace HRSystem.DTO.EmployeeDTOs
{
    public class PaginationDTO
    {
        public int PageNumber { get; set; } = 1; // Default to 1
        public int PageSize { get; set; } = 100; // Default to 10

        // Fields for filtration (optional filters)
        public string? Name { get; set; } // Filter by Name
        public string? Email { get; set; } // Filter by Email
        public string? PhoneNumber { get; set; } // Filter by PhoneNumber
        public string? NationalId { get; set; } // Filter by NationalId
        public string? Gender { get; set; } // Filter by Gender
        public DateTime? HiringDate { get; set; } // Filter by HiringDate
        public DateTime? DateOfBarth { get; set; } // Filter by DateOfBarth
        public double? Salary { get; set; } // Filter by Salary (partial match or range could be added)
                                             // Add more fields as needed
    }
}
