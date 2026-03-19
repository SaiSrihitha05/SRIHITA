import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { Register } from './components/register/register';
import { authGuard } from './guards/auth-guard';
import { PolicyDetails } from './components/customer/policy-details/policy-details';
import { FileClaim } from './components/customer/file-claim/file-claim';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/home/home')
        .then(m => m.Home)
  },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  {
    path: 'forgot-password',
    loadComponent: () => import('./components/auth/forgot-password/forgot-password')
      .then(m => m.ForgotPassword)
  },

  {
    path: 'reset-password',
    loadComponent: () => import('./components/auth/reset-password/reset-password')
      .then(m => m.ResetPassword)
  },
  {
    path: 'admin-dashboard',
    loadComponent: () => import('./components/admin/admin-layout/admin-layout').then(m => m.AdminLayout),
    canActivate: [authGuard],
    data: { role: 'Admin' },
    children: [
      {
        path: '',
        loadComponent: () => import('./components/admin/dashboard/dashboard').then(m => m.AdminDashboard)
      },
      {
        path: 'plans',
        loadComponent: () => import('./components/admin/admin-plans/admin-plans').then(m => m.AdminPlans)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./components/customer/notification-center/notification-center').then(m => m.NotificationCenter)
      },
      {
        path: 'policies',
        loadComponent: () => import('./components/admin/admin-policies/admin-policies').then(m => m.AdminPolicies)
      },
      {
        path: 'claims',
        loadComponent: () => import('./components/admin/admin-claims/admin-claims').then(m => m.AdminClaims)
      },
      {
        path: 'users',
        loadComponent: () => import('./components/admin/admin-users/admin-users').then(m => m.AdminUsers)
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/admin/admin-profile/admin-profile').then(m => m.AdminProfile)
      },
      {
        path: 'loans',
        loadComponent: () => import('./components/admin/admin-loans/admin-loans').then(m => m.AdminLoans)
      }
    ]
  },
  {
    path: 'agent-dashboard',
    loadComponent: () => import('./components/agent/agent-layout/agent-layout').then(m => m.AgentLayout),
    canActivate: [authGuard],
    data: { role: 'Agent' },
    children: [
      {
        path: '',
        loadComponent: () => import('./components/agent/dashboard/dashboard').then(m => m.AgentDashboard)
      },
      {
        path: 'my-policies',
        loadComponent: () => import('./components/agent/agent-policies/agent-policies').then(m => m.AgentPolicies)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./components/customer/notification-center/notification-center').then(m => m.NotificationCenter)
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/agent/agent-profile/agent-profile').then(m => m.AgentProfile)
      },
      {
        path: 'explore-plans',
        loadComponent: () => import('./components/agent/agent-plans/agent-plans').then(m => m.AgentPlans)
      },
      {
        path: 'commissions',
        loadComponent: () => import('./components/agent/agent-earnings/agent-earnings').then(m => m.AgentEarnings)
      }
    ]
  },
  {
    path: 'claims-officer-dashboard',
    loadComponent: () => import('./components/claims-officer/claims-officer-layout/claims-officer-layout').then(m => m.ClaimsOfficerLayout),
    canActivate: [authGuard],
    data: { role: 'ClaimsOfficer' },
    children: [
      { path: '', loadComponent: () => import('./components/claims-officer/dashboard/dashboard').then(m => m.ClaimsOfficerDashboard) },
      { path: 'my-claims', loadComponent: () => import('./components/claims-officer/claims-officer-claims/claims-officer-claims').then(m => m.ClaimsOfficerClaims) },
      {
        path: 'notifications',
        loadComponent: () => import('./components/customer/notification-center/notification-center').then(m => m.NotificationCenter)
      },
      { path: 'profile', loadComponent: () => import('./components/claims-officer/profile/profile').then(m => m.ClaimsOfficerProfile) }
    ]
  },
  {
    path: 'customer-dashboard',
    loadComponent: () => import('./components/customer/customer-layout/customer-layout').then(m => m.CustomerLayout),
    canActivate: [authGuard],
    data: { role: 'Customer' },
    children: [
      {
        path: '',
        loadComponent: () => import('./components/customer/dashboard/dashboard').then(m => m.CustomerDashboard)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./components/customer/notification-center/notification-center').then(m => m.NotificationCenter)
      },
      {
        path: 'explore-plans',
        loadComponent: () => import('./components/customer/explore-plans/explore-plans').then(m => m.ExplorePlans)
      },
      {
        path: 'buy-policy',
        loadComponent: () => import('./components/customer/buy-policy/buy-policy').then(m => m.BuyPolicy)
      },
      {
        path: 'my-policies',
        loadComponent: () => import('./components/customer/my-policies/my-policies').then(m => m.MyPolicies)
      },
      {
        path: 'pay-premium',
        loadComponent: () => import('./components/customer/pay-premium/pay-premium').then(m => m.PayPremium)
      },
      {
        path: 'payment-history',
        loadComponent: () => import('./components/customer/payment-history/payment-history').then(m => m.PaymentHistory)
      },
      {
        path: 'my-claims',
        loadComponent: () => import('./components/customer/my-claims/my-claims').then(m => m.MyClaims)
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/customer/profile/profile').then(m => m.Profile)
      },
      { path: 'policy-details/:id', component: PolicyDetails },
      {
        path: 'file-claim',
        component: FileClaim
      },
      {
        path: 'my-loans',
        loadComponent: () => import('./components/customer/my-loans/my-loans').then(m => m.MyLoans)
      },
      {
        path: 'apply-loan',
        loadComponent: () => import('./components/customer/apply-loan/apply-loan').then(m => m.ApplyLoan)
      },
      {
        path: 'repay-loan/:id',
        loadComponent: () => import('./components/customer/repay-loan/repay-loan').then(m => m.RepayLoan)
      },
      {
        path: 'help',
        loadComponent: () => import('./components/customer/customer-help/customer-help').then(m => m.CustomerHelp)
      }
    ]
  },
  // --- Error Pages ---
  { path: 'not-found', loadComponent: () => import('./components/errors/not-found/not-found').then(m => m.NotFound) },
  { path: 'forbidden', loadComponent: () => import('./components/errors/forbidden/forbidden').then(m => m.Forbidden) },
  { path: 'server-error', loadComponent: () => import('./components/errors/server-error/server-error').then(m => m.ServerError) },
  { path: 'generic-error', loadComponent: () => import('./components/errors/generic-error/generic-error').then(m => m.GenericError) },

  {
    path: '**',
    redirectTo: 'not-found'
  }
];