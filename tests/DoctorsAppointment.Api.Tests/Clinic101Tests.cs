
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Controllers;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;

public class Clinic101Tests
{
    [Fact]
    public async Task CLINIC101() // asynch to run simultaneous booking
    {
        // ARRANGE: Create one doctor and ten patients
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var store = new DataStore(new[]
            {
                new Doctor { Id = 1 }
            });

            for (int id = 1; id <= 10; id++)
            {
                store.Patients.Add(new Patient { Id = id });
            }

            // Choose a future working day at 10:00 AM
            var date = DateTime.Today.AddDays(7);

            while (date.DayOfWeek == DayOfWeek.Friday ||
                   date.DayOfWeek == DayOfWeek.Saturday)
            {
                date = date.AddDays(1);
            }

            var bookingTime = date.AddHours(10);

            // Coordinate requests to start together
			// Barrier only coordinates when the threads begin the booking method. It does not guarantee that both threads reach the availability check at the same instant. This is why it is repeated 20 times.
            using var barrier = new Barrier(2);

            // ACT: receptionists attempt the same booking
            var tasks = Enumerable.Range(1, 2).Select(patientId =>
                Task.Factory.StartNew(() =>
                {
                    barrier.SignalAndWait();

                    var controller = new AppointmentsController(store);

                    return controller.CreateAppointment(new Appointment
                    {
                        DoctorId = 1,
                        PatientId = patientId,
                        StartTime = bookingTime
                    });
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            ).ToArray();

			// Wait for both booking attempts to finish
            var results = await Task.WhenAll(tasks);

            // ASSERT: Exactly one request should succeed
            int successful = results.Count(r =>
                r is ObjectResult response &&
                response.StatusCode == 201);

            int conflicts = results.Count(r =>
                r is ObjectResult response &&
                response.StatusCode == 409);

            Assert.True(successful == 1 && conflicts == 1,
                $"Attempt {attempt + 1}: " +
                $"Expected 1 success and 1 conflict, " +
                $"but got {successful} successes and {conflicts} conflicts.");

            Assert.Single(store.Appointments, a => !a.IsCancelled);
        }
    }
}
