using BarFlow.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace BarFlow.Api.Controllers;

[ApiController,Route("api/ingredients")]
public sealed class IngredientsController(IIngredientService service):ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<IngredientView>> List([FromQuery]string? search,[FromQuery]bool includeInactive,CancellationToken ct)=>service.List(search,includeInactive,ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<IngredientView>> Get(Guid id,CancellationToken ct)=>(await service.Get(id,ct)) is { } x?Ok(x):NotFound();
    [HttpPost] public async Task<ActionResult<IngredientView>> Create(IngredientInput input,CancellationToken ct){var x=await service.Create(input,ct);return CreatedAtAction(nameof(Get),new{id=x.Id},x);}
    [HttpPut("{id:guid}")] public async Task<ActionResult<IngredientView>> Update(Guid id,IngredientInput input,CancellationToken ct)=>(await service.Update(id,input,ct)) is { } x?Ok(x):NotFound();
    [HttpGet("{id:guid}/price-history")] public Task<IReadOnlyList<BarFlow.Domain.IngredientPriceHistory>> History(Guid id,CancellationToken ct)=>service.PriceHistory(id,ct);
    [HttpGet("export.csv")] public async Task<IActionResult> Export(CancellationToken ct){var rows=await service.List(null,true,ct);var csv=new StringBuilder("Nombre;Categoría;Proveedor;Precio neto;IVA;Costo base;Unidad base;Proteína;Kcal\n");foreach(var x in rows)csv.AppendLine($"{x.Name};{x.Category};{x.Supplier};{x.NetPrice};{x.VatPercent};{x.CostPerBaseUnit};{x.BaseUnit};{x.Nutrition.Protein};{x.Nutrition.Kcal}");return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),"text/csv","ingredientes.csv");}
}

[ApiController,Route("api/recipes")]
public sealed class RecipesController(IRecipeService service):ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<RecipeSummary>> List(CancellationToken ct)=>service.List(ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<RecipeDetail>> Get(Guid id,CancellationToken ct)=>(await service.Get(id,ct)) is { } x?Ok(x):NotFound();
    [HttpGet("{id:guid}/cost")] public async Task<ActionResult<object>> Cost(Guid id,CancellationToken ct)=>(await service.Get(id,ct)) is { } x?Ok(x.Analysis.Cost):NotFound();
    [HttpGet("{id:guid}/nutrition")] public async Task<ActionResult<object>> Nutrition(Guid id,CancellationToken ct)=>(await service.Get(id,ct)) is { } x?Ok(new{x.Analysis.TotalNutrition,x.Analysis.PerBar,x.Analysis.Per100G}):NotFound();
    [HttpPost] public async Task<ActionResult<RecipeDetail>> Create(RecipeInput input,CancellationToken ct){var x=await service.Create(input,ct);return CreatedAtAction(nameof(Get),new{id=x.Recipe.Id},x);}
    [HttpPut("{id:guid}")] public async Task<ActionResult<RecipeDetail>> Update(Guid id,RecipeInput input,CancellationToken ct)=>(await service.Update(id,input,ct)) is { } x?Ok(x):NotFound();
    [HttpGet("compare")] public Task<IReadOnlyList<RecipeDetail>> Compare([FromQuery]Guid[] ids,CancellationToken ct)=>service.Compare(ids,ct);
    [HttpGet("export.csv")] public async Task<IActionResult> Export(CancellationToken ct){var rows=await service.List(ct);var csv=new StringBuilder("Receta;Barras;Costo por barra;Proteína;Kcal;Precio directo;Precio gimnasio;Margen\n");foreach(var x in rows)csv.AppendLine($"{x.Name};{x.Bars};{x.CostPerBar};{x.ProteinPerBar};{x.KcalPerBar};{x.DirectPrice};{x.GymPrice};{x.BestMargin}");return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),"text/csv","recetas.csv");}
}

[ApiController,Route("api/production-batches")]
public sealed class ProductionController(IProductionService service):ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<BatchView>> List(CancellationToken ct)=>service.List(ct);
    [HttpPost] public async Task<ActionResult<BatchView>> Create(BatchInput input,CancellationToken ct)=>Ok(await service.Create(input,ct));
}

[ApiController]
public sealed class UtilityController(IRecipeService recipes):ControllerBase
{
    [HttpGet("api/dashboard")] public Task<DashboardView> Dashboard(CancellationToken ct)=>recipes.Dashboard(ct);
    [HttpPost("api/simulator")] public SimulatorResult Simulate(List<SimulatorLine> lines)=>SalesSimulator.Calculate(lines);
    [Route("/error")] public IActionResult Error(){var e=HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;return Problem(statusCode:e is KeyNotFoundException?404:400,title:e?.Message??"Error inesperado");}
}

[ApiController,Route("api/settings")]
public sealed class SettingsController(ISettingsService service):ControllerBase
{
    [HttpGet] public Task<BarFlow.Domain.AppSetting> Get(CancellationToken ct)=>service.Get(ct);
    [HttpPut] public Task<BarFlow.Domain.AppSetting> Update(BarFlow.Domain.AppSetting input,CancellationToken ct)=>service.Update(input,ct);
}
