using HRSystem.DataBase;
using HRSystem.Models;
using HRSystem.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRSystem.Extend;
using System.Globalization;
using HRSystem.DTO.ShiftDTOs;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRSystem.Services.ShiftServices
{
    public class ShiftServices : IShiftServices
    {
        private readonly ILogger<ShiftServices> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ShiftServices(ILogger<ShiftServices> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        #region Create Shift

        public async Task<bool> CreateShiftAsync(AddShiftDTO shiftDto)
        {
            bool ownsTransaction = false;
            IDbContextTransaction? transaction = null;

            try
            {
                if (shiftDto == null || string.IsNullOrEmpty(shiftDto.EmployeeId))
                {
                    _logger.LogWarning("Invalid shift data: Shift DTO is null or EmployeeId is empty.");
                    return false;
                }

                if (_unitOfWork.GetDbContext().Database.CurrentTransaction == null)
                {
                    transaction = await _unitOfWork.BeginTransactionAsync();
                    ownsTransaction = true;
                }

                var employee = await _unitOfWork.UsersServices.GetByID(shiftDto.EmployeeId);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found.", shiftDto.EmployeeId);
                    if (ownsTransaction)
                    {
                        await _unitOfWork.RollbackAsync();
                    }
                    return false;
                }

                var newShift = new Shift
                {
                    StartTime = DateTime.ParseExact(shiftDto.StartTime, "H:mm", CultureInfo.InvariantCulture),
                    EndTime = DateTime.ParseExact(shiftDto.EndTime, "H:mm", CultureInfo.InvariantCulture)
                };

                _logger.LogDebug("Adding new shift: StartTime={StartTime}, EndTime={EndTime}",
                    newShift.StartTime, newShift.EndTime);
                await _unitOfWork.Repository<Shift>().ADD(newShift);
                await _unitOfWork.Save();

                var employeeShift = new EmployeeShift
                {
                    EmployeeId = shiftDto.EmployeeId,
                    ShiftId = newShift.Id
                };

                _logger.LogDebug("Assigning shift to employee: EmployeeId={EmployeeId}, ShiftId={ShiftId}",
                    employeeShift.EmployeeId, employeeShift.ShiftId);
                await _unitOfWork.Repository<EmployeeShift>().ADD(employeeShift);

                await _unitOfWork.Save();

                if (ownsTransaction)
                {
                    await _unitOfWork.CommitAsync();
                }

                _logger.LogInformation("Shift assigned successfully to EmployeeId: {EmployeeId}, ShiftId: {ShiftId}",
                    employeeShift.EmployeeId, newShift.Id);
                return true;
            }
            catch (Exception ex)
            {
                if (ownsTransaction)
                {
                    await _unitOfWork.RollbackAsync();
                }
                _logger.LogError(ex, "Failed to create shift assignment for EmployeeId: {EmployeeId}. Error: {Message}",
                    shiftDto?.EmployeeId, ex.Message);
                throw; 
            }
            finally
            {
                if (ownsTransaction && transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        #endregion


        #region Update Shift

        public async Task<bool> UpdateShiftAsync(ShiftDTO shiftDto)
        {

            try
            {
                if (shiftDto == null || string.IsNullOrEmpty(shiftDto.EmployeeId))
                {
                    _logger.LogWarning("Invalid shift data: Shift DTO is null or EmployeeId is empty.");
                    return false;
                }

                var existingShift = await _unitOfWork.Repository<Shift>().GetById(shiftDto.Id);
                if (existingShift == null)
                {
                    _logger.LogWarning("Shift with ID {ShiftId} not found.", shiftDto.Id);
                    return false;
                }

                if (!string.IsNullOrEmpty(shiftDto.StartTime))
                    existingShift.StartTime = DateTime.ParseExact(shiftDto.StartTime, "H:mm", CultureInfo.InvariantCulture);

                if (!string.IsNullOrEmpty(shiftDto.EndTime))
                    existingShift.EndTime = DateTime.ParseExact(shiftDto.EndTime, "H:mm", CultureInfo.InvariantCulture);

                if ((!string.IsNullOrEmpty(shiftDto.StartTime) || !string.IsNullOrEmpty(shiftDto.EndTime)) &&
                    existingShift.StartTime >= existingShift.EndTime)
                {
                    _logger.LogWarning("Invalid time range after update: StartTime {StartTime} is not before EndTime {EndTime}.",
                        existingShift.StartTime, existingShift.EndTime);
                    return false;
                }

                _unitOfWork.Repository<Shift>().Update(existingShift);
                await _unitOfWork.Save();

                _logger.LogInformation("Shift updated successfully: ShiftId={ShiftId}", shiftDto.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update shift with ID: {ShiftId}. Error: {Message}", shiftDto?.Id, ex.Message);
                return false;
            }
        }

        #endregion


        #region Delete Shift
        public async Task<bool> DeleteShiftAsync(DeleteShiftDTO dto)
        {
            _logger.LogInformation("Attempting to delete shift with ID: {ShiftId} for EmployeeId: {EmployeeId}",
                dto?.ShiftId, dto?.EmployeeId);

            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.EmployeeId))
                {
                    _logger.LogWarning("Invalid shift data: DeleteShiftDTO is null or EmployeeId is empty.");
                    return false;
                }

                var existingShift = await _unitOfWork.Repository<Shift>().GetById(dto.ShiftId);
                if (existingShift == null)
                {
                    _logger.LogWarning("Shift with ID {ShiftId} not found.", dto.ShiftId);
                    return false;
                }

                var employeeShift = await _unitOfWork.Repository<EmployeeShift>()
                    .GetAll()
                    .ContinueWith(t => t.Result.FirstOrDefault(es => es.EmployeeId == dto.EmployeeId && es.ShiftId == dto.ShiftId));
                if (employeeShift == null)
                {
                    _logger.LogWarning("EmployeeShift with EmployeeId {EmployeeId} and ShiftId {ShiftId} not found.",
                        dto.EmployeeId, dto.ShiftId);
                    return false;
                }

                await _unitOfWork.BeginTransactionAsync();

                _unitOfWork.Repository<Shift>().Delete(existingShift.Id);
                _unitOfWork.Repository<EmployeeShift>().Delete(employeeShift.Id);

                await _unitOfWork.Save();
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Shift deleted successfully for EmployeeId: {EmployeeId}, ShiftId: {ShiftId}",
                    dto.EmployeeId, dto.ShiftId);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Failed to delete shift assignment for EmployeeId: {EmployeeId}. Error: {Message}",
                    dto?.EmployeeId, ex.Message);
                return false;
            }
        }
        #endregion


        #region Get Shift By EmployeeId

        public async Task<List<ShiftDTO>> GetByEmployeeId(string employeeId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                {
                    _logger.LogWarning("Invalid employee ID provided: null or empty.");
                    return new List<ShiftDTO>();
                }

                var employeeShifts = _unitOfWork.Repository<EmployeeShift>()
                    .Filter(es => es.EmployeeId == employeeId);

                var shifts = employeeShifts
                    .Select(es => new ShiftDTO
                    {
                        Id = es.Shift.Id,
                        StartTime = es.Shift.StartTime.ToString("H:mm", CultureInfo.InvariantCulture),
                        EndTime = es.Shift.EndTime.ToString("H:mm", CultureInfo.InvariantCulture),
                        EmployeeId = es.EmployeeId
                    })
                    .ToList();

                if (!shifts.Any())
                {
                    _logger.LogInformation("No shifts found for employee with ID: {EmployeeId}", employeeId);
                }
                else
                {
                    _logger.LogInformation("Found {ShiftCount} shifts for employee with ID: {EmployeeId}", shifts.Count, employeeId);
                }

                return shifts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve shifts for employee with ID: {EmployeeId}. Error: {Message}", employeeId, ex.Message);
                return new List<ShiftDTO>();
            }
        }

        #endregion


        #region Get Shift By Id

        public async Task<ShiftDTO?> GetShiftByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid shift ID provided: {ShiftId}. ID must be greater than zero.", id);
                    return null;
                }

                var shift = await _unitOfWork.Repository<Shift>().GetById(id);
                if (shift == null)
                {
                    _logger.LogWarning("Shift with ID {ShiftId} not found.", id);
                    return null;
                }

                var employeeShift = _unitOfWork.Repository<EmployeeShift>()
                    .Filter(es => es.ShiftId == id)
                    .FirstOrDefault();

                string startTime = shift.StartTime.ToString("H:mm", CultureInfo.InvariantCulture);
                string endTime = shift.EndTime.ToString("H:mm", CultureInfo.InvariantCulture);

                _logger.LogInformation("Successfully retrieved shift with ID: {ShiftId}", id);
                return new ShiftDTO
                {
                    Id = shift.Id,
                    StartTime = startTime,
                    EndTime = endTime,
                    EmployeeId = employeeShift?.EmployeeId ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve shift with ID: {ShiftId}. Error: {Message}", id, ex.Message);
                return null;
            }
        }

        #endregion

    }
}