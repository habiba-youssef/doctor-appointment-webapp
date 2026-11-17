import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StatusService {
  private http = inject(HttpClient);

  check() {
    return this.http.get(
      `${environment.apiUrl}/status`,
      { responseType: 'text' }
    );
  }
}