// import { Component, signal } from '@angular/core';
// import { RouterOutlet } from '@angular/router';

// @Component({
//   imports: [RouterOutlet],
//   selector: 'app-root',
//   styleUrl: './app.css',
//   templateUrl: './app.html',
// })
// export class App {
//   protected readonly title = signal('frontend');
// }


import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private http = inject(HttpClient);

  backendMessage = signal('Connecting to backend...');

  constructor() {
    this.http.get('http://localhost:5126/status', {
      responseType: 'text'
    }).subscribe({
      next: (response) => {
        this.backendMessage.set(response);
      },
      error: () => {
        this.backendMessage.set('Backend connection failed');
      }
    });
  }
}