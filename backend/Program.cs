using DoctorsAppointment.Api.Data;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using System.Globalization;
using DoctorsAppointment.Api.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ClinicDb")
    )
);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
// builder.Services.AddSingleton<DataStore>(); // run data storage to upload data
builder.Services.AddSingleton<DataStore>(serviceProvider =>
    new DataStore(serviceProvider.GetRequiredService<IWebHostEnvironment>()));      // specifies constructor

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

    // Only insert doctors if the table is empty
    if (!db.Doctors.Any())
    {
        // Find the existing CSV file
        var path = Path.Combine(
            app.Environment.ContentRootPath,
            "Data",
            "doctors.csv"
        );

        // Read doctors from CSV
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(
            reader,
            CultureInfo.InvariantCulture
        );

        var doctors = csv.GetRecords<Doctor>().ToList();

        // Save doctors to PostgreSQL
        db.Doctors.AddRange(doctors);
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
