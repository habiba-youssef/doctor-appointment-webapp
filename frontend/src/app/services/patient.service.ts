
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Patient } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private http = inject(HttpClient);

  // Retrieve registered patients
  getAll() {
    return this.http.get<Patient[]>(
      `${environment.apiUrl}/patients`
    );
  }

  // Search by Civil ID
  search(civilId: string) {
    return this.http.get<Patient>(
      `${environment.apiUrl}/patients/search?civilId=${encodeURIComponent(civilId)}`
    );
  }

  // Register a new patient
  create(patient: Patient) {
    return this.http.post<Patient>(
      `${environment.apiUrl}/patients`,
      patient
    );
  }
}
