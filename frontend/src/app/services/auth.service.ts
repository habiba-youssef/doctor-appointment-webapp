
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

// Structure of the response returned by /auth/login
export interface LoginResponse {
  token: string;
  role: 'Receptionist' | 'Doctor';
  doctorId: number | null;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);

  // Send username and password to the backend
  login(username: string, password: string) {
    return this.http.post<LoginResponse>(
      `${environment.apiUrl}/auth/login`,
      { username, password }
    );
  }

  // Save login details after successful authentication
  saveLogin(response: LoginResponse) {
    sessionStorage.setItem('token', response.token);
    sessionStorage.setItem('role', response.role);

    if (response.doctorId !== null) {
      sessionStorage.setItem('doctorId', response.doctorId.toString());
    } else {
      sessionStorage.removeItem('doctorId');
    }
  }

  // Retrieve the saved JWT
  getToken() {
    return sessionStorage.getItem('token');
  }

  // Retrieve the user's role
  getRole() {
    return sessionStorage.getItem('role');
  }

  // Retrieve the doctor's ID
  getDoctorId() {
    const id = sessionStorage.getItem('doctorId');
    return id !== null ? Number(id) : null;
  }

  // Log out
  logout() {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('role');
    sessionStorage.removeItem('doctorId');
  }
}
