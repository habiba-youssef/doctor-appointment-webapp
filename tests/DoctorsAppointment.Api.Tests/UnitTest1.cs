
using Xunit;
using Microsoft.EntityFrameworkCore;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using DoctorsAppointment.Api.Services;

public class UnitTest1
{
    // Create a temporary database with a doctor, patients and working schedule
    private static async Task<ClinicDbContext> CreateTestDb(int patientCount = 1)
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ClinicDbContext(options);

        db.Doctors.Add(new Doctor { Id = 1 });

        for (int i = 1; i <= patientCount; i++)
        {
            db.Patients.Add(new Patient
            {
                Id = i,
                FirstName = "Test",
                LastName = $"Patient{i}",
                CivilId = i.ToString("D12"),
                PhoneNumber = "55550000",
                DateOfBirth = new DateOnly(2000, 1, 1),
                Gender = GenderOption.Female
            });
        }

        // Doctor works Sunday to Thursday, 08:00 to 16:00
        for (int day = 0; day <= 4; day++)
        {
            db.DoctorSchedules.Add(new DoctorSchedule
            {
                DoctorId = 1,
                WorkingDays = (DayOfWeek)day,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0)
            });
        }

        await db.SaveChangesAsync();
        return db;
    }

    // Choose a future working day using Kuwait time
    private static DateTime GetFutureWorkingDay()
    {
        var date = DateTime.UtcNow.AddHours(3).Date.AddDays(2);

        while (date.DayOfWeek == DayOfWeek.Friday ||
               date.DayOfWeek == DayOfWeek.Saturday)
        {
            date = date.AddDays(1);
        }

        return date;
    }


    [Fact]
    public async Task CorrectBooking()
    {
        // ARRANGE: Prepare the test data
        using var db = await CreateTestDb();
        var service = new AppointmentsService(db, TimeProvider.System);

        var date = GetFutureWorkingDay();

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(9).AddMinutes(30)
        };

        // ACT: Try to book the appointment
        var result = await service.CreateAppointmentAsync(appointment);

        // ASSERT: Booking should succeed
        Assert.Equal(BookingStatus.Created, result.Status);
        Assert.Single(db.Appointments);
    }


    [Fact]
    // Test booking at quarter-past (now allowed in Stage 2)
    public async Task BookingAtNineFifteen_ShouldBeAccepted()
    {
        // ARRANGE: Prepare the test data
        using var db = await CreateTestDb();
        var service = new AppointmentsService(db, TimeProvider.System);

        var date = GetFutureWorkingDay();

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(9).AddMinutes(15)
        };

        // ACT: Try to book the appointment
        var result = await service.CreateAppointmentAsync(appointment);

        // ASSERT: 09:15 is now a valid start time
        Assert.Equal(BookingStatus.Created, result.Status);
    }


    [Fact]
    // Test booking outside working hours
    public async Task BookingBeforeWorkingHrs_ShouldBeRejected()
    {
        // ARRANGE: Prepare the test data
        using var db = await CreateTestDb();
        var service = new AppointmentsService(db, TimeProvider.System);

        var date = GetFutureWorkingDay();

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(7)
        };

        // ACT: Try to book the appointment
        var result = await service.CreateAppointmentAsync(appointment);

        // ASSERT: Booking should be rejected
        Assert.Equal(BookingStatus.Invalid, result.Status);

        Assert.Equal(
            "Appointments are only available between 08:00 and 16:00.",
            result.Error
        );
    }


    [Fact]
    // Test booking on Friday
    public async Task BookingFriday_ShouldBeRejected()
    {
        // ARRANGE: Prepare the test data
        using var db = await CreateTestDb();
        var service = new AppointmentsService(db, TimeProvider.System);

        var date = GetFutureWorkingDay();

        while (date.DayOfWeek != DayOfWeek.Friday)
        {
            date = date.AddDays(1);
        }

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(12).AddMinutes(30)
        };

        // ACT: Try to book the appointment
        var result = await service.CreateAppointmentAsync(appointment);

        // ASSERT: Booking should be rejected
        Assert.Equal(BookingStatus.Invalid, result.Status);

        Assert.Equal(
            "Appointments are only available Sunday to Thursday.",
            result.Error
        );
    }


    [Fact]
    public async Task DoubleBooking_ShouldBeRejected()
    {
        // ARRANGE: Create a doctor and two patients
        using var db = await CreateTestDb(patientCount: 2);
        var service = new AppointmentsService(db, TimeProvider.System);

        var date = GetFutureWorkingDay();

        // First patient's appointment
        var appointment1 = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(12).AddMinutes(30)
        };

        // Second patient requests the same slot
        var appointment2 = new Appointment
        {
            DoctorId = 1,
            PatientId = 2,
            StartTime = date.AddHours(12).AddMinutes(30)
        };

        // ACT: Book both appointments
        var result1 = await service.CreateAppointmentAsync(appointment1);
        var result2 = await service.CreateAppointmentAsync(appointment2);

        // ASSERT: First booking should succeed
        Assert.Equal(BookingStatus.Created, result1.Status);

        // ASSERT: Second booking should fail
        Assert.Equal(BookingStatus.Conflict, result2.Status);

        Assert.Equal(
            "Doctor already has an overlapping appointment.",
            result2.Error
        );

        Assert.Single(db.Appointments);
    }


    [Fact]
    // Test cancellation and rebooking
    public async Task Rebook_ShouldPass()
    {
        // ARRANGE: Create a doctor and two patients
        using var db = await CreateTestDb(patientCount: 2);
        var service = new AppointmentsService(db, TimeProvider.System);

        var date = GetFutureWorkingDay();

        var appointment1 = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(12).AddMinutes(30)
        };

        // ACT 1: Book the first appointment
        var result1 = await service.CreateAppointmentAsync(appointment1);

        // ASSERT: Booking succeeds
        Assert.Equal(BookingStatus.Created, result1.Status);

        // ACT 2: Cancel the first appointment
        var cancelResult = await service.CancelAppointmentAsync(
            appointment1.Id,
            "Patient requested cancellation"
        );

        // ASSERT: Cancellation succeeds
        Assert.Equal(CancellationStatus.Cancelled, cancelResult.Status);

        // ARRANGE: Another patient requests the same slot
        var appointment2 = new Appointment
        {
            DoctorId = 1,
            PatientId = 2,
            StartTime = date.AddHours(12).AddMinutes(30)
        };

        // ACT 3: Rebook the cancelled slot
        var result2 = await service.CreateAppointmentAsync(appointment2);

        // ASSERT: Rebooking succeeds
        Assert.Equal(BookingStatus.Created, result2.Status);

        // Exactly one active appointment remains
        Assert.Single(db.Appointments, a => !a.IsCancelled);
    }


    [Fact]
    // Test booking dates in the past
    public async Task BookingInPast_ShouldBeRejected()
    {
        // ARRANGE: Prepare the test data
        using var db = await CreateTestDb();
        var service = new AppointmentsService(db, TimeProvider.System);

        // Choose a past working day
        var date = DateTime.UtcNow.AddHours(3).Date.AddDays(-1);

        while (date.DayOfWeek == DayOfWeek.Friday ||
               date.DayOfWeek == DayOfWeek.Saturday)
        {
            date = date.AddDays(-1);
        }

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(12)
        };

        // ACT: Try to book the appointment
        var result = await service.CreateAppointmentAsync(appointment);

        // ASSERT: Booking should be rejected
        Assert.Equal(BookingStatus.Invalid, result.Status);

        Assert.Equal(
            "Cannot book an appointment in the past.",
            result.Error
        );
    }


    [Fact]
    // Test booking at 4 PM
    public async Task BookingAtFour_ShouldBeRejected()
    {
        // ARRANGE: Prepare the test data
        using var db = await CreateTestDb();
        var service = new AppointmentsService(db, TimeProvider.System);

        var date = GetFutureWorkingDay();

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(16)
        };

        // ACT: Try to book the appointment
        var result = await service.CreateAppointmentAsync(appointment);

        // ASSERT: Booking should be rejected
        Assert.Equal(BookingStatus.Invalid, result.Status);

        Assert.Equal(
            "Appointments are only available between 08:00 and 16:00.",
            result.Error
        );
    }
}