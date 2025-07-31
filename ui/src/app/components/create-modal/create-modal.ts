import { Component, EventEmitter, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-create-account-modal',
  templateUrl: './create-modal.html',
  styleUrls: ['../login-modal/login-modal.css'],
  standalone: true,
  imports: [FormsModule],
  providers: [AuthService]
})
export class CreateAccountModal {
  email = '';
  password = '';
  username = '';

  auth = inject(AuthService);

  @Output() closeModal = new EventEmitter<void>();

  createAccount() {
    console.log(this.email, this.password, this.username);
    this.auth.createAccount({
      email: this.email,
      password: this.password,
      username: this.username
    }).subscribe({
      next: (res) => {
        console.log('Account created:', res);
        this.close();
      },
      error: (err) => {
        console.error('Account creation failed:', err);
      }
    });
  }

  close() {
    this.closeModal.emit();
  }
}
