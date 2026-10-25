// test using: dotnet test .\tests\DoctorsAppointment.Api.Tests\DoctorsAppointment.Api.Tests.csproj

using Xunit;
using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Controllers;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;

public class UnitTest1
{
    [Fact]
    public void CorrectBooking()
    {
        // ARRANGE: Prepare the test data
        var store = new DataStore(new[]{
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });

        var controller = new AppointmentsController(store);

        // Choose a future working day
        var date = DateTime.Today.AddDays(1);

        while (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday){
            date = date.AddDays(1);
        }

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(9).AddMinutes(30)
        };

        //Try to book the appointment
        var result = controller.CreateAppointment(appointment);
        // ASSERT: Check that the booking was rejected
        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, response.StatusCode);
    }

    [Fact]
    // Test booking slot at quarter-past
    public void BookingAtNineFifteen_ShouldBeRejected()
    {
        // ARRANGE: Prepare the test data
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });

        var controller = new AppointmentsController(store);

        // Choose a future working day
        var date = DateTime.Today.AddDays(1);

        while (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday){
            date = date.AddDays(1);
        }

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(9).AddMinutes(15)
        };

        //Try to book the appointment
        var result = controller.CreateAppointment(appointment);
        // ASSERT: Check that the booking was rejected
        var response = Assert.IsType<BadRequestObjectResult>(result);
        // Check the exact error message
        Assert.Equal(
            "Appointments must start on the hour or half hour.",
            response.Value
        );
    }

    [Fact]
    // Test booking slot outside working hours
    public void BookingBeforeWorkingHrs_ShouldBeRejected()
    {
        // ARRANGE: Prepare the test data
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });

        var controller = new AppointmentsController(store);

        // Choose a future working day
        var date = DateTime.Today.AddDays(1);

        while (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday){
            date = date.AddDays(1);
        }

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(7).AddMinutes(0)
        };

        //Try to book the appointment
        var result = controller.CreateAppointment(appointment);
        // ASSERT: Check that the booking was rejected
        var response = Assert.IsType<BadRequestObjectResult>(result);
        // Check the exact error message
        Assert.Equal(
            "Appointments are only available from 08:00 to 16:00.",
            response.Value
        );
    }

    [Fact]
    // Test booking on Weekend
    public void BookingFriday_ShouldBeRejected()
    {
        // ARRANGE: Prepare the test data
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });

        var controller = new AppointmentsController(store);

        // Choose a future working day
        var date = DateTime.Today.AddDays(1);

        while (date.DayOfWeek != DayOfWeek.Friday){
            date = date.AddDays(1);
        }
        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(12).AddMinutes(30)
        };

        //Try to book the appointment
        var result = controller.CreateAppointment(appointment);
        // ASSERT: Check that the booking was rejected
        var response = Assert.IsType<BadRequestObjectResult>(result);
        // Check the exact error message
        Assert.Equal(
            "Appointments are only available Sunday to Thursday.",
            response.Value
        );
    }

    [Fact]
    public void DoubleBooking_ShouldBeRejected()
    {
        // ARRANGE: Create a doctor and two patients
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });
        store.Patients.Add(new Patient { Id = 2 });

        var controller = new AppointmentsController(store);

        // Choose a future working day
        var date = DateTime.Today.AddDays(1);

        while (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday){
            date = date.AddDays(1);
        }

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
        var result1 = controller.CreateAppointment(appointment1);
        var result2 = controller.CreateAppointment(appointment2);

        // ASSERT: First booking should succeed (201)
        var response1 = Assert.IsType<ObjectResult>(result1);
        Assert.Equal(201, response1.StatusCode);

        // ASSERT: Second booking should fail (409)
        var response = Assert.IsType<ConflictObjectResult>(result2);

        Assert.Equal(
            "This doctor already has an appointment at this time.",
            response.Value
        );
    }

    [Fact] 
    // Test cancel and rebook
    public void Rebook_ShouldPass()
    {
        // ARRANGE: Create a doctor and two patients
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });
        store.Patients.Add(new Patient { Id = 2 });

        var controller = new AppointmentsController(store);

        // Choose a future working day
        var date = DateTime.Today.AddDays(1);

        while (date.DayOfWeek == DayOfWeek.Friday ||
            date.DayOfWeek == DayOfWeek.Saturday)
        {
            date = date.AddDays(1);
        }

        var appointment1 = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(12).AddMinutes(30)
        };

        // ACT 1: Book the first appointment
        var result1 = controller.CreateAppointment(appointment1);

        // ASSERT: Booking succeeds
        var response1 = Assert.IsType<ObjectResult>(result1);
        Assert.Equal(201, response1.StatusCode);

        // ACT 2: Cancel the first appointment
        var cancelResult = controller.CancelAppointment(appointment1.Id);

        // ASSERT: Cancellation succeeds
        Assert.IsType<NoContentResult>(cancelResult);

        // ARRANGE: Another patient requests the same slot
        var appointment2 = new Appointment
        {
            DoctorId = 1,
            PatientId = 2,
            StartTime = date.AddHours(12).AddMinutes(30)
        };

        // ACT 3: Rebook the cancelled slot
        var result2 = controller.CreateAppointment(appointment2);

        // ASSERT: Rebooking succeeds
        var response2 = Assert.IsType<ObjectResult>(result2);
        Assert.Equal(201, response2.StatusCode);
    }

    [Fact]
    // Booking Dates in the Past
    public void BookingInPast_ShouldBeRejected()
    {
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });

        var controller = new AppointmentsController(store);

        // Choose yesterday at 9:00 AM
        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = DateTime.Today.AddDays(-1).AddHours(12)
        };

        var result = controller.CreateAppointment(appointment);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    // Test booking slot at 4 pm
    public void BookingAtFour_ShouldBeRejected()
    {
        // ARRANGE: Prepare the test data
        var store = new DataStore(new[]
        {
            new Doctor { Id = 1 }
        });

        store.Patients.Add(new Patient { Id = 1 });

        var controller = new AppointmentsController(store);

        // Choose a future working day
        var date = DateTime.Today.AddDays(1);

        while (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday){
            date = date.AddDays(1);
        }

        var appointment = new Appointment
        {
            DoctorId = 1,
            PatientId = 1,
            StartTime = date.AddHours(16).AddMinutes(0)
        };

        //Try to book the appointment
        var result = controller.CreateAppointment(appointment);
        // ASSERT: Check that the booking was rejected
        var response = Assert.IsType<BadRequestObjectResult>(result);
        // Check the exact error message
        Assert.Equal(
            "Appointments are only available from 08:00 to 16:00.",
            response.Value
        );
    }

}