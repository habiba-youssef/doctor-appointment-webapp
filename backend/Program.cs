using DoctorsAppointment.Api.Data;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using System.Globalization;
using DoctorsAppointment.Api.Models;
using DoctorsAppointment.Api.Services;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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


builder.Services.AddScoped<
    IPasswordHasher<ClinicUser>,
    PasswordHasher<ClinicUser>
>();

// Read JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("JWT key must be at least 32 bytes.");

// Configure JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Get our database context
    var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    // Create/update database tables before seeding
    db.Database.Migrate();

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

    // Seed demo user accounts
    var hasher = scope.ServiceProvider
        .GetRequiredService<IPasswordHasher<ClinicUser>>();

    if (!db.ClinicUsers.Any(u => u.Username == "receptionist"))
    {
        var password = builder.Configuration["SeedUsers:ReceptionistPassword"]
            ?? throw new InvalidOperationException("Receptionist seed password missing.");

        var user = new ClinicUser
        {
            Username = "receptionist",
            Role = "Receptionist"
        };

        user.PasswordHash = hasher.HashPassword(user, password);
        db.ClinicUsers.Add(user);
    }

    if (!db.ClinicUsers.Any(u => u.Username == "doctor"))
    {
        var password = builder.Configuration["SeedUsers:DoctorPassword"]
            ?? throw new InvalidOperationException("Doctor seed password missing.");

        var doctorId = db.Doctors
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .First();

        var user = new ClinicUser
        {
            Username = "doctor",
            Role = "Doctor",
            DoctorId = doctorId
        };

        user.PasswordHash = hasher.HashPassword(user, password);
        db.ClinicUsers.Add(user);
    }

    db.SaveChanges();

}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting(); // built-in middleware that evaluates an incoming HTTP request and matches it to a specific executable endpoint
app.UseCors("AllowAngular"); // Allows your Angular frontend to access your backend from a different origin using CORS policy

app.UseAuthentication(); // Identify who is making the request
app.UseAuthorization(); // Check what they are allowed to do

app.MapControllers();

app.Run();