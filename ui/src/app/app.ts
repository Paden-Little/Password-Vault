import { Component } from '@angular/core';
import {LoginModal} from './components/login-modal/login-modal';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [LoginModal],
  templateUrl: './app.html', //Top level HTML template
  styleUrls: ['./app.css'], //Inject global default styles here from app.css
})
export class AppComponent {}
