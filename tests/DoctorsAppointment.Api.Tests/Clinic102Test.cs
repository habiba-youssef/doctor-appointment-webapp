
using Xunit;
using Microsoft.EntityFrameworkCore;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using DoctorsAppointment.Api.Services;
using DoctorsAppointment.Api.Helpers;

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
    public async Task CLINIC102()
    {
        // ARRANGE 1: Create a temporary in-memory database
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new ClinicDbContext(options);

        // ARRANGE 2: Create a doctor and patient
        db.Doctors.Add(new Doctor
        {
            Id = 1,
            FirstName = "Test",
            LastName = "Doctor"
        });

        db.Patients.Add(new Patient
        {
            Id = 1,
            FirstName = "Test",
            LastName = "Patient",
            CivilId = "000000000001",
            PhoneNumber = "55550000",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Gender = GenderOption.Female
        });

        // Doctor works Sunday from 08:00 to 12:00
        db.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = 1,
            WorkingDays = DayOfWeek.Sunday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0)
        });

        await db.SaveChangesAsync();

        // Simulate the server's clock
        var fakeClock = new TestClock();
        Assert.Equal(
            new DateTime(2030, 1, 6, 11, 0, 0),
            KuwaitTimeHelper.GetNow(fakeClock)
        );

        var controller = new AppointmentsService(
            db, fakeClock
        );

        // Use the current booking service
        var service = new AppointmentsService(db, fakeClock);

        // Attempt to book at 10:00 Kuwait time
        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = new DateTime(2030, 1, 6, 10, 0, 0),
            Type = AppointmentType.Consultation
        };

        // ACT 1: Try to book the appointment
        var result = await service.CreateAppointmentAsync(appointment);

        // ASSERT 1: Booking should be rejected
        Assert.Equal(BookingStatus.Invalid, result.Status);

        Assert.Equal(
            "Cannot book an appointment in the past.",
            result.Error
        );

        // ASSERT 2: No appointment should have been saved
        Assert.Empty(db.Appointments);

        // ACT 2: Check the doctor's availability on the same day
        var availability = await service.GetAvailabilityAsync(
            1,
            new DateOnly(2030, 1, 6),
            AppointmentType.Consultation
        );

        // ASSERT 3: Availability request succeeds
        Assert.Equal(AvailabilityStatus.Valid, availability.Status);
        Assert.NotNull(availability.Slots);

        // 10:00 has already passed
        var pastSlot = availability.Slots.Single(
            s => s.Time == "10:00"
        );

        Assert.False(pastSlot.Available);

        // 11:15 is still in the future and within working hours
        var futureSlot = availability.Slots.Single(
            s => s.Time == "11:15"
        );

        Assert.True(futureSlot.Available);
    }
}
