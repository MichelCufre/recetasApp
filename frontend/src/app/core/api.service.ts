import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export const API_URL = 'http://localhost:5212/api';

export interface Nutrition { kcal:number; protein:number; carbohydrates:number; sugars:number; fiber:number; totalFat:number; saturatedFat:number; transFat:number; sodiumMg:number; cholesterolMg:number; polyols:number; sourceType:string; source?:string; sourceUrl?:string }
export interface Ingredient { id:string; name:string; category:string; brand?:string; supplier?:string; purchaseUnit:string; packageQuantity:number; netPrice:number; vatPercent:number; grossPrice:number; baseUnit:string; costPerBaseUnit:number; densityGPerMl?:number; priceUpdatedAt:string; notes?:string; active:boolean; stockCurrent:number; stockMinimum:number; nutrition:Nutrition }
export interface RecipeSummary { id:string; name:string; flavor?:string; bars:number; costPerBar:number; proteinPerBar:number; kcalPerBar:number; directPrice?:number; gymPrice?:number; bestMargin:number; active:boolean }
export interface RecipeDetail { recipe:any; analysis:any; profitability:any; alerts:string[] }
export interface Dashboard { ingredientCount:number; recipeCount:number; averageCost:number; averagePrice:number; averageMargin:number; mostProfitable?:string; mostExpensive?:string; highestProtein?:string; lowestCalories?:string; recipes:RecipeSummary[]; costDistribution:any[] }
export interface Batch { id:string; recipeId:string; recipeName:string; producedAt:string; actualBars:number; discardedBars:number; actualWeight:number; totalCost:number; costPerGoodBar:number; wastePercent:number }

@Injectable({providedIn:'root'})
export class ApiService {
  constructor(private http:HttpClient) {}
  ingredients(search=''):Observable<Ingredient[]> { return this.http.get<Ingredient[]>(`${API_URL}/ingredients`, {params:new HttpParams().set('search',search)}); }
  ingredient(id:string):Observable<Ingredient> { return this.http.get<Ingredient>(`${API_URL}/ingredients/${id}`); }
  saveIngredient(value:any,id?:string):Observable<Ingredient> { return id?this.http.put<Ingredient>(`${API_URL}/ingredients/${id}`,value):this.http.post<Ingredient>(`${API_URL}/ingredients`,value); }
  recipes():Observable<RecipeSummary[]> { return this.http.get<RecipeSummary[]>(`${API_URL}/recipes`); }
  recipe(id:string):Observable<RecipeDetail> { return this.http.get<RecipeDetail>(`${API_URL}/recipes/${id}`); }
  saveRecipe(value:any,id?:string):Observable<RecipeDetail> { return id?this.http.put<RecipeDetail>(`${API_URL}/recipes/${id}`,value):this.http.post<RecipeDetail>(`${API_URL}/recipes`,value); }
  compare(ids:string[]):Observable<RecipeDetail[]> { return this.http.get<RecipeDetail[]>(`${API_URL}/recipes/compare`,{params:ids.reduce((p,id)=>p.append('ids',id),new HttpParams())}); }
  dashboard():Observable<Dashboard> { return this.http.get<Dashboard>(`${API_URL}/dashboard`); }
  batches():Observable<Batch[]> { return this.http.get<Batch[]>(`${API_URL}/production-batches`); }
  createBatch(value:any):Observable<Batch> { return this.http.post<Batch>(`${API_URL}/production-batches`,value); }
  simulate(lines:any[]):Observable<any> { return this.http.post(`${API_URL}/simulator`,lines); }
  settings():Observable<any> { return this.http.get(`${API_URL}/settings`); }
  saveSettings(value:any):Observable<any> { return this.http.put(`${API_URL}/settings`,value); }
}
