using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("patients")]
public class PatientsController : ControllerBase
{
    private readonly DataStore _dataStore;

    // Constructor
    public PatientsController(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    // GET /patients — Retrieve all registered patients from DataStore.Patients
    [HttpGet]
    public IActionResult GetPatients()
    {
        return Ok(_dataStore.Patients);
    }

	// Post /patients — Register a new patient by receiving their details and adding them to DataStore.Patients
	[HttpPost]
	public IActionResult CreatePatient([FromBody] Patient patient)
	{
		// Check if any patient information is invalid
		if (string.IsNullOrWhiteSpace(patient.FirstName)){
			return BadRequest("First Name is Required");
		}
		else if (string.IsNullOrWhiteSpace(patient.LastName)){
			return BadRequest("Surname is Required");
		}
		else if (string.IsNullOrWhiteSpace(patient.PhoneNumber)){
			return BadRequest("Phone Number is Required");
		}
		else if (patient.PhoneNumber.Length != 8){
			return BadRequest("Phone number needs to be 8 characters long");
		}
		else if (!patient.PhoneNumber.All(char.IsDigit)){
			return BadRequest("Invalid Phone Number");
		}
		else if (patient.DateOfBirth == default){
			return BadRequest("Date of Birth is Required");
		}
		else if (patient.DateOfBirth > DateOnly.FromDateTime(DateTime.Today)){
			return BadRequest("Date of Birth needs to be Before Today");
		}
		else if (patient.DateOfBirth < DateOnly.FromDateTime(DateTime.Today).AddYears(-110)){
    		return BadRequest("Patient age cannot exceed 110 years.");
}
		else if (!Enum.IsDefined(patient.Gender)){
			return BadRequest("Gender Selection is Required");
		}
		else
		{
			// Generate ID and add patient
			patient.Id = _dataStore.Patients.Count + 1;
			_dataStore.Patients.Add(patient);

			return StatusCode(201, patient);
		}
	}
}