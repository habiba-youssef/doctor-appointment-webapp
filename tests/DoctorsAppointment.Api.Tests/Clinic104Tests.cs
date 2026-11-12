
using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Controllers;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;

public class Clinic104Tests
{
    [Fact]
    public void CLINIC104()
    {
        // ARRANGE: Create an empty DataStore
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        var controller = new PatientsController(store);

        // Create first patient
        var patient1 = new Patient
        {
            FirstName = "Noura",
            LastName = "Ahmed",
            CivilId = "000000000001",
            PhoneNumber = "55550000",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = GenderOption.Female
        };

        // Create second patient with the SAME Civil ID
        var patient2 = new Patient
        {
            FirstName = "Noura",
            LastName = "Ahmed",
            CivilId = "000000000001",
            PhoneNumber = "55550000",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = GenderOption.Female
        };

        // ACT: Register both patients
        var result1 = controller.CreatePatient(patient1);
        var result2 = controller.CreatePatient(patient2);

        // ASSERT: First registration should succeed
        var response1 = Assert.IsType<ObjectResult>(result1);
        Assert.Equal(201, response1.StatusCode);

        // Second registration should fail with 409 Conflict
        Assert.IsType<ConflictObjectResult>(result2);

        // Only one patient should be saved
        Assert.Single(store.Patients);
    }
}