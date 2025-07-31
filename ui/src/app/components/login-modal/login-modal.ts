import {Component, EventEmitter, inject, Output} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {AuthService} from '../../services/auth';
import {LoginRequest} from '../../services/auth';

@Component({
  selector: 'app-login-modal',
  templateUrl: './login-modal.html',
  imports: [
    FormsModule
  ],
  styleUrls: ['./login-modal.css'],
  providers: [AuthService]
})
export class LoginModal {
  email = '';
  password = '';
  auth = inject(AuthService);
  @Output() closeModal = new EventEmitter<void>();

  login() {
    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: (res) => {
        console.log('Logged in:', res);
        this.close();
      },
      error: (err) => {
        console.error('Login failed:', err);
      }
    });
  }


  close() {
    this.closeModal.emit();
  }
}
