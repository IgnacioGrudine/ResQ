import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgComponentOutlet } from '@angular/common';
import { LucideLeaf, LucideSprout, LucideTrendingUp, LucideHandshake, LucideShoppingBag, LucideStore, LucideCheck, LucideIcon } from '@lucide/angular';
import { ResqButtonComponent } from '../../shared/ui';
import { ResqCardComponent } from '../../shared/ui';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, NgComponentOutlet, LucideLeaf, LucideShoppingBag, LucideStore, LucideCheck, ResqButtonComponent, ResqCardComponent],
  templateUrl: './landing.component.html'
})
export class LandingComponent {
  readonly steps = [
    {
      number: '1',
      title: 'Explorá',
      description: 'Encontrá comercios cercanos con packs disponibles para hoy.'
    },
    {
      number: '2',
      title: 'Comprá',
      description: 'Elegí tu pack sorpresa y pagá online de forma segura.'
    },
    {
      number: '3',
      title: 'Retirá',
      description: 'Presentá tu código QR en el comercio y llevate tu pack.'
    }
  ];

  readonly impacts: { icon: LucideIcon; title: string; description: string }[] = [
    {
      icon: LucideSprout,
      title: 'Ambiental',
      description: 'Reducimos el desperdicio alimenticio y la huella de carbono de cada plato descartado.'
    },
    {
      icon: LucideTrendingUp,
      title: 'Comercial',
      description: 'Los comercios recuperan costos de productos que de otro modo terminarían en la basura.'
    },
    {
      icon: LucideHandshake,
      title: 'Social',
      description: 'Más personas acceden a comida de calidad a precios justos.'
    }
  ];

  readonly merchantFeatures = [
    'Publicá packs sorpresa en minutos',
    'Pagos online con Mercado Pago',
    'Dashboard de métricas en tiempo real',
    'Validación de retiro por código QR'
  ];

  readonly stats = [
    { value: '500+', label: 'Packs rescatados' },
    { value: '30+', label: 'Comercios adheridos' },
    { value: '2 tn', label: 'CO₂ evitado' }
  ];
}
