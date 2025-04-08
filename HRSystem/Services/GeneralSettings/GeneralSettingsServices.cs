using HRSystem.DataBase;
using HRSystem.DTO.GeneralSettingsDTOs;
using HRSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Services.GeneralSettings
{
    public class GeneralSettingsServices : IGeneralSettingsServices
    {
        private readonly ApplicationDbContext _context;

        public GeneralSettingsServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GeneralSettingDTO> AddGeneralSettings(AddGeneralSettingDTO model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "The provided model cannot be null.");

            // Parsing time values from string to DateTime
            if (!DateTime.TryParseExact(model.FirstShiftTimeOfAttend, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime firstShiftTimeOfAttend))
                throw new ArgumentException("Invalid FirstShiftTimeOfAttend format. Expected format: HH:mm");

            if (!DateTime.TryParseExact(model.FirstShiftTimeOfLeave, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime firstShiftTimeOfLeave))
                throw new ArgumentException("Invalid FirstShiftTimeOfLeave format. Expected format: HH:mm");

            if (!DateTime.TryParseExact(model.SecondShiftTimeOfAttend, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime secondShiftTimeOfAttend))
                throw new ArgumentException("Invalid SecondShiftTimeOfAttend format. Expected format: HH:mm");

            if (!DateTime.TryParseExact(model.SecondShiftTimeOfLeave, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime secondShiftTimeOfLeave))
                throw new ArgumentException("Invalid SecondShiftTimeOfLeave format. Expected format: HH:mm");

            // Creating entity object
            var generalSetting = new GeneralSetting
            {
              
            };

            try
            {
                // Saving to the database
                await _context.GeneralSetting.AddAsync(generalSetting);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                // Handling database update exceptions (e.g., constraint violations, connection issues)
                throw new InvalidOperationException("An error occurred while saving the general settings. Please try again later.", dbEx);
            }
            catch (Exception ex)
            {
                // Handling any unexpected exceptions
                throw new Exception("An unexpected error occurred while processing your request.", ex);
            }


            // Returning DTO
            return new GeneralSettingDTO
            {
            };
        }

        public async Task<GeneralSettingDTO> GetGeneralSettings()
        {
            try
            {
                var generalSetting = await _context.GeneralSetting.FirstOrDefaultAsync();

                if (generalSetting == null)
                    throw new InvalidOperationException("General settings not found.");

                return new GeneralSettingDTO
                {
                };
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while fetching general settings.", ex);
            }
        }

        public async Task<bool> UpdateGeneralSettings(GeneralSettingDTO model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "The provided model cannot be null.");

            // Retrieve the existing settings
            var generalSetting = await _context.GeneralSetting.FirstOrDefaultAsync();
            if (generalSetting == null)
                throw new InvalidOperationException("General settings not found.");

            // Update only the provided fields
            if (model.OverTimeHour.HasValue)
            {
               
            }
           

            try
            {
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException("An error occurred while updating the general settings. Please try again later.", dbEx);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while processing your request.", ex);
            }
        }

    }
}
