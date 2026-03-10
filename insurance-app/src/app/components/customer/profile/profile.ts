import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../services/user-service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.html'
})
export class Profile implements OnInit {
  private userService = inject(UserService);
  private cdr = inject(ChangeDetectorRef);

  userProfile: any = null;
  editData: any = {};
  isEditMode = false;
  loading = true;
  saving = false;

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.loading = true;
    this.userService.getProfile().subscribe({
      next: (data) => {
        this.userProfile = data;
        this.editData = { ...data }; // Clone data for editing
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error("Error loading profile", err);
        this.loading = false;
      }
    });
  }

  toggleEdit() {
    this.isEditMode = !this.isEditMode;
    if (!this.isEditMode) {
      this.editData = { ...this.userProfile }; // Reset changes if cancelled
    }
  }

  saveProfile() {
    this.saving = true;
    this.userService.updateProfile(this.editData).subscribe({
      next: (updated) => {
        this.userProfile = updated;
        this.isEditMode = false;
        this.saving = false;
        alert("Profile updated successfully!");
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.saving = false;
        alert(err.error?.message || "Failed to update profile");
      }
    });
  }
}