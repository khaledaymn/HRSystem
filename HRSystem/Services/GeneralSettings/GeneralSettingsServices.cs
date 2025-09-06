using HRSystem.DataBase;
using HRSystem.DTO.GeneralSettingsDTOs;
using HRSystem.Models;
using HRSystem.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Services.GeneralSettings
{
    public class GeneralSettingsServices : IGeneralSettingsServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GeneralSettingsServices> _logger;

        public GeneralSettingsServices(ILogger<GeneralSettingsServices> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        #region Add General Settings
        public async Task<GeneralSettingDTO> AddGeneralSettings(AddGeneralSettingDTO model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "The provided model cannot be null.");

            if (model.NumberOfVacationsInYear < 0)
                throw new ArgumentException("Number of vacations in year cannot be negative.", nameof(model.NumberOfVacationsInYear));

            if (model.RateOfExtraAndLateHour <= 0)
                throw new ArgumentException("Rate of extra hour must be positive.", nameof(model.RateOfExtraAndLateHour));
           
            if (model.NumberOfDayWorkingHours <= 0)
                throw new ArgumentException("Number of working hours must be positive.", nameof(model.NumberOfDayWorkingHours));

            
            var generalSetting = new GeneralSetting
            {
                NumberOfVacationsInYear = model.NumberOfVacationsInYear,
                RateOfExtraHour = model.RateOfExtraAndLateHour,
                NumberOfDayWorkingHours = model.NumberOfDayWorkingHours,
            };

            try
            {
                await _unitOfWork.Repository<GeneralSetting>().ADD(generalSetting);
                await _unitOfWork.Save();
                _logger.LogInformation("Successfully added general settings with ID {Id}.", generalSetting.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding general settings.");
                throw new InvalidOperationException("An error occurred while saving the general settings. Please try again later.", ex);
            }

            return new GeneralSettingDTO
            {
                NumberOfVacationsInYear = generalSetting.NumberOfVacationsInYear,
                RateOfExtraAndLateHour = generalSetting.RateOfExtraHour,
                NumberOfDayWorkingHours = generalSetting.NumberOfDayWorkingHours
            };
        }

        #endregion


        #region Get General Settings

        public async Task<GeneralSettingDTO> GetGeneralSettings()
        {
            try
            {
                var generalSetting = _unitOfWork.Repository<GeneralSetting>().GetAll().Result.FirstOrDefault();

                if (generalSetting == null)
                {
                    _logger.LogWarning("No general settings found in the database.");
                    throw new InvalidOperationException("General settings not found. Please configure the settings first.");
                }

                _logger.LogInformation("Successfully fetched general settings with ID {Id}.", generalSetting.Id);

                return new GeneralSettingDTO
                {
                    NumberOfVacationsInYear = generalSetting.NumberOfVacationsInYear,
                    RateOfExtraAndLateHour = generalSetting.RateOfExtraHour,
                    NumberOfDayWorkingHours = generalSetting.NumberOfDayWorkingHours,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching general settings.");
                throw new InvalidOperationException("An error occurred while fetching general settings. Please try again later.", ex);
            }
        }

        #endregion


        #region Update General Settings
        public async Task<GeneralSettingDTO> UpdateGeneralSettings(AddGeneralSettingDTO model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "The provided model cannot be null.");

            if (model.NumberOfVacationsInYear < 0)
                throw new ArgumentException("Number of vacations in year cannot be negative.", nameof(model.NumberOfVacationsInYear));

            if (model.RateOfExtraAndLateHour <= 0)
                throw new ArgumentException("Rate of extra hour must be positive.", nameof(model.RateOfExtraAndLateHour));

            if (model.NumberOfDayWorkingHours <= 0)
                throw new ArgumentException("Number of working hours must be positive.", nameof(model.NumberOfDayWorkingHours));

            
            var generalSetting = _unitOfWork.Repository<GeneralSetting>().GetAll().Result.FirstOrDefault();
            if (generalSetting == null)
            {
                _logger.LogWarning("No general settings found for update.");
                throw new InvalidOperationException("General settings not found. Please add settings first.");
            }

            try
            {
                generalSetting.NumberOfVacationsInYear = model.NumberOfVacationsInYear;
                generalSetting.RateOfExtraHour = model.RateOfExtraAndLateHour;
                generalSetting.NumberOfDayWorkingHours = model.NumberOfDayWorkingHours;
                _unitOfWork.Repository<GeneralSetting>().Update(generalSetting);
                await _unitOfWork.Save();
                _logger.LogInformation("Successfully updated general settings with ID {Id}.", generalSetting.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating general settings.");
                throw new InvalidOperationException("An error occurred while updating the general settings. Please try again later.", ex);
            }

            return new GeneralSettingDTO
            {
                NumberOfVacationsInYear = generalSetting.NumberOfVacationsInYear,
                RateOfExtraAndLateHour = generalSetting.RateOfExtraHour,
                NumberOfDayWorkingHours = generalSetting.NumberOfDayWorkingHours
            };
        }

        #endregion

    }
}
