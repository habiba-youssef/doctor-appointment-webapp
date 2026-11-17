import { Routes } from '@angular/router';
import { Receptionist } from './receptionist/receptionist';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'receptionist',
    pathMatch: 'full'
  },
  {
    path: 'receptionist',
    component: Receptionist
  }
];