import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http'; // Allows Angular to send HTTP requests to your backend API.

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})


// /// App class
// export class App {
//   private http = inject(HttpClient);

//   BEstatus = signal('Fetching data...');


//   constructor() {
//     this.http.get('http://localhost:5126/status', {
//       responseType: 'text'
//     }).subscribe({
//       next: (response) => {
//         this.BEstatus.set(response);
//       },


//       // if signal connection not made, means backend not running
//       error: () => {
//         this.BEstatus.set('Backend connection failed');
//       }
//     });
//   }
// }



export class App {

  private http = inject(HttpClient);
  // Stores the current backend status
  backendStatus = signal<'checking' | 'online' | 'offline'>('checking');

  constructor() {
    this.checkBackend();
  }

  checkBackend() {
    this.backendStatus.set('checking');

    this.http.get('http://localhost:5126/status', {
      responseType: 'text'
    }).subscribe({
      next: () => {
        this.backendStatus.set('online');
      },
      // error: () => {
      //   this.backendStatus.set('offline');
      error: (error) => {
        console.error('Backend status check failed:', error);
        this.backendStatus.set('offline');
// }
      }
    });
  }
}