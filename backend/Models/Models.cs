namespace DoctorsAppointment.Api.Models;

public enum GenderOption
{
    Female,
    Male
}

// Doctor's class and structure
public class Doctor
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
	public GenderOption Gender{ get; set; }
	public decimal ConsultationFee { get; set;}
}

// Patient's class and structure
public class Patient
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
	public GenderOption Gender{ get; set; }
	public string PhoneNumber { get; set; } = string.Empty;
	public DateOnly DateOfBirth {get; set;}
}

// Appointment Class
public class Appointment
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int PatientId { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsCancelled { get; set; }
}