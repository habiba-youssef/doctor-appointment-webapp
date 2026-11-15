namespace DoctorsAppointment.Api.Models;

// Patient's class and structure
public class Patient
{
    public int Id { get; set; }
    public string CivilId { get; set; } = string.Empty; // unique identifier
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
	public GenderOption Gender{ get; set; }
	public string PhoneNumber { get; set; } = string.Empty;
	public DateOnly DateOfBirth {get; set;}
}