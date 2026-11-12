
// dotnet test .\tests\DoctorsAppointment.Api.Tests\DoctorsAppointment.Api.Tests.csproj --filter "FullyQualifiedName~CLINIC103"


using Xunit;
using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Controllers;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;

public class Clinic103Tests
{
    [Fact]
    public void CLINIC103()
    {
        // ARRANGE: Create the DataStore
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        // Create the PatientsController
        var controller = new PatientsController(store);

        // Create a patient WITHOUT specifying gender
        var patient = new Patient
        {
            FirstName = "Test",
            LastName = "Patient",
            PhoneNumber = "55550000",
            DateOfBirth = new DateOnly(2000, 1, 1)
            // Gender intentionally missing
        };

        // ACT: Attempt to register the patient
        var result = controller.CreatePatient(patient);

        // ASSERT: Registration should be rejected
        Assert.IsType<BadRequestObjectResult>(result);

        // Verify no patient was saved
        Assert.Empty(store.Patients);
    }
}
