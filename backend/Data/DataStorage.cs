
using CsvHelper;
using System.Globalization; // ensure your CSV is interpreted consistently, even when someone runs your application on another computer
using DoctorsAppointment.Api.Models;

namespace DoctorsAppointment.Api.Data;

// shared instance of data storage -- point of reference so all lists are stored in the same location
public class DataStore
{
    public List<Doctor> Doctors { get; }
    public List<Patient> Patients { get; } = new();
    public List<Appointment> Appointments { get; } = new();

    // Additional constructor used by your unit tests to supply sample doctors directly.
    public DataStore(IEnumerable<Doctor> doctors) { Doctors = doctors.ToList();}

	// constructor that runs automatically when an object of a class is created, collects doctor data instantly from csv file
    public DataStore(IWebHostEnvironment environment)
    {
        // string path = "Data/doctors.csv";
        string path = Path.Combine(
            environment.ContentRootPath,
            "Data",
            "doctors.csv"
        );
        using var reader = new StreamReader(path); // creates temporary csv reader
        using var doctor_data = new CsvReader(reader, CultureInfo.InvariantCulture); // reads data

        Doctors = doctor_data.GetRecords<Doctor>().ToList(); // converts csv rows into objects
    }
}
