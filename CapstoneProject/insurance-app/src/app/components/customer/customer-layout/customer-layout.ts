import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CustomerSideBar } from '../customer-side-bar/customer-side-bar';

@Component({
  selector: 'app-customer-layout',
  imports: [RouterModule,CustomerSideBar],
  templateUrl: './customer-layout.html',
  styleUrl: './customer-layout.css',
})
export class CustomerLayout {

}
