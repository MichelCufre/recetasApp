import { Routes } from '@angular/router';
import { DashboardComponent, IngredientsComponent, IngredientFormComponent, RecipesComponent, RecipeDetailComponent, RecipeFormComponent, CompareComponent, ProductionComponent, SimulatorComponent, SettingsComponent } from './pages/pages';

export const routes: Routes = [
  {path:'dashboard',component:DashboardComponent}, {path:'ingredientes',component:IngredientsComponent},
  {path:'ingredientes/nuevo',component:IngredientFormComponent}, {path:'ingredientes/:id',component:IngredientFormComponent},
  {path:'recetas',component:RecipesComponent}, {path:'recetas/nueva',component:RecipeFormComponent},
  {path:'recetas/:id/editar',component:RecipeFormComponent}, {path:'recetas/:id',component:RecipeDetailComponent},
  {path:'comparar',component:CompareComponent}, {path:'produccion',component:ProductionComponent},
  {path:'simulador',component:SimulatorComponent}, {path:'configuracion',component:SettingsComponent},
  {path:'',pathMatch:'full',redirectTo:'dashboard'}, {path:'**',redirectTo:'dashboard'}
];
