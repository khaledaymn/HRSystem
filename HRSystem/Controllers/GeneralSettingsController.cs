using HRSystem.DTO.GeneralSettingsDTOs;
using HRSystem.Helper;
using HRSystem.Services.GeneralSettings;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class GeneralSettingsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public GeneralSettingsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        [HttpPost]
        [Route("~/GeneralSettings/AddGeneralSetting")]
        public async Task<IActionResult> AddGeneralSetting([FromBody] AddGeneralSettingDTO model)
        {
            if (model == null)
                return BadRequest("The request body cannot be null.");

            try
            {
                var result = await _unitOfWork.GeneralSettingsServices.AddGeneralSettings(model);
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
                var result = await _unitOfWork.GeneralSettingsServices.GetGeneralSettings();

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
        public async Task<IActionResult> UpdateGeneralSetting([FromBody] AddGeneralSettingDTO model)
        {
            if (model == null)
                return BadRequest("Invalid data.");

            try
            {
                var result = await _unitOfWork.GeneralSettingsServices.UpdateGeneralSettings(model); 

                if (result == null)
                    return NotFound("General settings not found.");

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
