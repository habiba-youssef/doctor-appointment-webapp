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
  availabilitySlots = signal<TimeSlot[]>([]); // to store time slots retrieved
  selectedDoctor = signal<Doctor | null>(null); // keeps track of which doctor is selected
  selectedDate = signal('');  // keeps track of date selected
  availabilityError = signal('');

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

  selectDoctor(doctor: Doctor) {
    this.selectedDoctor.set(doctor);
  }


  GetAvailability() {

    // 1. Get the selected doctor and date
    const doctor = this.selectedDoctor();
    const date = this.selectedDate();
    this.availabilityError.set('');


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
        console.log('Availability:', response);
      },

      // 5. Handle errors
      error: (error) => {
        this.availabilitySlots.set([]);

        this.availabilityError.set(
          typeof error.error === 'string'
            ? error.error
            : error.error?.title || 'Unable to retrieve availability.'
        );
        console.error('Error:', error);
      }

    });
  }

}