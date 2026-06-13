import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AppHeader } from './components/app-header/app-header';
import { AppSidebar } from './components/app-sidebar/app-sidebar';

@Component({
  selector: 'app-app-layout',
  imports: [RouterOutlet, AppHeader, AppSidebar],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
})
export class AppLayout {}
