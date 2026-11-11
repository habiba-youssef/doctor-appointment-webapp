
using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Controllers;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;

// Creates a fake clock. Instead of reading the actual time, it returns the time we choose.
public class TestClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        // Sunday, January 6, 2030 at 08:00 UTC
        return new DateTimeOffset(
            2030, 1, 6, 8, 0, 0, TimeSpan.Zero
        );
    }

    // Simulate a server configured to UTC
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public class Clinic102Tests
{
    [Fact]
    public void CLINIC102()
    {
        // ARRANGE: Create a doctor and patient
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });

        // Simulate the server's clock
        var fakeClock = new TestClock();

        var controller = new AppointmentsController(
            store, fakeClock
        );

        // Book at 10:00 Kuwait time
        // Actual Kuwait time is already 11:00
        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = new DateTime(
                2030, 1, 6, 10, 0, 0
            )
        };

        // ACT: Try to book the appointment
        var result = controller.CreateAppointment(appointment);

        // ASSERT: Booking should be rejected
        Assert.IsType<BadRequestObjectResult>(result);

        // Verify no appointment was saved
        Assert.Empty(store.Appointments);
    }
}
