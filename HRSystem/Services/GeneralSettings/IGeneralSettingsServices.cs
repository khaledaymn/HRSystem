using HRSystem.DTO.GeneralSettingsDTOs;

namespace HRSystem.Services.GeneralSettings
{
    public interface IGeneralSettingsServices
    {
        Task<GeneralSettingDTO> GetGeneralSettings();
        Task<GeneralSettingDTO> UpdateGeneralSettings(AddGeneralSettingDTO model);
        Task<GeneralSettingDTO> AddGeneralSettings(AddGeneralSettingDTO model);
    }
}
