// The DoctorsController.cs file is responsible for handling requests related to doctors.
// Middleman between your frontend (Angular) and your backend data (ClinicDataStore).
using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("doctors")]
public class DoctorsController : ControllerBase
{
    private readonly ClinicDbContext _db;
    // Constructor
    public DoctorsController(ClinicDbContext db)
    {
        _db = db;
    }

    // GET request to retrieve doctors
    [HttpGet]
    public async Task<IActionResult> GetDoctors()
    {
        var doctors = await _db.Doctors.ToListAsync();

        return Ok(doctors);
    }
}
