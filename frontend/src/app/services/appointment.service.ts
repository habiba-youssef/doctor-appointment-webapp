
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { Appointment, AppointmentType, TimeSlot } from '../models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AppointmentService {
  private http = inject(HttpClient);

  // Retrieve appointments, optionally filtered by doctor and date
  getAll(doctorId?: number, date?: string) {
    let params = new HttpParams();

    if (doctorId !== undefined) {
      params = params.set('doctorId', doctorId);
    }

    if (date) {
      params = params.set('date', date);
    }

    return this.http.get<Appointment[]>(
      `${environment.apiUrl}/appointments`,
      { params }
    );
  }

  // Retrieve available appointment slots
  getAvailability(doctorId: number, date: string, type: AppointmentType) {
    return this.http.get<TimeSlot[]>(
      `${environment.apiUrl}/appointments/availability`,
      {
        params: {
          doctorId,
          date,
          type
        }
      }
    );
  }

  // Book a new appointment
  create(appointment: {
    doctorId: number;
    patientId: number;
    startTime: string;
    type: AppointmentType;
  }) {
    return this.http.post<Appointment>(
      `${environment.apiUrl}/appointments`,
      appointment
    );
  }

  // Cancel an appointment
  cancel(id: number, reason: string) {
    return this.http.delete<void>(
      `${environment.apiUrl}/appointments/${id}`,
      { body: { reason } }
    );
  }

  // Reschedule an appointment
  reschedule(id: number, doctorId: number, startTime: string) {
    return this.http.put<Appointment>(
      `${environment.apiUrl}/appointments/${id}`,
      { doctorId, startTime }
    );
  }
}
