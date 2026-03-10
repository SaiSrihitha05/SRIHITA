import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ClaimsOfficerSideBar } from '../claims-officer-side-bar/claims-officer-side-bar';

@Component({
  selector: 'app-claims-officer-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, ClaimsOfficerSideBar],
  templateUrl: './claims-officer-layout.html',
  styleUrl: './claims-officer-layout.css'
})
export class ClaimsOfficerLayout {}