import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from '../../services/theme-service';

@Component({
    selector: 'app-theme-toggle',
    standalone: true,
    imports: [CommonModule],
    template: `
    <button 
      (click)="themeService.toggleTheme()" 
      class="p-2 rounded-lg bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-100 transition-colors"
      [title]="themeService.theme() === 'light' ? 'Switch to Dark Mode' : 'Switch to Light Mode'"
    >
      @if (themeService.theme() === 'light') {
        <!-- Moon Icon -->
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z" />
        </svg>
      } @else {
        <!-- Sun Icon -->
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m0 13.5V21m8.966-8.966h-2.25M6.75 12H4.5m12.728-4.472l-1.591 1.591M7.864 16.136l-1.591 1.591m12.728 12.728l-1.591-1.591M7.864 7.864L6.273 6.273M12 9a3 3 0 110 6 3 3 0 010-6z" />
        </svg>
      }
    </button>
  `,
    styles: []
})
export class ThemeToggle {
    protected readonly themeService = inject(ThemeService);
}
