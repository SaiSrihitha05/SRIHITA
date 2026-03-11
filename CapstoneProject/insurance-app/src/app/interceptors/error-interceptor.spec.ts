import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { errorInterceptor } from './error-interceptor';

describe('errorInterceptor', () => {
    let httpMock: HttpTestingController;
    let httpClient: HttpClient;
    let router: Router;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptors([errorInterceptor])),
                provideHttpClientTesting(),
                {
                    provide: Router,
                    useValue: jasmine.createSpyObj('Router', ['navigate'])
                }
            ]
        });

        httpMock = TestBed.inject(HttpTestingController);
        httpClient = TestBed.inject(HttpClient);
        router = TestBed.inject(Router);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should redirect to /login on 401 and clear localStorage', () => {
        spyOn(localStorage, 'clear');
        httpClient.get('/test').subscribe({ error: () => { } });

        const req = httpMock.expectOne('/test');
        req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

        expect(localStorage.clear).toHaveBeenCalled();
        expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should redirect to /forbidden on 403', () => {
        httpClient.get('/test').subscribe({ error: () => { } });

        const req = httpMock.expectOne('/test');
        req.flush('Forbidden', { status: 403, statusText: 'Forbidden' });

        expect(router.navigate).toHaveBeenCalledWith(['/forbidden']);
    });

    it('should redirect to /not-found on 404', () => {
        httpClient.get('/test').subscribe({ error: () => { } });

        const req = httpMock.expectOne('/test');
        req.flush('Not Found', { status: 404, statusText: 'Not Found' });

        expect(router.navigate).toHaveBeenCalledWith(['/not-found']);
    });

    it('should redirect to /server-error on 500', () => {
        httpClient.get('/test').subscribe({ error: () => { } });

        const req = httpMock.expectOne('/test');
        req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });

        expect(router.navigate).toHaveBeenCalledWith(['/server-error']);
    });

    it('should NOT redirect on 400 (Bad Request)', () => {
        httpClient.get('/test').subscribe({ error: () => { } });

        const req = httpMock.expectOne('/test');
        req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });

        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should redirect to /generic-error on unhandled status codes', () => {
        httpClient.get('/test').subscribe({ error: () => { } });

        const req = httpMock.expectOne('/test');
        req.flush('Unhandled', { status: 418, statusText: 'I am a teapot' });

        expect(router.navigate).toHaveBeenCalledWith(['/generic-error']);
    });
});
