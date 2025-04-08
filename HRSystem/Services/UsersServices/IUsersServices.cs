using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRSystem.DTO.AuthenticationDTOs;
using HRSystem.DTO.UserDTOs;

namespace HRSystem.Services.UsersServices
{
    public interface IUsersServices
    {
        Task<List<AuthenticationDTO>> GetAllAsync();
        Task<AuthenticationDTO> GetByID(string id);
        Task<bool> Edit(UpdateUserDTO model);
        Task<bool> Delete(string id);
        Task<AuthenticationDTO> Create(CreateUserDTO model);
        //TODO: Task<List<AuthenticationDTO>> GetEmployeesByBranchId(int id);
        //TODo: Task<List<EmployeesWithSalaryDTO>> GetEmployeesSalaries();
        //TODO: Task<SalaryDetailsDTO> GetEmployeeSalaryDetails(int EmployeeId);
        //ToDo: Task<EmployeeVacationsDTO> GetEmployeeVacations(int EmployeeId);
    }
}
