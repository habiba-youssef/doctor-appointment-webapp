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


	// [HttpPost]
	// public IActionResult NewPatient([FromBody] Patient patient)
	// {
	// 	patient.Id = _dataStore.Patients.Count + 1; 		// Generate a unique ID for the new patient
	// 	_dataStore.Patients.Add(patient); 		// Add patient to the in Patients list
	// 	return StatusCode(201, patient); 		// Return the created patient
	// 	// return StatusCode(400, "Data entered Incorrectly");

	// }

	// added validation checks --- edit later to make specific checks!!
	[HttpPost]
	public IActionResult CreatePatient([FromBody] Patient patient)
	{
		// Check if any patient information is invalid
		if (string.IsNullOrWhiteSpace(patient.FirstName) ||
			string.IsNullOrWhiteSpace(patient.LastName) ||
			string.IsNullOrWhiteSpace(patient.PhoneNumber) ||
			patient.PhoneNumber.Length != 8 || 	// ensures 8 digits
			!patient.PhoneNumber.All(char.IsDigit) || 	// checks that all entered characters are ints
			patient.DateOfBirth == default ||
			patient.DateOfBirth > DateOnly.FromDateTime(DateTime.Today) ||
			!Enum.IsDefined(patient.Gender))
		{
			return BadRequest("Error in patient data entered; check info...");
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