import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);

    return next(req).pipe(
        catchError((error: HttpErrorResponse) => {
            switch (error.status) {
                case 401:
                    // Unauthorized — clear session and redirect to login
                    localStorage.clear();
                    router.navigate(['/login']);
                    break;

                case 403:
                    // Forbidden — user doesn't have permission
                    router.navigate(['/forbidden']);
                    break;

                case 404:
                    // Not Found — resource doesn't exist
                    router.navigate(['/not-found']);
                    break;

                case 500:
                case 502:
                case 503:
                    // Server errors
                    router.navigate(['/server-error']);
                    break;

                // For 400 (Bad Request) and 409 (Conflict)
                // let the calling component handle them with the error message
                case 400:
                case 409:
                    break;

                default:
                    // Any other unhandled status codes (e.g. 504, 405, etc.)
                    router.navigate(['/generic-error']);
                    break;
            }

            // Re-throw so individual components can still handle the error if needed
            return throwError(() => error);
        })
    );
};
