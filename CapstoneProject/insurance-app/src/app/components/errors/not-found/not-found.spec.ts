import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotFound } from './not-found';
import { Router } from '@angular/router';
import { provideRouter } from '@angular/router';

describe('NotFound', () => {
    let component: NotFound;
    let fixture: ComponentFixture<NotFound>;
    let router: Router;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [NotFound],
            providers: [provideRouter([])]
        }).compileComponents();

        fixture = TestBed.createComponent(NotFound);
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
