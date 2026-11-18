namespace DoctorsAppointment.Api.Models;

public enum GenderOption
{
    NotSpecified,
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

// Each record represents one working day.
public class DoctorSchedule {
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public DayOfWeek WorkingDays { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class DoctorLeave
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public DateOnly LeaveDate { get; set; }
}
