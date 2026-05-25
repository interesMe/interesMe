import { Component, signal } from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [NgClass],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home {

  activeSide = signal<
    'work' | 'relationship' | null
  >(null);

  setSide(
    side: 'work' | 'relationship' | null
  ) {
    this.activeSide.set(side);
  }

}