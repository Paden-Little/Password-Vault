import {inject, Injectable} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface CreateAccountRequest {
  email: string;
  password: string;
  username: string;
}

export interface AuthResponse {
  token: string;
  userId: number;
  username: string;
}

@Injectable()
export class AuthService {
  private baseUrl = 'http://localhost:8080/auth';

  http = inject(HttpClient);
  constructor() {}

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, credentials);
  }

  createAccount(data: CreateAccountRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/create`, data);
  }
}
