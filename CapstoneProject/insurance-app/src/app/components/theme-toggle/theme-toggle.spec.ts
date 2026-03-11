import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ThemeToggle } from './theme-toggle';
import { ThemeService } from '../../services/theme-service';
import { signal } from '@angular/core';

describe('ThemeToggle', () => {
    let component: ThemeToggle;
    let fixture: ComponentFixture<ThemeToggle>;
    let mockThemeService: any;

    beforeEach(async () => {
        mockThemeService = {
            theme: signal('light'),
            toggleTheme: jasmine.createSpy('toggleTheme')
        };

        await TestBed.configureTestingModule({
            imports: [ThemeToggle],
            providers: [
                { provide: ThemeService, useValue: mockThemeService }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(ThemeToggle);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should call toggleTheme on click', () => {
        const button = fixture.nativeElement.querySelector('button');
        button.click();
        expect(mockThemeService.toggleTheme).toHaveBeenCalled();
    });

    it('should show moon icon in light mode', () => {
        // Moon icon is shown when light
        mockThemeService.theme.set('light');
        fixture.detectChanges();
        const moonInTemplate = fixture.nativeElement.querySelector('svg');
        expect(moonInTemplate).toBeTruthy();
    });

    it('should have correct title based on theme', () => {
        mockThemeService.theme.set('light');
        fixture.detectChanges();
        const button = fixture.nativeElement.querySelector('button');
        expect(button.title).toBe('Switch to Dark Mode');

        mockThemeService.theme.set('dark');
        fixture.detectChanges();
        expect(button.title).toBe('Switch to Light Mode');
    });
});
