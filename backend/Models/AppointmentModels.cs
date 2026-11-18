namespace DoctorsAppointment.Api.Models;

public enum AppointmentType
{
    Consultation,   // 0 = 30 minutes
    FollowUp,       // 1 = 15 minutes
    Procedure       // 2 = 60 minutes
}

public enum AppointmentStatus
{
    Booked = 0,
    Completed = 1,
    NoShow = 2
}

// Appointment Class
public class Appointment
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int PatientId { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public AppointmentType Type { get; set; } = AppointmentType.Consultation;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
}

// Availiability Slot Model
public record AvailabilitySlot(
    string Time,
    bool Available
);