import { Routes } from '@angular/router';
import { Receptionist } from './receptionist/receptionist';
import { Login } from './login/login';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  
  {
    path: 'login',
    component: Login
  },

  {
    path: 'receptionist',
    component: Receptionist
  }
];