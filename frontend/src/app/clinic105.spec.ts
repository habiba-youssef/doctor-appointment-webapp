
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { Receptionist } from './receptionist/receptionist';
import { AppointmentService } from './services/appointment.service';
import { DoctorService } from './services/doctor.service';
import { PatientService } from './services/patient.service';
import { StatusService } from './services/status.service';
import { AppointmentType } from './models';

describe('CLINIC-105', () => {

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should show an error and refresh after failed cancellation', async () => {

    // ARRANGE 1: Simulate the backend returning a 409 Conflict
    const appointmentMock = {
      getAll: vi.fn().mockReturnValue(of([])),

      cancel: vi.fn().mockReturnValue(
        throwError(() => new HttpErrorResponse({
          status: 409,
          statusText: 'Conflict',
          error: {
            title: 'Appointment already cancelled',
            status: 409,
            detail: 'Appointment is already cancelled.'
          }
        }))
      )
    };

    // Mock the other services used by Receptionist
    const doctorMock = {
      getAll: vi.fn().mockReturnValue(of([]))
    };

    const patientMock = {
      getAll: vi.fn().mockReturnValue(of([]))
    };

    const statusMock = {
      check: vi.fn().mockReturnValue(of('Backend is working!'))
    };

    // ARRANGE 2: Create the Angular testing environment
    await TestBed.configureTestingModule({
      imports: [Receptionist],
      providers: [
        { provide: AppointmentService, useValue: appointmentMock },
        { provide: DoctorService, useValue: doctorMock },
        { provide: PatientService, useValue: patientMock },
        { provide: StatusService, useValue: statusMock }
      ]
    }).compileComponents();

    // Create the actual Receptionist component
    const fixture = TestBed.createComponent(Receptionist);
    const receptionist = fixture.componentInstance;

    // ARRANGE 3: Simulate an existing appointment
    receptionist.appointments.set([{
      id: 7,
      doctorId: 1,
      patientId: 1,
      startTime: '2030-01-06T10:00:00',
      type: AppointmentType.Consultation,
      isCancelled: false
    }]);

    fixture.detectChanges();

    // Simulate entering a cancellation reason
    vi.spyOn(window, 'prompt').mockReturnValue('Patient requested cancellation');

    // ACT: Click the Cancel button
    const buttons = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;

    const cancelButton = Array.from(buttons).find(button => button.textContent?.trim() === 'Cancel');

    expect(cancelButton).toBeDefined();

    cancelButton!.click();

    // Update the HTML after the cancellation attempt
    fixture.detectChanges();

    // ASSERT 1: Cancellation request included the reason
    expect(appointmentMock.cancel).toHaveBeenCalledWith(7, 'Patient requested cancellation');

    // ASSERT 2: Error message is visible to the receptionist
    expect(fixture.nativeElement.textContent).toContain('Appointment is already cancelled.');

    // ASSERT 3: Appointments were refreshed after failure
    // One initial load + one refresh after failed cancellation
    expect(appointmentMock.getAll).toHaveBeenCalledTimes(2);

    // ASSERT 4: The outdated appointment was removed
    expect(receptionist.appointments()).toEqual([]);
  });
});
