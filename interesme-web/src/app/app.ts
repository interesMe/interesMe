import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { PreferencesService } from './core/services/preferences.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html'
})
export class App {
  private readonly preferences = inject(PreferencesService);
}
