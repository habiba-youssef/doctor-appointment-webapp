import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http'; // Allows Angular to send HTTP requests to your backend API.
import { firstValueFrom } from 'rxjs';    // For Step-by-Step Logic when Booking

const API_BASE_URL = 'http://localhost:5126';

// tells frontend what object structure looks like
import { Doctor } from './models';
import { Patient } from './models';
import { Appointment } from './models';
import { TimeSlot } from './models';

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

    this.http.get(`${API_BASE_URL}/status`, { 
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
    this.http.get<Doctor[]>(`${API_BASE_URL}/doctors`)
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
    this.http.get<Patient[]>(`${API_BASE_URL}/patients`)
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
      `${API_BASE_URL}/appointments/availability?doctorId=${doctor.id}&date=${date}`
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
    // this.http.post<Patient>(`${API_BASE_URL}/patients`, patient)
    // .subscribe({
    //   next: (response) => {
    //     console.log('Patient created:', response);
    //   },
    //   error: (error) => {
    //     console.error('Failed to create patient:', error);
    //   }
    // });
    return this.http.post<Patient>(`${API_BASE_URL}/patients`, patient);  //makes it usable in other functions (BookAppointment())
  }

  // Updated Boooking Logic to check availability before registering patient
  async BookAppointment(patient: Patient, form: HTMLFormElement) {
    this.patientError.set('');

    const doctor = this.selectedDoctor();
    const date = this.selectedDate();
    const time = this.selectedTime();

    if (!doctor || !date || !time) {
      this.patientError.set('Please select a doctor, date and time.');
      return;
    }

    // Send confirmation message
    const confirmed = confirm(
      'Appointment Information - Please check details \n\n' +
      'Patient Info: \n' +
      `Patient Full Name: ${patient.firstName} ${patient.lastName}\n` +
      `Patient DOB: ${patient.dateOfBirth}\n` +
      `Patient Phone Number: +965 ${patient.phoneNumber}\n\n` +

      `Doctor: Dr. ${doctor.firstName} ${doctor.lastName} --- ${doctor.specialty}\n` +
      `Date: ${date}\n` +
      `Time: ${time}`
    );

    if (!confirmed) return;

    try {
      // STEP 1: Check availability
      const slots = await firstValueFrom(
        this.http.get<TimeSlot[]>(
          `${API_BASE_URL}/appointments/availability?doctorId=${doctor.id}&date=${date}`
        )
      );

      if (!slots.some(s => s.time === time && s.available)) {
        this.patientError.set('This time slot is unavailable.');
        this.GetAvailability();
        return;
      }

      // STEP 2: Register patient
      const createdPatient = await firstValueFrom(
        this.createPatient(patient)
      );

      if (createdPatient.id == null) {
        throw new Error('Patient ID was not returned.');
      }

      // STEP 3: Book appointment
      await firstValueFrom(
        this.http.post(`${API_BASE_URL}/appointments`, {
          doctorId: doctor.id,
          patientId: createdPatient.id,
          startTime: `${date}T${time}:00`
        })
      );

      // STEP 4: Confirm booking and refresh
      alert(
        `Appointment booked successfully!\n\n` +
        `Patient: ${patient.firstName} ${patient.lastName}\n` +
        `Doctor: Dr. ${doctor.firstName} ${doctor.lastName}\n` +
        `Date: ${date}\nTime: ${time}`
      );

      form.reset();
      this.selectedTime.set(null);
      this.GetAvailability();
      this.GetBookingsList();
      this.GetPatientsList();

    } catch (error: any) {
      // Handle errors from any step
      this.patientError.set(
        typeof error.error === 'string'
          ? error.error
          : error.error?.title || error.message || 'Booking failed.'
      );

      this.GetAvailability();
    }
  }

  GetBookingsList() {
    this.http.get<Appointment[]>(`${API_BASE_URL}/appointments`)
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
      `${API_BASE_URL}/appointments/${appointmentId}`
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