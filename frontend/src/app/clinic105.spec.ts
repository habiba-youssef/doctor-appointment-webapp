
import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { App } from './app';

describe('CLINIC-105', () => {

  it('should show an error and refresh after failed cancellation', () => {

    // ARRANGE: Create fake HTTP methods so we don't need the real backend
    const httpMock = {

      // Simulate a successful GET request returning an empty list
      get: vi.fn().mockReturnValue(of([])),

      // Simulate a failed DELETE request when cancelling an appointment
      delete: vi.fn().mockReturnValue(
        throwError(() => new HttpErrorResponse({
          status: 409, // HTTP 409 means the request conflicts with the current state
          statusText: 'Conflict',
          error: 'Appointment is already cancelled.' // Error returned by backend
        }))
      )
    };

    // Create a test environment and replace the real HttpClient with our fake one
    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        { provide: HttpClient, useValue: httpMock }
      ]
    });

    // Create the Angular component so we can test its methods and UI
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    // Add an appointment to simulate what the receptionist currently sees
    app.appointments.set([{
      id: 7,
      doctorId: 1,
      patientId: 1,
      startTime: '2030-01-06T10:00:00',
      isCancelled: false
    }]);

    // Simulate the receptionist clicking OK on the confirmation popup
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    // Clear previous GET calls so we only track requests made during cancellation
    httpMock.get.mockClear();

    // ACT: Attempt to cancel appointment 7
    app.CancelAppointment(7);

    // Update the HTML to reflect any changes in the component
    fixture.detectChanges();

    // ASSERT 1: Check that the error message is visible to the receptionist
    expect(fixture.nativeElement.textContent)
      .toContain('Appointment is already cancelled.');

    // ASSERT 2: Check that the frontend requested an updated appointments list
    expect(httpMock.get).toHaveBeenCalledWith(
      'http://localhost:5126/appointments'
    );

    // ASSERT 3: Check that the outdated appointment was removed after refreshing
    expect(app.appointments()).toEqual([]);

    // Restore mocked functions to their original behavior
    vi.restoreAllMocks();
  });
});
