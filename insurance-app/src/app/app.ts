import { Component, signal, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './components/navbar/navbar';
import { Footer } from './components/footer/footer';
import { ThemeService } from './services/theme-service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Footer],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('insurance-app');
  private readonly themeService = inject(ThemeService);

  get isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }
}
