import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http'; // Allows Angular to send HTTP requests to your backend API.
import { firstValueFrom } from 'rxjs';    // For Step-by-Step Logic when Booking
import { AppointmentType } from './models';
import { HttpErrorResponse } from '@angular/common/http';   // identify what kind of HTTP error occurred

const API_BASE_URL = 'http://localhost:5126';

// tells frontend what object structure looks like
import { Doctor } from './models';
import { Patient } from './models';
import { Appointment } from './models';
import { TimeSlot } from './models';
import { RescheduleForm } from './models';

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
  selectedType = signal<AppointmentType>(AppointmentType.Consultation);
  availabilityError = signal('');
  patientError = signal('');
  cancellationError = signal('');   // clinic-105 fix
  appointments = signal<Appointment[]>([]);   // for appointments display
  AppointmentType = AppointmentType;
  existingPatient = signal<Patient | null>(null);   // Stores the patient returned by the backend
  patientSearchStatus = signal<'idle' | 'found' | 'notFound'>('idle');
  patientSearchError = signal('');    // Stores any search error message
  // Rescheduling
  reschedule = signal<RescheduleForm | null>(null);

  rescheduleSlots = signal<TimeSlot[]>([]);
  rescheduleError = signal('');
  rescheduleSuccess = signal('');
  savingReschedule = signal(false);

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

  selectAppointmentType(type: AppointmentType) {
      this.selectedType.set(type);

      // Clear old availability when appointment type changes
      this.availabilitySlots.set([]);
      this.selectedTime.set(null);
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
      `${API_BASE_URL}/appointments/availability?doctorId=${doctor.id}&date=${date}&type=${this.selectedType()}`
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
    return this.http.post<Patient>(`${API_BASE_URL}/patients`, patient);  //makes it usable in other functions (BookAppointment())
  }

  getSlotEndTime(startTime: string): string {
    // 1. Get appointment duration
    const duration = {
      [AppointmentType.Consultation]: 30,
      [AppointmentType.FollowUp]: 15,
      [AppointmentType.Procedure]: 60
    }[this.selectedType()];

    // 2. Convert start time into hours and minutes
    const [hours, minutes] = startTime.split(':').map(Number);

    // 3. Calculate end time in total minutes
    const totalMinutes = hours * 60 + minutes + duration;

    // 4. Convert back to HH:mm format
    const endHours = Math.floor(totalMinutes / 60);
    const endMinutes = totalMinutes % 60;

    return `${String(endHours).padStart(2, '0')}:${String(endMinutes).padStart(2, '0')}`;
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

    // Make sure a Civil ID search was completed before booking
    if (this.patientSearchStatus() === 'idle') {
        this.patientError.set('Please search for the patient using their Civil ID first.');
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
          `${API_BASE_URL}/appointments/availability?doctorId=${doctor.id}&date=${date}&type=${this.selectedType()}`
        )
      );

      if (!slots.some(s => s.time === time && s.available)) {
        this.patientError.set('This time slot is unavailable.');
        this.GetAvailability();
        return;
      }

      // STEP 2: Find existing patient or register a new one
      let patientId: number;

      try {
        // Search for the patient using Civil ID
        const existingPatient = await firstValueFrom(
          this.http.get<Patient>(`${API_BASE_URL}/patients/search?civilId=${encodeURIComponent(patient.civilId)}`)
        );

        // Patient exists - reuse their ID
        if (existingPatient.id == null) {
          throw new Error('Patient ID was not returned.');
        }

        patientId = existingPatient.id;

      } catch (error) {

        // Only register a new patient if the search returned 404
        if (!(error instanceof HttpErrorResponse) || error.status !== 404) {
          throw error;
        }

        const createdPatient = await firstValueFrom(
          this.createPatient(patient)
        );

        if (createdPatient.id == null) {
          throw new Error('Patient ID was not returned.');
        }

        patientId = createdPatient.id;
      }

      // STEP 3: Book appointment
      await firstValueFrom(
        this.http.post(`${API_BASE_URL}/appointments`, {
          doctorId: doctor.id,
          patientId,
          startTime: `${date}T${time}:00`,
          type: this.selectedType()
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

  getAppointmentType(type: AppointmentType): string {
    switch (type) {
      case AppointmentType.Consultation:
        return 'Consultation (30 min)';

      case AppointmentType.FollowUp:
        return 'Follow-up (15 min)';

      case AppointmentType.Procedure:
        return 'Procedure (60 min)';

      default:
        return 'Unknown';
    }
  }

  CancelAppointment(appointmentId: number) {
    // Clear any previous cancellation errors
    this.cancellationError.set('');

    const reason = prompt(
        'Are you sure you want to cancel this appointment?\n\nPlease enter a cancellation reason:'
    );

    if (reason === null) {
        return; // User clicked Cancel
    }

    if (!reason.trim()) {
        this.cancellationError.set('Cancellation reason is required.');
        return;
    }
    
    // Send DELETE request to backend
    this.http.delete(
      `${API_BASE_URL}/appointments/${appointmentId}`,
      {
        body: { reason: reason.trim() }
      }
    ).subscribe({

      // If cancellation is successful
      next: () => {console.log('Appointment cancelled successfully.');
        this.GetBookingsList();     // refresh
        this.GetAvailability();
      },
      // Error handling 
      error: (error) => {
        // Store the backend error so it can be displayed in the UI
        this.cancellationError.set(
          typeof error.error === 'string'
            ? error.error
            : error.error?.detail ||
              error.error?.title ||
              'Failed to cancel appointment.'
        );

        // Refresh the list to show the actual appointment state
        this.GetBookingsList();
      }
    });
  }

  async searchPatient(civilId: string) {

      // Clear previous search results
      this.existingPatient.set(null);
      this.patientSearchStatus.set('idle');
      this.patientSearchError.set('');

      // Check Civil ID is valid
      if (!/^[0-9]{12}$/.test(civilId)) {
          this.patientSearchError.set('Civil ID must contain exactly 12 digits.');
          return;
      }

      try {
          // Search for patient using Civil ID
          const patient = await firstValueFrom(
            this.http.get<Patient>(`${API_BASE_URL}/patients/search?civilId=${civilId}`)
          );

          // Patient exists - save their information
          this.existingPatient.set(patient);
          this.patientSearchStatus.set('found');

      } catch (error) {
          if (error instanceof HttpErrorResponse && error.status === 404) {
              // Patient does not exist - allow registration
              this.patientSearchStatus.set('notFound');

          } else {
              // Any other error
              this.patientSearchError.set('Unable to search for patient.');
          }
      }
  }

  resetPatientSearch() {
      this.existingPatient.set(null);
      this.patientSearchStatus.set('idle');
      this.patientSearchError.set('');
  }
  
  StartReschedule(appointment: Appointment) {
    this.reschedule.set({
      appointmentId: appointment.id,
      doctorId: appointment.doctorId,
      date: appointment.startTime.split('T')[0],
      time: null
    });

    this.rescheduleSlots.set([]);
    this.rescheduleError.set('');
    this.rescheduleSuccess.set('');
  }

  GetRescheduleAvailability() {
    const details = this.reschedule();

    this.rescheduleError.set('');
    this.rescheduleSlots.set([]);

    if (!details || !details.date) {
      this.rescheduleError.set('Please select a doctor and date.');
      return;
    }

    // Find original appointment to retrieve its type
    const appointment = this.appointments().find(
      a => a.id === details.appointmentId
    );

    if (!appointment) {
      this.rescheduleError.set('Appointment not found.');
      return;
    }

    // Get availability for the new doctor/date
    this.http.get<TimeSlot[]>(
      `${API_BASE_URL}/appointments/availability?doctorId=${details.doctorId}&date=${details.date}&type=${appointment.type}`
    ).subscribe({

      next: (response) => {
        // Ignore a response if the form has changed
        const current = this.reschedule();

        if (!current ||
            current.appointmentId !== details.appointmentId ||
            current.doctorId !== details.doctorId ||
            current.date !== details.date) {
          return;
        }

        this.rescheduleSlots.set(response);
      },

      error: (error) => {
        const current = this.reschedule();

        if (!current ||
            current.appointmentId !== details.appointmentId ||
            current.doctorId !== details.doctorId ||
            current.date !== details.date) {
          return;
        }

        this.rescheduleError.set(
          typeof error.error === 'string'
            ? error.error
            : error.error?.title || 'Unable to retrieve availability.'
        );
      }
    });
  }

  UpdateRescheduleDoctor(doctorId: number) {
    this.reschedule.update(current =>
      current ? { ...current, doctorId, time: null } : null
    );

    this.rescheduleSlots.set([]);
    this.rescheduleError.set('');
  }

  UpdateRescheduleDate(date: string) {
    this.reschedule.update(current =>
      current ? { ...current, date, time: null } : null
    );

    this.rescheduleSlots.set([]);
    this.rescheduleError.set('');
  }

  SelectRescheduleTime(slot: TimeSlot) {
    if (!slot.available) {
      return;
    }

    this.reschedule.update(current =>
      current ? { ...current, time: slot.time } : null
    );
  }

  SaveReschedule() {
    const details = this.reschedule();

    this.rescheduleError.set('');

    if (!details || !details.date || !details.time) {
      this.rescheduleError.set('Please select a doctor, date and available time.');
      return;
    }

    if (this.savingReschedule()) {
      return;
    }

    this.savingReschedule.set(true);

    // Send PUT request to backend
    this.http.put<Appointment>(
      `${API_BASE_URL}/appointments/${details.appointmentId}`,
      {
        doctorId: details.doctorId,
        startTime: `${details.date}T${details.time}:00`
      }
    ).subscribe({

      // Rescheduling successful
      next: () => {
        this.savingReschedule.set(false);
        this.CloseReschedule();

        this.rescheduleSuccess.set('Appointment rescheduled successfully.');

        // Refresh appointment list and booking availability
        this.GetBookingsList();
        this.GetAvailability();
      },

      // Handle errors
      error: (error) => {
        this.savingReschedule.set(false);

        this.rescheduleError.set(
          typeof error.error === 'string'
            ? error.error
            : error.error?.title || 'Failed to reschedule appointment.'
        );
      }
    });
  }

  CloseReschedule() {
    this.reschedule.set(null);
    this.rescheduleSlots.set([]);
    this.rescheduleError.set('');
  }
}