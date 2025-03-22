using HRSystem.DataBase;
using HRSystem.DTO;
using HRSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Services.OfficialVacationServices
{
    public class OfficialVacationServices : IOfficialVacationServices
    {
        private readonly ApplicationDbContext _context;

        public OfficialVacationServices(ApplicationDbContext context) => _context = context;

        #region Create Official Vacation
        public async Task<CreateOfficialVacationDTO> AddOfficialVacationAsync(CreateOfficialVacationDTO vacation)
        {
            if (vacation == null)
                throw new ArgumentNullException(nameof(vacation));

            var entity = new OfficialVacation
            {
                VacationName = vacation.VacationName,
                VacationDay = vacation.VacationDay
            };

            _context.OfficialVacation.Add(entity);
            await _context.SaveChangesAsync();

            return new CreateOfficialVacationDTO
            {
                VacationName = entity.VacationName,
                VacationDay = entity.VacationDay
            };
        }

        

        #endregion


        #region Get All Official Vacations

        public async Task<IEnumerable<OfficialVacationDTO>> GetAllOfficialVacationsAsync()
        {
            var vacations = await _context.OfficialVacation
                .Select(v => new OfficialVacationDTO
                {
                    Id = v.Id,
                    VacationName = v.VacationName,
                    VacationDay = v.VacationDay
                })
                .ToListAsync();

            return vacations;
        }

        #endregion


        #region Get Official Vacation By Id

        public async Task<OfficialVacationDTO?> GetOfficialVacationByIdAsync(int id)
        {
            var vacation = await _context.OfficialVacation
                .Where(v => v.Id == id)
                .Select(v => new OfficialVacationDTO
                {
                    Id = v.Id,
                    VacationName = v.VacationName,
                    VacationDay = v.VacationDay
                })
                .FirstOrDefaultAsync();

            return vacation;
        }

        #endregion


        #region Update Official Vacation

        public async Task<OfficialVacationDTO> UpdateOfficialVacationAsync(int id, OfficialVacationDTO vacation)
        {
            var existingVacation = await _context.OfficialVacation.FindAsync(id);

            if (existingVacation == null)
                throw new KeyNotFoundException($"No vacation found with ID {id}.");

            existingVacation.VacationName = vacation.VacationName;
            existingVacation.VacationDay = vacation.VacationDay;

            await _context.SaveChangesAsync();

            return new OfficialVacationDTO
            {
                Id = existingVacation.Id,
                VacationName = existingVacation.VacationName,
                VacationDay = existingVacation.VacationDay
            };
        }

        #endregion


        #region Delete Official Vacation

        public async Task<bool> DeleteOfficialVacationAsync(int id)
        {
            var existingVacation = await _context.OfficialVacation.FindAsync(id);

            if (existingVacation == null)
                return false; 

            _context.OfficialVacation.Remove(existingVacation);
            await _context.SaveChangesAsync();

            return true; 
        }


        #endregion


        #region Is Official Vacation

        public async Task<bool> IsOfficialVacationAsync(DateTime date)
        {
            return await _context.OfficialVacation
                                 .AnyAsync(v => v.VacationDay.Date == date.Date);
        }

        #endregion

    }
}
