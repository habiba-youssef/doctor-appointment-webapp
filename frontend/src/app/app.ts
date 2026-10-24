import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http'; // Allows Angular to send HTTP requests to your backend API.

interface Doctor {
  id: number;
  firstName: string;
  lastName: string;
  specialty: string;
  gender: number;
  consultationFee: number;
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
  doctors = signal<Doctor[]>([]);
  selectedDoctor = signal<Doctor | null>(null); // keeps track of which doctor is selected

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

}