import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { trigger, transition, style, animate } from '@angular/animations';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
  animations: [
    trigger('routeAnim', [
      transition('false => true', [
        style({ opacity: 0, transform: 'translateY(12px)' }),
        animate('350ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class App {}
