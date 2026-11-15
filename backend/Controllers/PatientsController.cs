using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using Microsoft.EntityFrameworkCore;
using DoctorsAppointment.Api.Helpers;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("patients")]
public class PatientsController : ControllerBase
{
    private readonly ClinicDbContext _db;
	private readonly TimeProvider _clock;

    // Constructor
    public PatientsController(ClinicDbContext db, TimeProvider clock)
    {
        _db = db;
		_clock = clock;
    }

    // GET /patients — Retrieve all registered patients from PostgreSQL DB
    [HttpGet]
    public async Task<IActionResult> GetPatients()
    {
        var patients = await _db.Patients.ToListAsync();
        return Ok(patients);
    }

	// Post /patients — Register a new patient by receiving their details and adding them to PostgreSQL DB
	[HttpPost]
	public async Task<IActionResult> CreatePatient([FromBody] Patient patient)
	{
		// Get today's date in Kuwait
		var today = KuwaitTimeHelper.GetToday(_clock);

		// Check if no First Name was added
		if (string.IsNullOrWhiteSpace(patient.FirstName)){
			return BadRequest("First Name is Required");
		}
		// Check if no Last Name was added
		else if (string.IsNullOrWhiteSpace(patient.LastName)){
			return BadRequest("Surname is Required");
		}
		// Check if no Phone Number was added
		else if (string.IsNullOrWhiteSpace(patient.PhoneNumber)){
			return BadRequest("Phone Number is Required");
		}
		// Phone number mst be 8 digits
		else if (patient.PhoneNumber.Length != 8){
			return BadRequest("Phone number needs to be 8 characters long");
		}
		// Phone number must be digits only, no other characters allowed
		else if (!patient.PhoneNumber.All(c => c >= '0' && c <= '9')){
			return BadRequest("Invalid Phone Number");
		}
		// Check if no DOB was added
		else if (patient.DateOfBirth == default){
			return BadRequest("Date of Birth is Required");
		}
		// No DOB in the future
		else if (patient.DateOfBirth > today){
			return BadRequest("Date of Birth cannot be in the future");
		}
		// No patient should be older than 110 years old
		else if (patient.DateOfBirth < today.AddYears(-110)){
    		return BadRequest("Patient age cannot exceed 110 years.");
		}
		// Clinic-103 Bug Fix - Ensure Gender is added, not just default value
		else if (!Enum.IsDefined(patient.Gender) || patient.Gender == GenderOption.NotSpecified){
			return BadRequest("Gender Selection is Required");
		}
		// Clinic-104 Bug Fix - Civil ID must not be empty + has to be 12 digits + ensures all characers are digits
		else if (string.IsNullOrWhiteSpace(patient.CivilId) || patient.CivilId.Length != 12 || !patient.CivilId.All(c => c >= '0' && c <= '9'))
		{
			return BadRequest("Civil ID must contain exactly 12 digits.");
		}
		// If patient already exists, do not create a new one
		else if (await _db.Patients.AnyAsync(p => p.CivilId == patient.CivilId))
		{
			return Conflict("A patient with this Civil ID already exists.");
		}
		else
		{
			// Generate ID and add patient
			patient.Id = 0;		// default
			_db.Patients.Add(patient);

			try
			{
				// Save patient to PostgreSQL
				await _db.SaveChangesAsync();
			}
			catch (DbUpdateException error) when (		// database update exception
				error.InnerException is Npgsql.PostgresException pgError &&		// Check that the underlying error came from PostgreSQL. Store it in pgError.
				pgError.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation &&		// Check that PostgreSQL rejected a duplicate value. PostgreSQL error code: 23505
				pgError.ConstraintName == "IX_Patients_CivilId")		// Check that the duplicate occurred specifically on the Civil ID unique index
			{
				// Handle duplicate Civil IDs rejected by PostgreSQL
				return Conflict("A patient with this Civil ID already exists.");
			}

			return StatusCode(201, patient);
		}

	}
}