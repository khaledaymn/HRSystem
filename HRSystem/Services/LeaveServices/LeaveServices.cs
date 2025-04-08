using HRSystem.DataBase;
using HRSystem.Models;
using HRSystem.DTO;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HRSystem.Services.LeaveServices
{
    public class LeaveServices : ILeaveServices
    {
        private readonly ApplicationDbContext _context;
        public LeaveServices(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Take Leave

        public async Task<bool> AddLeave(LeaveDTO model)
        {
            //if (model == null)
            //    throw new ArgumentNullException(nameof(model));

            //if (!DateTime.TryParseExact(model.Time, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime leaveTime))
            //    throw new FormatException("Invalid time format. Use hh:mm AM/PM.");

            //var employeeName = await _context.Users
            //    .Where(e => e.Id == model.EmployeeId)
            //    .Select(e => e.Name)
            //    .FirstOrDefaultAsync();

            //if (string.IsNullOrEmpty(employeeName))
            //    throw new InvalidOperationException("Employee not found.");

            ////var generalSettings = await _context.GeneralSetting
            ////    .Select(g => new { g.FirstShiftTimeOfLeave, g.SecondShiftTimeOfLeave })
            ////    .FirstOrDefaultAsync();

            //if (generalSettings == null)
            //    throw new InvalidOperationException("General settings not found.");

            //DateTime firstShiftLeaveTime = generalSettings.FirstShiftTimeOfLeave;
            //DateTime secondShiftLeaveTime = generalSettings.SecondShiftTimeOfLeave;

            //bool isExtraTime = false;
            //string extraTimeText = "";
            //double totalExtraMinutes = 0;

            //if (leaveTime >= firstShiftLeaveTime.AddHours(1) || leaveTime >= secondShiftLeaveTime.AddHours(1))
            //{
            //    DateTime relevantShiftLeaveTime = leaveTime >= firstShiftLeaveTime.AddHours(1) ? firstShiftLeaveTime : secondShiftLeaveTime;
            //    var timeDifference = leaveTime - relevantShiftLeaveTime;

            //    var hours = (int)timeDifference.TotalHours;
            //    var minutes = timeDifference.Minutes;
            //    totalExtraMinutes = timeDifference.TotalMinutes;

            //    extraTimeText = hours > 0 && minutes > 0
            //        ? $"{hours} ساعة و {minutes} دقيقة"
            //        : hours > 0
            //            ? $"{hours} ساعة"
            //            : $"{minutes} دقيقة";

            //    isExtraTime = true;
            //}

            //var leave = new Leave
            //{
            //    TimeOfLeave = leaveTime,
            //    Latitude = model.Latitude,
            //    Longitude = model.Longitude,
            //    EmployeeId = model.EmployeeId,
            //    Branch = model.Branch,
            //};

            try
            {
                //    await _context.Leave.AddAsync(leave);
                //    var result = await _context.SaveChangesAsync();

                //    if (isExtraTime)
                //    {
                //        var notification = new Notification
                //        {
                //            CreatedAt = DateTime.UtcNow,
                //            Title = "طلب ساعات إضافية",
                //            Message = $"الموظف {employeeName} يريد أخذ {extraTimeText} إضافية لأنه قام بالمغادرة الساعة {leaveTime:hh:mm tt}.",
                //            NumberOfExtraHours = totalExtraMinutes
                //        };

                //        await _context.Notifications.AddAsync(notification);
                //        await _context.SaveChangesAsync();
                //    }

                return false;
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException("An error occurred while saving leave data. Please try again.", dbEx);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while processing your request.", ex);
            }
        }



        #endregion


        //#region Get all Leaves

        //public async Task<IEnumerable<LeaveDTO>> GetAllLeaves()
        //{
        //    return await _context.Leave
        //        .Include(e => e.Employee) 
        //        .OrderBy(e => e.Employee.Name)
        //        .AsNoTracking()
        //        .Select(l => new LeaveDTO
        //        {
        //            Time = l.TimeOfLeave,
        //            Latitude = l.Latitude,
        //            Longitude = l.Longitude,
        //            EmployeeId = l.EmployeeId
        //        })
        //        .ToListAsync();
        //}

        //#endregion


        //#region Get Employee Leaves

        //public async Task<IEnumerable<LeaveDTO>> GetEmployeeLeaves(string empId)
        //{
        //    return await _context.Leave
        //        .Where(a => a.EmployeeId == empId)
        //        .AsNoTracking()
        //        .Select(l => new LeaveDTO
        //        {
        //            Time = l.TimeOfLeave,
        //            Latitude = l.Latitude,
        //            Longitude = l.Longitude,
        //            EmployeeId = l.EmployeeId
        //        })
        //        .ToListAsync();
        //}

        //#endregion

    }
}
