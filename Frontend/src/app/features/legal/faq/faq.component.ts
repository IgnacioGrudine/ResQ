import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideArrowLeft, LucideLeaf, LucideMessageCircle, LucideChevronDown, LucideMail } from '@lucide/angular';

interface FaqItem { question: string; answer: string; }

const FAQ_ITEMS: FaqItem[] = [
  {
    question: '¿Qué es ResQ y cómo funciona?',
    answer: 'ResQ es una plataforma que conecta comercios gastronómicos con consumidores para vender excedentes de comida como "packs sorpresa" a precio reducido, cerca tuyo.'
  },
  {
    question: '¿Cómo me registro como consumidor?',
    answer: 'Desde "Crear cuenta", con tu email y una contraseña. No hace falta ninguna red social.'
  },
  {
    question: '¿Cómo me registro como comercio?',
    answer: 'Completando el registro de comercio con los datos de tu local y tu ubicación. Después conectás tu cuenta de Mercado Pago para poder empezar a cobrar.'
  },
  {
    question: 'Olvidé mi contraseña, ¿cómo la recupero?',
    answer: 'Desde "¿Olvidaste tu contraseña?" en el login, ingresás tu email y te mandamos un enlace de un solo uso, válido por 1 hora, para elegir una nueva. Si tu cuenta es solo con Google, iniciá sesión con Google en vez de restablecer.'
  },
  {
    question: '¿Cómo reservo y pago un pack?',
    answer: 'Elegís un pack en el mapa o el feed, lo pagás con Mercado Pago y recibís un código de retiro.'
  },
  {
    question: '¿Cómo retiro mi pack?',
    answer: 'Mostrás el código alfanumérico en el comercio, dentro de la franja horaria de retiro publicada.'
  },
  {
    question: '¿Puedo cancelar una reserva ya pagada?',
    answer: 'Sí. Podés cancelarla desde "Mis Órdenes" en cualquier momento antes de que cierre la franja horaria de retiro de ese pack, y te reembolsamos el pago completo. Una vez cerrada esa ventana, o si ya lo retiraste, no se puede cancelar.'
  },
  {
    question: '¿Qué medios de pago acepta ResQ?',
    answer: 'Todos los que ofrece Mercado Pago: tarjetas de crédito y débito, dinero en cuenta, y otros medios habilitados.'
  },
  {
    question: '¿ResQ cobra comisión?',
    answer: 'Sí, cobramos una comisión de plataforma al comercio por cada venta procesada. El consumidor paga solo el precio publicado del pack.'
  },
  {
    question: '¿Cómo dejo una reseña?',
    answer: 'Después de retirar un pack, la app te invita a calificar tu experiencia con el comercio.'
  }
];

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [RouterLink, LucideArrowLeft, LucideLeaf, LucideMessageCircle, LucideChevronDown, LucideMail],
  templateUrl: './faq.component.html'
})
export class FaqComponent {
  readonly items = FAQ_ITEMS;
  readonly openIndex = signal<number | null>(null);

  toggle(index: number): void {
    this.openIndex.update(current => (current === index ? null : index));
  }
}
