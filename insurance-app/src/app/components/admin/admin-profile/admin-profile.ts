import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../services/user-service';

@Component({
  selector: 'app-admin-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-profile.html'
})
export class AdminProfile implements OnInit {
  private userService = inject(UserService);
  private cdr = inject(ChangeDetectorRef);

  profile: any = { name: '', email: '', phone: '' };
  isEditing = false;
  loading = true;

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.userService.getProfile().subscribe({
      next: (data) => {
        this.profile = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error fetching profile', err)
    });
  }

  // admin-profile.ts
  onUpdate() {
    // Construct the DTO exactly as the backend expects
    const updateDto = {
      name: this.profile.name,
      phone: this.profile.phone,
      isActive: this.profile.isActive ?? true // Default to true if not provided
    };

    this.userService.updateProfile(updateDto).subscribe({
      next: (res) => {
        alert(res.message);
        this.isEditing = false;
        this.loadProfile(); // Refresh to ensure sync
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        alert('Update failed. Please check your inputs.');
      }
    });
  }
}