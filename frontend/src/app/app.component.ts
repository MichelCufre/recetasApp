import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `<div class="shell">
    <aside><a class="brand" routerLink="/dashboard"><span>B</span><div>BarFlow<small>Nutrición & rentabilidad</small></div></a>
      <nav>
        <a *ngFor="let n of nav" [routerLink]="n.url" routerLinkActive="active">{{n.label}}</a>
      </nav><div class="aside-foot"><div class="avatar">MC</div><div><b>Mi producción</b><small>UYU · Uruguay</small></div></div>
    </aside>
    <main><header><div><b>{{title}}</b><small>Control de barritas artesanales</small></div><span class="live"><i></i> Datos actualizados</span></header><section class="page"><router-outlet/></section></main>
  </div>`,
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'BarFlow';
  nav=[
    {url:'/dashboard',label:'Dashboard'}, {url:'/ingredientes',label:'Ingredientes'},
    {url:'/recetas',label:'Recetas'}, {url:'/comparar',label:'Comparar'},
    {url:'/produccion',label:'Producción'}, {url:'/ventas',label:'Ventas'}, {url:'/simulador',label:'Simulador'},
    {url:'/configuracion',label:'Configuración'}];
}
