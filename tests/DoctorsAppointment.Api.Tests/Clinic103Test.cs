
// dotnet test .\tests\DoctorsAppointment.Api.Tests\DoctorsAppointment.Api.Tests.csproj --filter "FullyQualifiedName~CLINIC103"

using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DoctorsAppointment.Api.Controllers;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;

public class Clinic103Tests
{
    [Fact]
    public async Task CLINIC103()
    {
        // ARRANGE 1: Create a temporary in-memory database
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new ClinicDbContext(options);

        // Create the PatientsController
        var controller = new PatientsController(
            db,
            TimeProvider.System
        );

        // Create a patient WITHOUT specifying gender
        var patient = new Patient
        {
            FirstName = "Test",
            LastName = "Patient",
            CivilId = "000000000001", // Update after CLINIC-104
            PhoneNumber = "55550000",
            DateOfBirth = new DateOnly(2000, 1, 1)
            // Gender intentionally missing
        };

        // ACT: Attempt to register the patient
        var result = await controller.CreatePatient(patient);
        
        // ASSERT 1: Registration should return a validation error
        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(response.Value);

        // ASSERT 2: Verify that gender caused the validation failure
        Assert.False(controller.ModelState.IsValid);

        Assert.True(problem.Errors.ContainsKey("gender"));

        Assert.Contains("Gender Selection is Required", problem.Errors["gender"]);

        // ASSERT 3: Verify no patient was registered
        Assert.Empty(db.Patients);
    }
}
