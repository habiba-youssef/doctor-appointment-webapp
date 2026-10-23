using Microsoft.AspNetCore.Mvc;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("status")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Backend is working!");
    }
}