import { Injectable, signal, effect } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class ThemeService {
    private readonly THEME_KEY = 'user-theme';

    // Signal to track the current theme
    theme = signal<'light' | 'dark'>(this.getInitialTheme());

    constructor() {
        // Effect to apply the theme class to the document element whenever it changes
        effect(() => {
            const currentTheme = this.theme();
            if (currentTheme === 'dark') {
                document.documentElement.classList.add('dark');
            } else {
                document.documentElement.classList.remove('dark');
            }
            localStorage.setItem(this.THEME_KEY, currentTheme);
        });
    }

    toggleTheme() {
        this.theme.update(current => current === 'light' ? 'dark' : 'light');
    }

    private getInitialTheme(): 'light' | 'dark' {
        const savedTheme = localStorage.getItem(this.THEME_KEY);
        if (savedTheme === 'light' || savedTheme === 'dark') {
            return savedTheme;
        }

        // Fallback to system preference
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
}
