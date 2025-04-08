using HRSystem.DTO.GeneralSettingsDTOs;

namespace HRSystem.Services.GeneralSettings
{
    public interface IGeneralSettingsServices
    {
        Task<GeneralSettingDTO> GetGeneralSettings();
        Task<bool> UpdateGeneralSettings(GeneralSettingDTO model);
        Task<GeneralSettingDTO> AddGeneralSettings(AddGeneralSettingDTO model);
    }
}
