import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http'; // Allows Angular to send HTTP requests to your backend API.

// tells frontend what object structure looks like
interface Doctor {
  id: number;
  firstName: string;
  lastName: string;
  specialty: string;
  gender: number;
  consultationFee: number;
}

interface TimeSlot {
  time: string;
  available: boolean;
}

interface Patient {
  id?: number;
  firstName: string;
  lastName: string;
  gender: number;
  phoneNumber: string;
  dateOfBirth: string; // not Date because using date-selection calendar, the value you retrieve is a string
}

interface Appointment {
  id: number;
  doctorId: number;
  patientId: number;
  startTime: string;
  isCancelled: boolean;
}

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})

export class App {

  private http = inject(HttpClient);
  // signals 
  backendStatus = signal<'checking' | 'online' | 'offline'>('checking');
  doctors = signal<Doctor[]>([]); // to store doctors retrieved
  patients = signal<Patient[]>([]);
  availabilitySlots = signal<TimeSlot[]>([]); // to store time slots retrieved
  selectedDoctor = signal<Doctor | null>(null); // keeps track of which doctor is selected
  selectedDate = signal('');  // keeps track of date selected
  selectedTime = signal<string | null>(null);
  availabilityError = signal('');
  patientError = signal('');
  appointments = signal<Appointment[]>([]);   // for appointments display

  constructor() {
    this.checkBackend();
  }

  checkBackend() {
    this.backendStatus.set('checking');

    this.http.get('http://localhost:5126/status', {
      responseType: 'text'
    }).subscribe({
      next: () => {
        this.backendStatus.set('online');
        this.GetDoctorList(); // run GetDoctorList() as soon as backend runs
        this.GetBookingsList();
        this.GetPatientsList();

      },
      error: () => {
        this.backendStatus.set('offline');
      }
    });
  }

  GetDoctorList() {
    this.http.get<Doctor[]>('http://localhost:5126/doctors')
    .subscribe({
      next: (response) => {this.doctors.set(
        response.sort((a, b) => a.specialty.localeCompare(b.specialty)) // sorting by speciality and alphabetically
      );
        // console.log('Doctors received:', response);
      },

      error: (error) => {
        console.error('Failed to retrieve doctors:', error);
      }
    });
  }

  GetPatientsList() {
    this.http.get<Patient[]>('http://localhost:5126/patients')
      .subscribe({
        next: (response) => {this.patients.set(response);},
        error: (error) => {
          console.error('Failed to retrieve patients:', error);
        }
      });
  }

  selectDoctor(doctor: Doctor) {
    this.selectedDoctor.set(doctor);
    this.selectedTime.set(null);
    this.availabilitySlots.set([]);
  }

  GetAvailability() {

    // 1. Get the selected doctor and date
    const doctor = this.selectedDoctor();
    const date = this.selectedDate();
    this.availabilityError.set('');   // Clear Error Message
    this.selectedTime.set(null);      // Clear selected time

    // 2. Make sure both are selected
    if (!doctor || !date) {
      return;
    }

    // 3. Send request to backend
    this.http.get<TimeSlot[]>(
      `http://localhost:5126/appointments/availability?doctorId=${doctor.id}&date=${date}`
    ).subscribe({

      // 4. Store the returned time slots
      next: (response) => {
        this.availabilitySlots.set(response);
        // console.log('Availability:', response);
      },

      // errors handling
      error: (error) => {
        this.availabilitySlots.set([]);

        this.availabilityError.set(
          typeof error.error === 'string'
            ? error.error
            : error.error?.title || 'Unable to retrieve availability.'
        );
        // console.error('Error:', error);
      }

    });
  }

  selectTime(slot: TimeSlot) {
    if (!slot.available) {
      return;
    }

    this.selectedTime.set(slot.time);
  }

  createPatient(patient: Patient) {
    // this.http.post<Patient>('http://localhost:5126/patients', patient)
    // .subscribe({
    //   next: (response) => {
    //     console.log('Patient created:', response);
    //   },
    //   error: (error) => {
    //     console.error('Failed to create patient:', error);
    //   }
    // });
    return this.http.post<Patient>('http://localhost:5126/patients', patient);  //makes it usable in other functions (BookAppointment())
  }

  BookAppointment(patient: Patient, form: HTMLFormElement) {
    this.patientError.set('');  // Clear Error Message

    // Get the selected doctor, date and time
    const doctor = this.selectedDoctor();
    const date = this.selectedDate();
    const time = this.selectedTime();

    // Check that all appointment details are selected
    if (!doctor || !date || !time) {
      // console.error('Please select a doctor, date and time.');
      this.patientError.set('Please select a doctor, date and time.');
      return;
    }

    // Send confirmation message
    const confirmed = confirm(
      'Appointment Information - Please check details \n\n'+
      'Patient Info: \n' +
      `Patient Full Name: ${patient.firstName} ${patient.lastName}\n` +
      `Patient DOB: ${patient.dateOfBirth}\n` +
      `Patient Phone Number: +965 ${patient.phoneNumber}\n\n` +

      `Doctor: Dr. ${doctor.firstName} ${doctor.lastName}  ---  ${doctor.specialty}\n` +
      `Date: ${date}\n` +
      `Time: ${time}`
    );

    // If the user clicks Cancel, stop the function
    if (!confirmed) {
      return;
    }


    // Register the patient first
    this.createPatient(patient).subscribe({
      next: (createdPatient) => {   //createdPatient is respose returned from backend
        // Get the new patient's ID
        const patientId = createdPatient.id;

        if (patientId == null) {
          console.error('Patient ID was not returned.');
          return;
        }

        // Create the appointment
        this.http.post(
          'http://localhost:5126/appointments',
          {
            doctorId: doctor.id,
            patientId: patientId,
            startTime: `${date}T${time}:00`
          }
        ).subscribe({

          next: (response) => {
            console.log('Appointment booked successfully:', response);

            // creates confirmtion popup window
            alert(
              `Appointment booked successfully!\n\n` +
              `Patient: ${patient.firstName} ${patient.lastName}\n` +
              `Doctor: Dr. ${doctor.firstName} ${doctor.lastName}\n` +
              `Date: ${date}\n` +
              `Time: ${time}`
            );

            form.reset();                     // Reset patient form
            this.GetAvailability();           // Refresh availability
            this.GetBookingsList();           // Refresh booking list
            this.GetPatientsList();

          },

        });

      },

    error: (error) => {
      this.patientError.set(
        typeof error.error === 'string' ? error.error : error.error?.title || 'Failed to register patient.'
      );
      // console.error('Failed to register patient:', error);
    }
    });
  }

  GetBookingsList() {
    this.http.get<Appointment[]>('http://localhost:5126/appointments')
    .subscribe({
      next: (response) => {
        // console.log('Appointments received:', response);
        this.appointments.set(response.filter(appointment => !appointment.isCancelled)); // to hide canceled appointments
      },

      error: (error) => {
        console.error('Failed to retrieve appointment:', error);
      }
    });
  }

  getDoctorName(doctorId: number): string {
    const doctor = this.doctors().find(d => d.id === doctorId);

    return doctor
      ? `Dr. ${doctor.firstName} ${doctor.lastName}`
      : 'Unknown Doctor';
  }

  getPatientName(patientId: number): string {
  const patient = this.patients().find(p => p.id === patientId);

  return patient
    ? `${patient.firstName} ${patient.lastName}`
    : 'Unknown Patient';
  }

  CancelAppointment(appointmentId: number) {
    // Send confirmation message
    const confirmed = confirm(
      'Are you sure you want to cancel this appointment?'
    );

    // If the user clicks Cancel, stop the function
    if (!confirmed) {
      return;
    }

    // Send DELETE request to backend
    this.http.delete(
      `http://localhost:5126/appointments/${appointmentId}`
    ).subscribe({

      // If cancellation is successful
      next: () => {console.log('Appointment cancelled successfully.');
        this.GetBookingsList();     // refresh
        this.GetAvailability();
      },
      // Error handling 
      error: (error) => {console.error('Failed to cancel appointment:', error);}
    });
  }

}