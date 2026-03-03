import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../services/user-service';

@Component({
  selector: 'app-claims-officer-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.html'
})
export class ClaimsOfficerProfile implements OnInit {
  private userService = inject(UserService);
  private cdr = inject(ChangeDetectorRef);

  profile: any = { name: '', email: '', phone: '', isActive: true };
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
      error: (err) => {
        console.error('Error fetching profile', err);
        this.loading = false;
      }
    });
  }

  onUpdate() {
    // Backend DTO: { name: string, phone: string, isActive: boolean }
    const updateDto = {
      name: this.profile.name,
      phone: this.profile.phone,
      isActive: this.profile.isActive
    };

    this.userService.updateProfile(updateDto).subscribe({
      next: (res) => {
        alert(res.message);
        this.isEditing = false;
        this.loadProfile();
        this.cdr.detectChanges();
      },
      error: (err) => alert('Update failed: ' + err.error?.message)
    });
  }
}