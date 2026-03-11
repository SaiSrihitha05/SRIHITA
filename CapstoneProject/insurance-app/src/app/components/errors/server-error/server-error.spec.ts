import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ServerError } from './server-error';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';

describe('ServerError', () => {
    let component: ServerError;
    let fixture: ComponentFixture<ServerError>;
    let router: Router;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ServerError],
            providers: [provideRouter([])]
        }).compileComponents();

        fixture = TestBed.createComponent(ServerError);
        component = fixture.componentInstance;
        router = TestBed.inject(Router);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should have a retry method', () => {
        expect(component.retry).toBeDefined();
        expect(typeof component.retry).toBe('function');
    });
});
