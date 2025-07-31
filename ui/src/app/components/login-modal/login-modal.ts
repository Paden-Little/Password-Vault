import { Component, EventEmitter, Output } from '@angular/core';
import {FormsModule} from '@angular/forms';

@Component({
  selector: 'app-login-modal',
  templateUrl: './login-modal.html',
  imports: [
    FormsModule
  ],
  styleUrls: ['./login-modal.css']
})
export class LoginModal {
  email = '';
  password = '';

  @Output() closeModal = new EventEmitter<void>();

  login() {
    // Replace with real auth logic
    console.log('Logging in:', { email: this.email, password: this.password });
    this.close();
  }

  close() {
    this.closeModal.emit();
  }
}
