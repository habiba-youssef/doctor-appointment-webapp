
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Doctor } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DoctorService {
  private http = inject(HttpClient);

  // Retrieve all doctors
  getAll() {
    return this.http.get<Doctor[]>(
      `${environment.apiUrl}/doctors`
    );
  }
}
