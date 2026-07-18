import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideLeaf, LucideShoppingBag, LucideStore, LucideCheck } from '@lucide/angular';
import { ResqButtonComponent } from '../../shared/ui';
import { ResqCardComponent } from '../../shared/ui';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, LucideLeaf, LucideShoppingBag, LucideStore, LucideCheck, ResqButtonComponent, ResqCardComponent],
  templateUrl: './landing.component.html'
})
export class LandingComponent {
  /** The three problems ResQ solves at once, each grounded in a real photo, not an icon. */
  readonly pillars = [
    {
      eyebrow: 'Para vos',
      title: 'Comé mejor. Gastá menos.',
      description: 'Accedé a comida rica y en buen estado de los comercios que ya conocés, a un precio pensado para que valga la pena. Vos elegís qué pack comprar y cuándo.',
      imageUrl: 'https://images.unsplash.com/photo-1739290189330-82cf06bbf650?auto=format&fit=crop&w=800&q=80',
      imageAlt: 'Persona sosteniendo una bolsa con un pack sorpresa de comida'
    },
    {
      eyebrow: 'Para tu negocio',
      title: 'Lo que sobra, no se pierde.',
      description: 'Todos los días se descarta comida en buen estado que ya pagaste producir. Publicá esos excedentes en ResQ y convertilos en ingresos reales antes de que se conviertan en pérdida.',
      imageUrl: 'https://images.unsplash.com/photo-1753351052363-53ce102830eb?auto=format&fit=crop&w=800&q=80',
      imageAlt: 'Dueña de un comercio gastronómico sonriendo en su local'
    },
    {
      eyebrow: 'Para todos',
      title: 'Cada pack rescatado, menos desperdicio.',
      description: 'El desperdicio de alimentos es una de las principales causas de emisiones evitables. Cada compra en ResQ es comida que no termina en la basura.',
      imageUrl: 'https://images.unsplash.com/photo-1600659911670-7831fad053ee?auto=format&fit=crop&w=800&q=80',
      imageAlt: 'Hoja verde con gotas de agua'
    }
  ];

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
      description: 'Presentá tu código en el comercio y llevate tu pack.'
    }
  ];

  readonly merchantFeatures = [
    'Publicá packs sorpresa en minutos',
    'Pagos online con Mercado Pago',
    'Dashboard de métricas en tiempo real',
    'Validación de retiro con código'
  ];
}
