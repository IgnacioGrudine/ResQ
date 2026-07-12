import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideArrowLeft, LucideLeaf, LucideFileText } from '@lucide/angular';

@Component({
  selector: 'app-terminos',
  standalone: true,
  imports: [RouterLink, LucideArrowLeft, LucideLeaf, LucideFileText],
  templateUrl: './terminos.component.html'
})
export class TerminosComponent {}
