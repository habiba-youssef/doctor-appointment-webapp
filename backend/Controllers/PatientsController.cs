using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using Microsoft.EntityFrameworkCore;
using DoctorsAppointment.Api.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Authorize(Roles = "Receptionist")]
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

    // GET /patients/search - Search existing patients
    [HttpGet("search")]
    public async Task<IActionResult> SearchPatients([FromQuery] string? civilId)
    {
        // Validate Civil ID
    	if (string.IsNullOrWhiteSpace(civilId) || civilId.Length != 12 || !civilId.All(char.IsDigit))
		{
			ModelState.AddModelError("civilId", "Civil ID must contain exactly 12 digits.");
			return ValidationProblem(ModelState);
		}
		
		// Search for existing patient
		var patient = await _db.Patients
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.CivilId == civilId);

		if (patient == null)
		{
			return Problem(
                statusCode: 404,
                title: "Patient not found",
                detail: "No patient was found with this Civil ID."
            );
		}

		return Ok(patient);
    }

	// Post /patients — Register a new patient by receiving their details and adding them to PostgreSQL DB
	[HttpPost]
	public async Task<IActionResult> CreatePatient([FromBody] Patient patient)
	{
		// Get today's date in Kuwait
		var today = KuwaitTimeHelper.GetToday(_clock);

		// Check if no First Name was added
		if (string.IsNullOrWhiteSpace(patient.FirstName)){
            ModelState.AddModelError("firstName", "First Name is Required");
            return ValidationProblem(ModelState);
		}
		// Check if no Last Name was added
		else if (string.IsNullOrWhiteSpace(patient.LastName)){
			ModelState.AddModelError("lastName", "Surname Name is Required");
            return ValidationProblem(ModelState);
		}
		// Check if no Phone Number was added
		else if (string.IsNullOrWhiteSpace(patient.PhoneNumber)){
			ModelState.AddModelError("phoneNumber", "Phone Number is Required");
            return ValidationProblem(ModelState);
		}
		// Phone number mst be 8 digits
		else if (patient.PhoneNumber.Length != 8){
			ModelState.AddModelError("phoneNumber", "Phone number needs to be 8 characters long");
            return ValidationProblem(ModelState);
		}
		// Phone number must be digits only, no other characters allowed
		else if (!patient.PhoneNumber.All(c => c >= '0' && c <= '9')){
			ModelState.AddModelError("phoneNumber", "Invalid Phone Number");
            return ValidationProblem(ModelState);
		}
		// Check if no DOB was added
		else if (patient.DateOfBirth == default){
			ModelState.AddModelError("dateOfBirth", "Date of Birth is Required");
            return ValidationProblem(ModelState);
		}
		// No DOB in the future
		else if (patient.DateOfBirth > today){
			ModelState.AddModelError("dateOfBirth", "Date of Birth cannot be in the future");
            return ValidationProblem(ModelState);
		}
		// No patient should be older than 110 years old
		else if (patient.DateOfBirth < today.AddYears(-110)){
			ModelState.AddModelError("dateOfBirth", "Patient age cannot exceed 110 years");
            return ValidationProblem(ModelState);
		}
		// Clinic-103 Bug Fix - Ensure Gender is added, not just default value
		else if (!Enum.IsDefined(patient.Gender) || patient.Gender == GenderOption.NotSpecified){
			ModelState.AddModelError("gender", "Gender Selection is Required");
            return ValidationProblem(ModelState);
		}
		// Clinic-104 Bug Fix - Civil ID must not be empty + has to be 12 digits + ensures all characers are digits
		else if (string.IsNullOrWhiteSpace(patient.CivilId) || patient.CivilId.Length != 12 || !patient.CivilId.All(c => c >= '0' && c <= '9'))
		{
			ModelState.AddModelError("civilId", "Civil ID must contain exactly 12 digits.");
			return ValidationProblem(ModelState);
		}
		// If patient already exists, do not create a new one
		else if (await _db.Patients.AnyAsync(p => p.CivilId == patient.CivilId))
		{
			return Problem(
                statusCode: 409,
                title: "Duplicate patient",
                detail: "A patient with this Civil ID already exists."
            );
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
				return Problem(
					statusCode: 409,
					title: "Duplicate patient",
					detail: "A patient with this Civil ID already exists."
				);
			}

			return StatusCode(201, patient);
		}

	}
}