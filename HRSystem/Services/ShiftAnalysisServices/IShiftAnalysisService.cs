using HRSystem.Models;

namespace HRSystem.Services.ShiftAnalysisServices
{
    public interface IShiftAnalysisService
    {
        Task AnalyzePreviousShiftForEmployees(DateTime shiftStartTime);
        Task<Shift> GetPreviousShift(string employeeId, DateTime currentShiftStartTime);
    }
}
