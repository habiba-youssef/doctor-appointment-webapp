
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using DoctorsAppointment.Api.Models;
using Testcontainers.PostgreSql;
using Xunit;

public class Clinic101Tests : IAsyncLifetime
{
    // Create a temporary PostgreSQL database for testing
    private readonly PostgreSqlContainer _database =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("clinic_booking")
            .WithUsername("clinic_user")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();

    // Test-only login password
    private readonly string _testPassword = Guid.NewGuid().ToString("N");

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;


    // Find the backend folder containing the CSV seed files
    private static string FindBackendFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var backend = Path.Combine(
                directory.FullName,
                "backend"
            );

            if (File.Exists(Path.Combine(
                backend,
                "DoctorsAppointment.Api.csproj")))
            {
                return backend;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Backend folder not found.");
    }


    // SETUP: Runs before the test
    public async Task InitializeAsync()
    {
        // Start a temporary PostgreSQL container
        await _database.StartAsync();

        // Start the real ASP.NET Core API for testing
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Use backend folder for CSV files
                builder.UseContentRoot(FindBackendFolder());

                // Override real configuration with test configuration
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:ClinicDb"] = _database.GetConnectionString(),
                            ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
                            ["SeedUsers:ReceptionistPassword"] = _testPassword,
                            ["SeedUsers:DoctorPassword"] = Guid.NewGuid().ToString("N")
                        }
                    );
                });
            });

        // Create an HTTP client connected to the test API
        _client = _factory.CreateClient();
    }


    // CLEANUP: Runs after the test
    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();

        // Delete temporary PostgreSQL container
        await _database.DisposeAsync();
    }


    [Fact]
    [Trait("Category", "Integration")]
    public async Task CLINIC101_TenParallelBookings_OnlyOneSucceeds()
    {
        var client = _client!;

        // ARRANGE 1: Log in as Receptionist
        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                username = "receptionist",
                password = _testPassword
            }
        );

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginData = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync()
        );

        var token = loginData.RootElement
            .GetProperty("token")
            .GetString();

        // Add JWT token to future HTTP requests
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);


        // ARRANGE 2: Create ten different patients
        var patientIds = new List<int>();

        for (int i = 1; i <= 10; i++)
        {
            using var patientResponse = await client.PostAsJsonAsync(
                "/patients",
                new
                {
                    civilId = i.ToString("D12"),
                    firstName = "Test",
                    lastName = $"Patient{i}",
                    phoneNumber = "55550000",
                    dateOfBirth = "2000-01-01",
                    gender = 1
                }
            );

            Assert.Equal(HttpStatusCode.Created, patientResponse.StatusCode);

            var patient = await patientResponse.Content
                .ReadFromJsonAsync<Patient>();

            Assert.NotNull(patient);

            patientIds.Add(patient.Id);
        }


        // ARRANGE 3: Choose a future Sunday at 10:00 AM
        // Doctor 1 works Sundays from 08:00 to 12:00
        var date = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(3)
        ).AddDays(7);

        while (date.DayOfWeek != DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        var bookingTime = date.ToDateTime(
            new TimeOnly(10, 0)
        );


        // ARRANGE 4: Prepare simultaneous booking requests
        // All requests will wait until the start signal is sent
        var startSignal = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var tasks = patientIds.Select(async patientId =>
        {
            // Wait for all bookings to be released together
            await startSignal.Task;

            // Each patient attempts the SAME doctor and time
            return await client.PostAsJsonAsync(
                "/appointments",
                new
                {
                    doctorId = 1,
                    patientId,
                    startTime = bookingTime.ToString(
                        "yyyy-MM-ddTHH:mm:ss"
                    ),
                    type = 0
                }
            );
        }).ToArray();


        // ACT: Release all ten booking requests together
        startSignal.SetResult(true);

        // Wait until all requests have finished
        var responses = await Task.WhenAll(tasks);


        // ASSERT 1: Exactly one booking should succeed
        int successful = responses.Count(r =>
            r.StatusCode == HttpStatusCode.Created
        );

        // ASSERT 2: Nine bookings should be rejected
        int conflicts = responses.Count(r =>
            r.StatusCode == HttpStatusCode.Conflict
        );

        Assert.True(
            successful == 1 && conflicts == 9,
            $"Expected 1 success and 9 conflicts, " +
            $"but received {successful} successes " +
            $"and {conflicts} conflicts. " +
            $"HTTP statuses: {string.Join(", ",
                responses.Select(r => (int)r.StatusCode))}"
        );


        // ASSERT 3: Verify only one active appointment exists
        var appointments = await client
            .GetFromJsonAsync<List<Appointment>>(
                $"/appointments?doctorId=1&date={date:yyyy-MM-dd}"
            );

        Assert.NotNull(appointments);

        Assert.Single(
            appointments, 
                a => a.DoctorId == 1 &&
                    !a.IsCancelled &&
                    a.StartTime == bookingTime
        );


        // Dispose HTTP responses
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }
}
