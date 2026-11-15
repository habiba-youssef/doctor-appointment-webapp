namespace DoctorsAppointment.Api.Models;

public enum AppointmentType
{
    Consultation,   // 0 = 30 minutes
    FollowUp,       // 1 = 15 minutes
    Procedure       // 2 = 60 minutes
}

// Appointment Class
public class Appointment
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int PatientId { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsCancelled { get; set; }
    public AppointmentType Type { get; set; } = AppointmentType.Consultation;
}

// Availiability Slot Model
public record AvailabilitySlot(
    string Time,
    bool Available
);