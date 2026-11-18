
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

// Protect receptionist pages
export const receptionistGuard: CanActivateFn = () => {

  const authService = inject(AuthService);
  const router = inject(Router);

  // Check if the user is logged in
  const token = authService.getToken();

  // Check if the user is a Receptionist
  const role = authService.getRole();

  if (token && role === 'Receptionist') {
    return true; // Allow access
  }

  // Otherwise, redirect to login
  return router.createUrlTree(['/login']);
};

// Protect doctor-specific pages
export const doctorGuard: CanActivateFn = (route) => {
  
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  const role = authService.getRole();
  const loggedInDoctorId = authService.getDoctorId();

  // Doctor ID requested in the URL
  const requestedDoctorId = Number(route.paramMap.get('id'));

  if (
    token &&
    role === 'Doctor' &&
    loggedInDoctorId !== null &&
    requestedDoctorId === loggedInDoctorId
  ) {
    return true;
  }

  return router.createUrlTree(['/login']);
};