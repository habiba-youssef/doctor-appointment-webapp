import { Routes } from '@angular/router';
import { Receptionist } from './receptionist/receptionist';
import { Login } from './login/login';
import { receptionistGuard } from './guards/auth.guard';

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
    component: Receptionist,
    canActivate: [receptionistGuard]
  }
];