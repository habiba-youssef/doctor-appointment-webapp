
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DoctorsAppointment.Api.Controllers;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;

public class Clinic104Tests
{
    [Fact]
    public async Task CLINIC104()
    {
        // ARRANGE 1: Create a temporary in-memory database
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new ClinicDbContext(options);

        // ARRANGE 2: Create the PatientsController
        var controller = new PatientsController(
            db,
            TimeProvider.System
        );

        // ARRANGE 3: Create first patient
        var patient1 = new Patient
        {
            FirstName = "Noura",
            LastName = "Ahmed",
            CivilId = "000000000001",
            PhoneNumber = "55550000",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = GenderOption.Female
        };

        // Create another patient with the SAME Civil ID
        var patient2 = new Patient
        {
            FirstName = "Noura",
            LastName = "Ahmed",
            CivilId = "000000000001",
            PhoneNumber = "55550000",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = GenderOption.Female
        };

        // ACT 1: Register the first patient
        var result1 = await controller.CreatePatient(patient1);

        // ASSERT 1: First registration succeeds (201)
        var response1 = Assert.IsAssignableFrom<ObjectResult>(result1);
        Assert.Equal(201, response1.StatusCode);

        // ACT 2: Register the second patient
        var result2 = await controller.CreatePatient(patient2);

        // ASSERT 2: Second registration is rejected
        var response2 = Assert.IsAssignableFrom<ObjectResult>(result2);

        // Verify ProblemDetails response
        var problem = Assert.IsType<ProblemDetails>(response2.Value);

        Assert.Equal(409, response2.StatusCode ?? problem.Status);

        Assert.Equal("Duplicate patient", problem.Title);

        Assert.Equal("A patient with this Civil ID already exists.", problem.Detail);

        // ASSERT 3: Only one patient was saved
        Assert.Single(db.Patients);
    }
}
