import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GenericError } from './generic-error';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';

describe('GenericError', () => {
    let component: GenericError;
    let fixture: ComponentFixture<GenericError>;
    let router: Router;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [GenericError],
            providers: [provideRouter([])]
        }).compileComponents();

        fixture = TestBed.createComponent(GenericError);
        component = fixture.componentInstance;
        router = TestBed.inject(Router);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should navigate to home on goHome()', () => {
        const navSpy = spyOn(router, 'navigate');
        component.goHome();
        expect(navSpy).toHaveBeenCalledWith(['/']);
    });
});
