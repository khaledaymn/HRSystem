using HRSystem.DTO.GeneralSettingsDTOs;
using HRSystem.Services.GeneralSettings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeneralSettingsController : ControllerBase
    {
        private readonly IGeneralSettingsServices _generalSettingsServices;
        public GeneralSettingsController(IGeneralSettingsServices generalSettingsServices) => _generalSettingsServices = generalSettingsServices;

        [HttpPost]
        [Route("~/GeneralSettings/AddGeneralSetting")]
        public async Task<IActionResult> AddGeneralSetting([FromBody] AddGeneralSettingDTO model)
        {
            if (model == null)
                return BadRequest("The request body cannot be null.");

            try
            {
                var result = await _generalSettingsServices.AddGeneralSettings(model);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }


        [HttpGet]
        [Route("~/GeneralSettings/GetGeneralSetting")]
        public async Task<IActionResult> GetGeneralSetting()
        {
            try
            {
                var result = await _generalSettingsServices.GetGeneralSettings();

                if (result == null)
                    return NotFound("General settings not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPut]
        [Route("~/GeneralSettings/UpdateGeneralSetting")]
        public async Task<IActionResult> UpdateGeneralSetting([FromBody] GeneralSettingDTO model)
        {
            if (model == null)
                return BadRequest("Invalid data.");

            try
            {
                bool isUpdated = await _generalSettingsServices.UpdateGeneralSettings(model); 

                if (!isUpdated)
                    return StatusCode(500, "Failed to update general settings.");

                return Ok(new { message = "General settings updated successfully." });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

    }
}
