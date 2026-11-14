using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("patients")]
public class PatientsController : ControllerBase
{
    private readonly ClinicDbContext _db;
	private readonly TimeProvider _clock;
	// set timezone
	private static readonly TimeZoneInfo KuwaitZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuwait");

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
		var kuwaitNow = TimeZoneInfo.ConvertTime(
			_clock.GetUtcNow(),
			KuwaitZone
		);
		var today = DateOnly.FromDateTime(kuwaitNow.DateTime);

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
		else if (patient.DateOfBirth > today){
			return BadRequest("Date of Birth cannot be in the future");
		}
		else if (patient.DateOfBirth < today.AddYears(-110)){
    		return BadRequest("Patient age cannot exceed 110 years.");
		}
		// Clinic-103 Bug Fix
		else if (!Enum.IsDefined(patient.Gender) || patient.Gender == GenderOption.NotSpecified){
			return BadRequest("Gender Selection is Required");
		}
		// Clinic-104 Bug Fix
		else if (string.IsNullOrWhiteSpace(patient.CivilId) || patient.CivilId.Length != 12 || !patient.CivilId.All(c => c >= '0' && c <= '9'))
		{
			return BadRequest("Civil ID must contain exactly 12 digits.");
		}
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