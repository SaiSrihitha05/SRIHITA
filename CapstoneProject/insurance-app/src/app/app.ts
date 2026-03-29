import { Component, signal, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './components/navbar/navbar';
import { Footer } from './components/footer/footer';
import { Chatbot } from './components/chatbot/chatbot';
import { ThemeService } from './services/theme-service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Footer, Chatbot],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {

  protected readonly title = signal('insurance-app');
  private readonly themeService = inject(ThemeService);

  get isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  get userRole(): string | null {
    return localStorage.getItem('role');
  }
}
