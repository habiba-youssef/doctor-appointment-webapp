// The DoctorsController.cs file is responsible for handling requests related to doctors.
// Middleman between your frontend (Angular) and your backend data (ClinicDataStore).
using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly DataStore _dataStore;
    // Constructor
    public DoctorsController(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    // GET request to retrieve doctors
    [HttpGet]
    public IActionResult GetDoctors()
    {
        return Ok(_dataStore.Doctors);
    }
}
