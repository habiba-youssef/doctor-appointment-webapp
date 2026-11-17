
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { AppointmentService } from '../services/appointment.service';
import {
  DoctorAppointment,
  AppointmentStatus,
  AppointmentType
} from '../models';

@Component({
  selector: 'app-doctor-dashboard',
  imports: [],
  templateUrl: './doctor.html',
  styleUrl: './doctor.css'
})
export class DoctorDashboard {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private appointmentService = inject(AppointmentService);

  // Doctor ID from /doctor/:id
  doctorId = Number(this.route.snapshot.paramMap.get('id'));

  // Today's date in Kuwait (UTC+3)
  selectedDate = signal(
    new Date(Date.now() + 3 * 60 * 60 * 1000)
      .toISOString()
      .slice(0, 10)
  );

  appointments = signal<DoctorAppointment[]>([]);
  loading = signal(false);
  savingId = signal<number | null>(null);
  error = signal('');
  success = signal('');

  AppointmentStatus = AppointmentStatus;

  constructor() {
    this.loadAppointments();
  }

  // Retrieve this doctor's appointments for the selected day
  loadAppointments() {
    this.loading.set(true);
    this.error.set('');

    this.appointmentService
      .getDoctorAppointments(this.doctorId, this.selectedDate())
      .subscribe({
        next: (response) => {
          this.appointments.set(response);
          this.loading.set(false);
        },
        error: (error) => {
          this.loading.set(false);
          this.error.set(
            error.error?.detail ||
            error.error?.title ||
            'Unable to load appointments.'
          );
        }
      });
  }

  // Update an appointment's status
  updateStatus(appointmentId: number, status: AppointmentStatus) {
    if (this.savingId() !== null) return;

    this.savingId.set(appointmentId);
    this.error.set('');
    this.success.set('');

    this.appointmentService
      .updateDoctorStatus(this.doctorId, appointmentId, status)
      .subscribe({
        next: () => {
          this.savingId.set(null);
          this.loadAppointments();
          this.success.set('Appointment status updated successfully.');
        },
        error: (error) => {
          this.savingId.set(null);
          this.error.set(
            error.error?.detail ||
            error.error?.title ||
            'Failed to update appointment status.'
          );
        }
      });
  }

  // Display appointment type
  getTypeName(type: AppointmentType): string {
    switch (type) {
      case AppointmentType.Consultation:
        return 'Consultation';
      case AppointmentType.FollowUp:
        return 'Follow-up';
      case AppointmentType.Procedure:
        return 'Procedure';
      default:
        return 'Unknown';
    }
  }

  // Display appointment status
  getStatusName(status: AppointmentStatus): string {
    switch (status) {
      case AppointmentStatus.Booked:
        return 'Booked';
      case AppointmentStatus.Completed:
        return 'Completed';
      case AppointmentStatus.NoShow:
        return 'No-show';
      default:
        return 'Unknown';
    }
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
