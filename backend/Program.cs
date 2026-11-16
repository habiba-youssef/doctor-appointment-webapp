using DoctorsAppointment.Api.Data;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using System.Globalization;
using DoctorsAppointment.Api.Models;
using DoctorsAppointment.Api.Services;
using CsvHelper.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ClinicDb")
    )
);

// Add services to the container.
builder.Services.AddScoped<AppointmentsService>();
builder.Services.AddControllers();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// connect frontend to backend -- CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Get our database context
    var db = scope.ServiceProvider
        .GetRequiredService<ClinicDbContext>();

    // 1. Seed Doctors
    if (!db.Doctors.Any())
    {
        var path = Path.Combine(
            app.Environment.ContentRootPath,
            "Data",
            "doctors.csv"
        );

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(
            reader,
            CultureInfo.InvariantCulture
        );

        var doctors = csv.GetRecords<Doctor>().ToList();

        db.Doctors.AddRange(doctors);
        db.SaveChanges();
    }

    // 2. Seed Doctor Schedules
    if (!db.DoctorSchedules.Any())
    {
        var path = Path.Combine(
            app.Environment.ContentRootPath,
            "Data",
            "doctor_schedules.csv"
        );

        using var reader = new StreamReader(path);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };
        using var csv = new CsvReader(reader, config);

        var schedules = csv.GetRecords<DoctorSchedule>().ToList();

        db.DoctorSchedules.AddRange(schedules);
        db.SaveChanges();
    }

    // 3. Seed Doctor Leave
    if (!db.DoctorLeaves.Any())
    {
        var path = Path.Combine(
            app.Environment.ContentRootPath,
            "Data",
            "doctor_leaves.csv"
        );

        using var reader = new StreamReader(path);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };
        using var csv = new CsvReader(reader, config);

        var leaves = csv.GetRecords<DoctorLeave>().ToList();

        db.DoctorLeaves.AddRange(leaves);
        db.SaveChanges();
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting(); // built-in middleware that evaluates an incoming HTTP request and matches it to a specific executable endpoint
app.UseCors("AllowAngular"); // Allows your Angular frontend to access your backend from a different origin using CORS policy

app.UseAuthorization();

app.MapControllers();

app.Run();