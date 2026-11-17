
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  imports: [],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  loginError = signal('');
  loggingIn = signal(false);

  login(username: string, password: string) {
    this.loginError.set('');
    this.loggingIn.set(true);

    this.authService.login(username, password)
      .subscribe({
        next: (response) => {
          this.loggingIn.set(false);
          this.authService.saveLogin(response);

          // Redirect based on role
          if (response.role === 'Receptionist') {
            this.router.navigate(['/receptionist']);
          } else if (response.role === 'Doctor') {
            this.router.navigate(['/doctor', response.doctorId]);
          }
        },

        error: (error) => {
          this.loggingIn.set(false);
          this.loginError.set(
            error.error?.detail ||
            error.error?.title ||
            'Login failed. Please check your credentials.'
          );
        }
      });
  }
}
