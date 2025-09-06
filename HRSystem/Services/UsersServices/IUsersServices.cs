using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRSystem.DTO.AuthenticationDTOs;
using HRSystem.DTO.UserDTOs;
using HRSystem.DTO.EmployeeDTOs;
using HRSystem.DTO.AttendanceDTOs;

namespace HRSystem.Services.UsersServices
{
    public interface IUsersServices
    {
        Task<List<AuthenticationDTO>> GetAllAsync(PaginationDTO pagination);
        Task<AuthenticationDTO> GetByID(string id);
        Task<bool> Edit(UpdateUserDTO model);
        Task<bool> Delete(string id);
        Task<AuthenticationDTO> Create(CreateUserDTO model);
        Task<List<EmployeesWithSalaryDTO>> GetEmployeesSalaries(ParamDTO dto);
        //Task<SalaryDetailsDTO> GetEmployeeSalaryDetails(string employeeId, DateTime? startDate = null, DateTime? endDate = null);
        Task<SalaryDetailsDTO> GetEmployeeSalaryDetails(string employeeId, int? month = null, int? year = null);
        Task CalculateSalary(DateTime? calculationDate = null);
        Task<List<AuthenticationDTO>> GetEmployeesByBranchId(int id);
        Task<string> UpdateNetSalary(UpdateSalaryDTO dto);
        Task<string> UpdateWorkStatisticsAsync(UpdateWorkStatisticsDTO dto);
        Task<string> UpdateSalesPresentage(UpdateSalesPresentageDTO dto);
    }
}
