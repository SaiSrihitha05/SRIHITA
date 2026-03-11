import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Forbidden } from './forbidden';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';

describe('Forbidden', () => {
    let component: Forbidden;
    let fixture: ComponentFixture<Forbidden>;
    let router: Router;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [Forbidden],
            providers: [provideRouter([])]
        }).compileComponents();

        fixture = TestBed.createComponent(Forbidden);
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

    it('should go back on goBack()', () => {
        spyOn(window.history, 'back');
        component.goBack();
        expect(window.history.back).toHaveBeenCalled();
    });
});
