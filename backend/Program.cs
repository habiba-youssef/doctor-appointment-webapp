using DoctorsAppointment.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
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
