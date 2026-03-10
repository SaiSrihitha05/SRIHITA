// src/app/guards/auth-guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('token');
  const userRole = localStorage.getItem('role');

  // 1. Check if the user is logged in
  if (!token) {
    router.navigate(['/login']);
    return false;
  }

  // 2. Check if the route requires a specific role
  const expectedRole = route.data['role'];

  // If the route has a role requirement and the user doesn't match it
  if (expectedRole && userRole !== expectedRole) {
    // Redirect to their own dashboard or home if they try to access the wrong area
    if (userRole === 'Admin') router.navigate(['/admin-dashboard']);
    else if (userRole === 'Agent') router.navigate(['/agent-dashboard']);
    else if (userRole === 'ClaimsOfficer') router.navigate(['/claims-officer-dashboard']);
    else router.navigate(['/customer-dashboard']);
    
    return false;
  }

  return true;
};