import { Component } from '@angular/core';
import {LoginModal} from './components/login-modal/login-modal';
import {CreateAccountModal} from './components/create-modal/create-modal';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [LoginModal, CreateAccountModal],
  templateUrl: './app.html', //Top level HTML template
  styleUrls: ['./app.css'], //Inject global default styles here from app.css
})
export class AppComponent {}
