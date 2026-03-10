import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AdminSideBar } from '../admin-side-bar/admin-side-bar';


@Component({
  selector: 'app-admin-layout',
  imports: [RouterModule,AdminSideBar],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.css',
})
export class AdminLayout {

}
