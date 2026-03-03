import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../services/user-service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.html'
})
export class AdminUsers implements OnInit {
  private userService = inject(UserService);
  private cdr = inject(ChangeDetectorRef);

  activeTab: 'Customer' | 'Agent' | 'ClaimsOfficer' = 'Customer';
  users: any[] = [];
  showModal = false;
  loading = false;

  // Shared form object for CreateAgentDto and CreateClaimsOfficerDto
  newUser = {
    name: '',
    email: '',
    password: '',
    phone: ''
  };

  ngOnInit() { this.switchTab('Customer'); }

  switchTab(tab: 'Customer' | 'Agent' | 'ClaimsOfficer') {
    this.activeTab = tab;
    this.loading = true; // Start loading
    this.users = []; // Clear old data immediately to avoid flickering

    const request = tab === 'Customer' ? this.userService.getCustomers() :
      tab === 'Agent' ? this.userService.getAgents() :
        this.userService.getClaimsOfficers();

    request.subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false; // Stop loading only when data arrives
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading users', err);
        this.loading = false; // Stop loading even on error
        this.cdr.detectChanges();
      }
    });
  }
  onSubmit() {
    const action = this.activeTab === 'Agent'
      ? this.userService.createAgent(this.newUser)
      : this.userService.createClaimsOfficer(this.newUser);

    action.subscribe({
      next: () => {
        alert(`${this.activeTab} created successfully!`);
        this.showModal = false;
        this.switchTab(this.activeTab);
        this.newUser = { name: '', email: '', password: '', phone: '' };
        this.cdr.detectChanges();
      },
      error: (err) => alert('Error creating user: ' + err.error?.title || 'Check fields')
    });
  }

  deleteUser(id: number) {
    if (confirm('Delete this user account?')) {
      this.userService.deleteUser(id).subscribe(() => {
        this.switchTab(this.activeTab);
        this.cdr.detectChanges();
      });
    }
  }
}