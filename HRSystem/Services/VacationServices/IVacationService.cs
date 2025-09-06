using HRSystem.DTO.AttendanceDTOs;
using HRSystem.DTO.EmployeeDTOs;
using HRSystem.Models;

namespace HRSystem.Services.VacationServices
{
    public interface IVacationService
    {
        Task AddVacationOrAbsence(string employeeId, DateTime date, double shiftHours);
        Task<EmployeeVacationsDTO> GetEmployeeVacations(string employeeId, ParamDTO dto);
        Task<List<EmployeeVacationsDTO>> GetAllEmployeesVacations(ParamDTO dto);
    }
}
