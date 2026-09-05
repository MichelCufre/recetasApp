using BarFlow.Application;
using BarFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarFlow.Infrastructure;

public sealed class IngredientService(AppDbContext db) : IIngredientService
{
    private static readonly UnitConversionService Units = new();
    public async Task<IReadOnlyList<IngredientView>> List(string? search, bool includeInactive, CancellationToken ct)
    {
        var q = db.Ingredients.AsNoTracking().Include(x => x.Nutrition).AsQueryable();
        if (!includeInactive) q = q.Where(x => x.Active);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Name.ToLower().Contains(search.ToLower()));
        var ingredients=await q.OrderBy(x => x.Name).ToListAsync(ct);
        var usage=await db.RecipeIngredients.AsNoTracking()
            .Join(db.Recipes.AsNoTracking(),line=>line.RecipeId,recipe=>recipe.Id,(line,recipe)=>new{line.IngredientId,RecipeId=recipe.Id,recipe.Active})
            .GroupBy(x=>x.IngredientId)
            .Select(g=>new{IngredientId=g.Key,Count=g.Select(x=>x.RecipeId).Distinct().Count(),ActiveCount=g.Where(x=>x.Active).Select(x=>x.RecipeId).Distinct().Count()})
            .ToDictionaryAsync(x=>x.IngredientId,ct);
        return ingredients.Select(x=>usage.TryGetValue(x.Id,out var count)?View(x,count.Count,count.ActiveCount):View(x)).ToList();
    }
    public async Task<IngredientView?> Get(Guid id, CancellationToken ct) =>
        (await db.Ingredients.AsNoTracking().Include(x=>x.Nutrition).FirstOrDefaultAsync(x=>x.Id==id,ct)) is { } x ? View(x) : null;
    public async Task<IngredientView> Create(IngredientInput input, CancellationToken ct)
    {
        Validate(input); var x = Map(new Ingredient(), input); db.Add(x); await db.SaveChangesAsync(ct); return View(x);
    }
    public async Task<IngredientView?> Update(Guid id, IngredientInput input, CancellationToken ct)
    {
        Validate(input); var x=await db.Ingredients.Include(y=>y.Nutrition).FirstOrDefaultAsync(y=>y.Id==id,ct); if(x is null)return null;
        if(x.NetPrice!=input.NetPrice || x.PackageQuantity!=input.PackageQuantity || x.PurchaseUnit!=input.PurchaseUnit)
            db.IngredientPriceHistory.Add(new(){IngredientId=id,PreviousPrice=x.NetPrice,NewPrice=input.NetPrice,Supplier=input.Supplier,PackageQuantity=input.PackageQuantity,PurchaseUnit=input.PurchaseUnit});
        Map(x,input); x.PriceUpdatedAt=DateTime.UtcNow; await db.SaveChangesAsync(ct); return View(x);
    }
    public async Task<IReadOnlyList<IngredientPriceHistory>> PriceHistory(Guid id,CancellationToken ct)=>
        await db.IngredientPriceHistory.AsNoTracking().Where(x=>x.IngredientId==id).OrderByDescending(x=>x.ChangedAt).ToListAsync(ct);
    public async Task<bool> Delete(Guid id,CancellationToken ct)
    {
        var ingredient=await db.Ingredients.FirstOrDefaultAsync(x=>x.Id==id,ct);
        if(ingredient is null)return false;
        if(await db.RecipeIngredients.AnyAsync(x=>x.IngredientId==id,ct))
            throw new InvalidOperationException("No se puede eliminar el ingrediente porque está usado en una o más recetas. Quitalo primero de esas recetas o marcá el ingrediente como inactivo.");
        db.Ingredients.Remove(ingredient);
        await db.SaveChangesAsync(ct);
        return true;
    }
    private static void Validate(IngredientInput x){if(string.IsNullOrWhiteSpace(x.Name))throw new ArgumentException("El nombre es obligatorio.");if(x.PackageQuantity<=0)throw new ArgumentException("La cantidad del envase debe ser mayor a cero.");if(x.NetPrice<0)throw new ArgumentException("El precio no puede ser negativo.");}
    private static Ingredient Map(Ingredient x,IngredientInput i){x.Name=i.Name.Trim();x.Category=i.Category;x.Brand=i.Brand;x.Supplier=i.Supplier;x.PurchaseUnit=i.PurchaseUnit;x.PackageQuantity=i.PackageQuantity;x.NetPrice=i.NetPrice;x.VatPercent=i.VatPercent;x.BaseUnit=i.BaseUnit;x.DensityGPerMl=i.DensityGPerMl;x.Notes=i.Notes;x.Active=i.Active;x.StockCurrent=i.StockCurrent;x.StockMinimum=i.StockMinimum;x.Nutrition??=new();var n=x.Nutrition;var z=i.Nutrition;n.Kcal=z.Kcal;n.Protein=z.Protein;n.Carbohydrates=z.Carbohydrates;n.Sugars=z.Sugars;n.Fiber=z.Fiber;n.TotalFat=z.TotalFat;n.SaturatedFat=z.SaturatedFat;n.TransFat=z.TransFat;n.SodiumMg=z.SodiumMg;n.CholesterolMg=z.CholesterolMg;n.Polyols=z.Polyols;n.SourceType=z.SourceType;n.Source=z.Source;n.SourceUrl=z.SourceUrl;return x;}
    private static IngredientView View(Ingredient x,int recipeCount=0,int activeRecipeCount=0)=>new(x.Id,x.Name,x.Category,x.Brand,x.Supplier,x.PurchaseUnit,x.PackageQuantity,x.NetPrice,x.VatPercent,x.GrossPrice,x.BaseUnit,Units.CostPerBaseUnit(x),x.DensityGPerMl,x.PriceUpdatedAt,x.Notes,x.Active,x.StockCurrent,x.StockMinimum,recipeCount,activeRecipeCount,new(x.Nutrition.Kcal,x.Nutrition.Protein,x.Nutrition.Carbohydrates,x.Nutrition.Sugars,x.Nutrition.Fiber,x.Nutrition.TotalFat,x.Nutrition.SaturatedFat,x.Nutrition.TransFat,x.Nutrition.SodiumMg,x.Nutrition.CholesterolMg,x.Nutrition.Polyols,x.Nutrition.SourceType,x.Nutrition.Source,x.Nutrition.SourceUrl));
}

public sealed class RecipeService(AppDbContext db) : IRecipeService
{
    private static readonly RecipeCalculator Calc=new();
    public async Task<IReadOnlyList<RecipeSummary>> List(CancellationToken ct)=>(await Query().AsNoTracking().ToListAsync(ct)).Select(Summary).ToList();
    public async Task<RecipeDetail?> Get(Guid id,CancellationToken ct)=>(await Query().AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id,ct)) is { } x?Detail(x):null;
    public async Task<RecipeDetail> Create(RecipeInput input,CancellationToken ct){Validate(input);var x=new Recipe();await Map(x,input,ct);db.Add(x);await db.SaveChangesAsync(ct);return Detail(x);}
    public async Task<RecipeDetail?> Update(Guid id,RecipeInput input,CancellationToken ct)
    {
        Validate(input);
        var x=await Query().FirstOrDefaultAsync(y=>y.Id==id,ct);
        if(x is null)return null;

        await using var transaction=await db.Database.BeginTransactionAsync(ct);
        db.RecipeIngredients.RemoveRange(x.Ingredients);
        db.SalePrices.RemoveRange(x.SalePrices);
        await db.SaveChangesAsync(ct);

        x.Ingredients.Clear();
        x.SalePrices.Clear();
        await Map(x,input,ct);
        db.RecipeIngredients.AddRange(x.Ingredients);
        db.SalePrices.AddRange(x.SalePrices);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Detail(x);
    }
    public async Task<IReadOnlyList<RecipeDetail>> Compare(IEnumerable<Guid> ids,CancellationToken ct){var set=ids.Take(4).ToList();return (await Query().AsNoTracking().Where(x=>set.Contains(x.Id)).ToListAsync(ct)).Select(Detail).ToList();}
    public async Task<IReadOnlyList<RecipeSummary>> Versions(Guid id,CancellationToken ct){var current=await db.Recipes.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException("Receta no encontrada.");var family=Family(current.Name);return (await Query().AsNoTracking().ToListAsync(ct)).Where(x=>Family(x.Name).Equals(family,StringComparison.OrdinalIgnoreCase)).Select(Summary).OrderBy(x=>x.Name).ToList();}
    public async Task<RecipeDetail> CreateVersion(Guid id,RecipeVersionInput input,CancellationToken ct){if(string.IsNullOrWhiteSpace(input.VersionName))throw new ArgumentException("El nombre de la versión es obligatorio.");if(input.TargetWeightPerBar<=0)throw new ArgumentException("El peso objetivo debe ser mayor a cero.");var source=await Query().FirstOrDefaultAsync(x=>x.Id==id,ct)??throw new KeyNotFoundException("Receta no encontrada.");var family=Family(source.Name);var name=$"{family} · {input.VersionName.Trim()}";if(await db.Recipes.AnyAsync(x=>x.Name==name,ct))throw new ArgumentException("Ya existe una versión con ese nombre.");var copy=new Recipe{Name=name,Description=source.Description,Flavor=source.Flavor,Category=source.Category,Keto=source.Keto,SugarFree=source.SugarFree,Vegan=source.Vegan,Active=source.Active,TargetWeightPerBar=input.TargetWeightPerBar,FinalBatchWeight=source.FinalBatchWeight,TargetProteinPerBar=source.TargetProteinPerBar,WastePercent=source.WastePercent,PackageCostPerBar=source.PackageCostPerBar,LabelCostPerBar=source.LabelCostPerBar,LaborPerBatch=source.LaborPerBatch,ElectricityPerBatch=source.ElectricityPerBatch,TransportPerBatch=source.TransportPerBatch,OtherCostsPerBatch=source.OtherCostsPerBatch,IndirectCostsPercent=source.IndirectCostsPercent,Notes=source.Notes,Ingredients=source.Ingredients.Select(l=>new RecipeIngredient{IngredientId=l.IngredientId,Ingredient=l.Ingredient,Quantity=l.Quantity,Unit=l.Unit,Section=l.Section,Optional=l.Optional,Observation=l.Observation}).ToList(),SalePrices=source.SalePrices.Select(p=>new SalePrice{Channel=p.Channel,Price=p.Price}).ToList()};var weight=copy.Ingredients.Sum(l=>new UnitConversionService().Convert(l.Quantity,l.Unit,Unit.G,l.Ingredient.DensityGPerMl));copy.FinalBatchWeight=weight;copy.ExpectedBars=(int)Math.Floor(weight/copy.TargetWeightPerBar);if(copy.ExpectedBars<=0)throw new ArgumentException("El lote no alcanza para una unidad del peso indicado.");db.Recipes.Add(copy);await db.SaveChangesAsync(ct);return Detail(copy);}
    public async Task<DashboardView> Dashboard(CancellationToken ct)
    {var recipes=(await Query().AsNoTracking().ToListAsync(ct)).Select(Summary).ToList();var prices=recipes.Where(x=>x.DirectPrice>0).ToList();var dist=(await Query().AsNoTracking().FirstOrDefaultAsync(ct)) is { } first?Calc.Analyze(first).Contributions.OrderByDescending(x=>x.Cost).ToList():[];return new(await db.Ingredients.CountAsync(ct),recipes.Count,recipes.Count==0?0:recipes.Average(x=>x.CostPerBar),prices.Count==0?0:prices.Average(x=>x.DirectPrice!.Value),prices.Count==0?0:prices.Average(x=>x.BestMargin),recipes.MaxBy(x=>x.BestMargin)?.Name,recipes.MaxBy(x=>x.CostPerBar)?.Name,recipes.MaxBy(x=>x.ProteinPerBar)?.Name,recipes.MinBy(x=>x.KcalPerBar)?.Name,recipes,dist);}
    private IQueryable<Recipe> Query()=>db.Recipes.Include(x=>x.Ingredients).ThenInclude(x=>x.Ingredient).ThenInclude(x=>x.Nutrition).Include(x=>x.SalePrices).AsSplitQuery();
    private static string Family(string name)=>name.Split(" · ",2,StringSplitOptions.TrimEntries)[0];
    private async Task Map(Recipe x,RecipeInput i,CancellationToken ct){var ids=i.Ingredients.Select(l=>l.IngredientId).Distinct().ToList();var ingredients=await db.Ingredients.Include(z=>z.Nutrition).Where(z=>ids.Contains(z.Id)).ToDictionaryAsync(z=>z.Id,ct);if(ingredients.Count!=ids.Count)throw new ArgumentException("Uno o más ingredientes no existen.");var units=new UnitConversionService();var finalWeight=i.Ingredients.Sum(l=>units.Convert(l.Quantity,l.Unit,Unit.G,ingredients[l.IngredientId].DensityGPerMl));var expectedBars=(int)Math.Floor(finalWeight/i.TargetWeightPerBar);if(expectedBars<=0)throw new ArgumentException("El peso total de los ingredientes no alcanza para producir una barrita del peso objetivo.");x.Name=i.Name.Trim();x.Description=i.Description;x.Flavor=i.Flavor;x.Category=i.Category;x.Keto=i.Keto;x.SugarFree=i.SugarFree;x.Vegan=i.Vegan;x.Active=i.Active;x.ExpectedBars=expectedBars;x.TargetWeightPerBar=i.TargetWeightPerBar;x.FinalBatchWeight=finalWeight;x.TargetProteinPerBar=i.TargetProteinPerBar;x.WastePercent=i.WastePercent;x.PackageCostPerBar=i.PackageCostPerBar;x.LabelCostPerBar=i.LabelCostPerBar;x.LaborPerBatch=i.LaborPerBatch;x.ElectricityPerBatch=i.ElectricityPerBatch;x.TransportPerBatch=i.TransportPerBatch;x.OtherCostsPerBatch=i.OtherCostsPerBatch;x.IndirectCostsPercent=i.IndirectCostsPercent;x.Notes=i.Notes;x.UpdatedAt=DateTime.UtcNow;x.Ingredients=i.Ingredients.Select(l=>new RecipeIngredient{IngredientId=l.IngredientId,Ingredient=ingredients[l.IngredientId],Quantity=l.Quantity,Unit=l.Unit,Section=l.Section,Optional=l.Optional,Observation=l.Observation}).ToList();x.SalePrices=i.SalePrices.GroupBy(p=>p.Channel).Select(g=>new SalePrice{Channel=g.Key,Price=g.Last().Price}).ToList();}
    private static void Validate(RecipeInput x){if(string.IsNullOrWhiteSpace(x.Name))throw new ArgumentException("El nombre es obligatorio.");if(x.TargetWeightPerBar<=0)throw new ArgumentException("El peso objetivo de la barrita debe ser mayor a cero.");if(x.Ingredients.Count==0)throw new ArgumentException("La receta debe tener al menos un ingrediente.");if(x.Ingredients.Any(i=>i.Quantity<=0))throw new ArgumentException("Las cantidades deben ser mayores a cero.");}
    private static RecipeSummary Summary(Recipe x){var a=Calc.Analyze(x);var direct=x.SalePrices.FirstOrDefault(p=>p.Channel==PriceChannel.Direct)?.Price;var gym=x.SalePrices.FirstOrDefault(p=>p.Channel==PriceChannel.Gym)?.Price;var price=direct??gym;var margin=price is null?0:Calc.Profit(price.Value,a.Cost.PerBar,x.ExpectedBars).MarginPercent;return new(x.Id,x.Name,x.Flavor,x.Category,x.ExpectedBars,x.TargetWeightPerBar,a.Cost.PerBar,a.PerBar.Protein,a.PerBar.Kcal,direct,gym,margin,x.Active);}
    private static RecipeDetail Detail(Recipe x){x.Ingredients=x.Ingredients.OrderBy(i=>i.Ingredient.Name,StringComparer.CurrentCultureIgnoreCase).ToList();var a=Calc.Analyze(x);var profitability=x.SalePrices.ToDictionary(p=>p.Channel,p=>Calc.Profit(p.Price,a.Cost.PerBar,x.ExpectedBars));var alerts=new List<string>();if(a.PerBar.Protein<x.TargetProteinPerBar)alerts.Add($"La receta está por debajo del objetivo de {x.TargetProteinPerBar:0.#} g de proteína.");if(a.PerBar.SaturatedFat>10)alerts.Add("Las grasas saturadas superan 10 g por barra.");foreach(var p in profitability.Where(p=>p.Value.MarginPercent<30))alerts.Add($"El canal {p.Key} deja menos de 30 % de margen.");if(a.RealWastePercent is > 10)alerts.Add("El peso final difiere más de 10 % del peso teórico.");return new(x,a,profitability,alerts);}
}

public sealed class ProductionService(AppDbContext db) : IProductionService
{
    private static readonly RecipeCalculator Calc=new();
    public async Task<IReadOnlyList<BatchView>> List(CancellationToken ct)=>(await db.ProductionBatches.AsNoTracking().Include(x=>x.Recipe).OrderByDescending(x=>x.ProducedAt).ToListAsync(ct)).Select(View).ToList();
    public async Task<BatchView> Create(BatchInput input,CancellationToken ct){if(input.Batches<=0||input.ActualBars<=0)throw new ArgumentException("Tandas y barras reales deben ser mayores a cero.");var recipe=await db.Recipes.Include(x=>x.Ingredients).ThenInclude(x=>x.Ingredient).ThenInclude(x=>x.Nutrition).Include(x=>x.SalePrices).FirstOrDefaultAsync(x=>x.Id==input.RecipeId,ct)??throw new KeyNotFoundException("Receta no encontrada.");var a=Calc.Analyze(recipe);var batch=new ProductionBatch{RecipeId=recipe.Id,Recipe=recipe,Batches=input.Batches,ExpectedBars=input.ExpectedBars,ActualBars=input.ActualBars,DiscardedBars=input.DiscardedBars,ActualWeight=input.ActualWeight,ProducedAt=input.ProducedAt??DateTime.UtcNow,Notes=input.Notes,TotalCostSnapshot=a.Cost.Total*input.Batches,Ingredients=a.Contributions.Select(c=>new ProductionBatchIngredient{IngredientId=c.IngredientId,IngredientName=c.Name,QuantityBase=c.QuantityBase*input.Batches,UnitCostSnapshot=c.QuantityBase==0?0:c.Cost/c.QuantityBase,TotalCostSnapshot=c.Cost*input.Batches}).ToList()};if(input.DeductStock)foreach(var line in recipe.Ingredients)line.Ingredient.StockCurrent-=new UnitConversionService().Convert(line.Quantity*input.Batches,line.Unit,line.Ingredient.BaseUnit,line.Ingredient.DensityGPerMl);db.Add(batch);await db.SaveChangesAsync(ct);return View(batch);}
    private static BatchView View(ProductionBatch x){var theoretical=x.Recipe.Ingredients.Count==0?x.ActualWeight:x.Recipe.Ingredients.Sum(i=>new UnitConversionService().Convert(i.Quantity*x.Batches,i.Unit,Unit.G,i.Ingredient.DensityGPerMl));var waste=theoretical==0?0:(theoretical-x.ActualWeight)/theoretical*100m;return new(x.Id,x.RecipeId,x.Recipe.Name,x.ProducedAt,x.ActualBars,x.DiscardedBars,x.ActualWeight,x.TotalCostSnapshot,x.ActualBars==0?0:x.TotalCostSnapshot/x.ActualBars,waste);}
}

public sealed class ProductSaleService(AppDbContext db) : IProductSaleService
{
    private static readonly RecipeCalculator Calc = new();

    public async Task<SalesSummary> List(CancellationToken ct)
    {
        var sales = (await db.ProductSales.AsNoTracking().Include(x => x.Recipe).Include(x => x.Customer)
            .OrderByDescending(x => x.SoldAt).ToListAsync(ct)).Select(View).ToList();
        var revenue = sales.Sum(x => x.Revenue);
        var cost = sales.Sum(x => x.Cost);
        var customers = (await db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct)).Select(c =>
        {
            var own = sales.Where(x => x.CustomerId == c.Id).ToList();
            var ownRevenue = own.Sum(x => x.Revenue); var ownCost = own.Sum(x => x.Cost);
            return new CustomerView(c.Id, c.Name, c.Phone, c.Notes, own.Sum(x => x.Quantity), own.Select(x=>x.OrderId).Distinct().Count(),
                ownRevenue, ownCost, ownRevenue - ownCost, ownRevenue == 0 ? 0 : (ownRevenue - ownCost) / ownRevenue * 100m);
        }).ToList();
        return new(revenue, cost, revenue - cost, revenue == 0 ? 0 : (revenue - cost) / revenue * 100m,
            sales.Sum(x => x.Quantity), sales.Select(x=>x.OrderId).Distinct().Count(), sales, customers);
    }

    public async Task<ProductSaleView> Create(ProductSaleInput input, CancellationToken ct)
    {
        if (input.Quantity <= 0) throw new ArgumentException("La cantidad vendida debe ser mayor a cero.");
        if (input.UnitPrice < 0) throw new ArgumentException("El precio de venta no puede ser negativo.");
        var recipe = await db.Recipes.Include(x => x.Ingredients).ThenInclude(x => x.Ingredient)
            .ThenInclude(x => x.Nutrition).Include(x => x.SalePrices)
            .FirstOrDefaultAsync(x => x.Id == input.RecipeId, ct)
            ?? throw new KeyNotFoundException("Producto no encontrado.");
        Customer? customer = null;
        if (input.CustomerId is { } customerId) customer = await db.Customers.FindAsync([customerId], ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");
        var sale = new ProductSale { RecipeId = recipe.Id, Recipe = recipe, Quantity = input.Quantity,
            UnitPrice = input.UnitPrice, UnitCostSnapshot = Calc.Analyze(recipe).Cost.PerBar,
            SoldAt = input.SoldAt ?? DateTime.UtcNow, Channel = input.Channel, Notes = input.Notes,
            CustomerId = customer?.Id, Customer = customer };
        db.ProductSales.Add(sale);
        await db.SaveChangesAsync(ct);
        return View(sale);
    }

    public async Task<SaleOrderView> CreateOrder(SaleOrderInput input, CancellationToken ct)
    {
        if (input.Lines.Count == 0) throw new ArgumentException("La venta debe tener al menos un producto.");
        if (input.Lines.Any(x => x.Quantity <= 0 || x.UnitPrice < 0)) throw new ArgumentException("Revisá cantidades y precios de la venta.");
        Customer? customer = null;
        if (input.CustomerId is { } customerId) customer = await db.Customers.FindAsync([customerId], ct) ?? throw new KeyNotFoundException("Cliente no encontrado.");
        var ids = input.Lines.Select(x => x.RecipeId).Distinct().ToList();
        var recipes = await db.Recipes.Include(x=>x.Ingredients).ThenInclude(x=>x.Ingredient).ThenInclude(x=>x.Nutrition).Include(x=>x.SalePrices).Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,ct);
        if (recipes.Count != ids.Count) throw new KeyNotFoundException("Uno o más productos no existen.");
        var orderId = Guid.NewGuid(); var soldAt = input.SoldAt ?? DateTime.UtcNow;
        var rows = input.Lines.Select(line => { var recipe=recipes[line.RecipeId]; return new ProductSale { OrderId=orderId,RecipeId=recipe.Id,Recipe=recipe,CustomerId=customer?.Id,Customer=customer,SoldAt=soldAt,Quantity=line.Quantity,UnitPrice=line.UnitPrice,UnitCostSnapshot=Calc.Analyze(recipe).Cost.PerBar,Channel=input.Channel,Notes=input.Notes }; }).ToList();
        db.ProductSales.AddRange(rows); await db.SaveChangesAsync(ct); return Order(rows.Select(View).ToList());
    }

    public async Task<CustomerDetail?> Customer(Guid id, CancellationToken ct)
    {
        var customer=await db.Customers.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id,ct); if(customer is null)return null;
        var lines=(await db.ProductSales.AsNoTracking().Include(x=>x.Recipe).Include(x=>x.Customer).Where(x=>x.CustomerId==id).OrderByDescending(x=>x.SoldAt).ToListAsync(ct)).Select(View).ToList();
        var revenue=lines.Sum(x=>x.Revenue);var cost=lines.Sum(x=>x.Cost);var view=new CustomerView(customer.Id,customer.Name,customer.Phone,customer.Notes,lines.Sum(x=>x.Quantity),lines.Select(x=>x.OrderId).Distinct().Count(),revenue,cost,revenue-cost,revenue==0?0:(revenue-cost)/revenue*100m);
        return new(view,lines.GroupBy(x=>x.OrderId).Select(g=>Order(g.ToList())).OrderByDescending(x=>x.SoldAt).ToList());
    }

    public async Task<bool> Delete(Guid id, CancellationToken ct)
    {
        var sale = await db.ProductSales.FindAsync([id], ct);
        if (sale is null) return false;
        db.ProductSales.RemoveRange(await db.ProductSales.Where(x=>x.OrderId==sale.OrderId).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<CustomerView> CreateCustomer(CustomerInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("El nombre del cliente es obligatorio.");
        var name = input.Name.Trim();
        if (await db.Customers.AnyAsync(x => x.Name.ToLower() == name.ToLower(), ct)) throw new ArgumentException("Ya existe un cliente con ese nombre.");
        var customer = new Customer { Name = name, Phone = input.Phone?.Trim(), Notes = input.Notes?.Trim() };
        db.Customers.Add(customer); await db.SaveChangesAsync(ct);
        return new(customer.Id, customer.Name, customer.Phone, customer.Notes, 0, 0, 0, 0, 0, 0);
    }

    private static ProductSaleView View(ProductSale x) => new(x.Id, x.OrderId, x.RecipeId, x.Recipe.Name, x.SoldAt,
        x.Quantity, x.UnitPrice, x.UnitCostSnapshot, x.Revenue, x.Cost, x.Profit,
        x.Revenue == 0 ? 0 : x.Profit / x.Revenue * 100m, x.Channel, x.Notes, x.CustomerId, x.Customer?.Name);
    private static SaleOrderView Order(IReadOnlyList<ProductSaleView> lines){var first=lines[0];var revenue=lines.Sum(x=>x.Revenue);var cost=lines.Sum(x=>x.Cost);return new(first.OrderId,first.CustomerId,first.CustomerName,first.SoldAt,first.Channel,first.Notes,lines.Sum(x=>x.Quantity),revenue,cost,revenue-cost,revenue==0?0:(revenue-cost)/revenue*100m,lines);}
}

public sealed class SettingsService(AppDbContext db) : ISettingsService
{
    public async Task<AppSetting> Get(CancellationToken ct) =>
        await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new AppSetting();
    public async Task<AppSetting> Update(AppSetting input, CancellationToken ct)
    {
        var current=await db.AppSettings.FirstOrDefaultAsync(ct);
        if(current is null){input.Id=1;db.Add(input);current=input;}
        else {current.Currency=input.Currency;current.DefaultVat=input.DefaultVat;current.MinimumMargin=input.MinimumMargin;current.DefaultProteinTarget=input.DefaultProteinTarget;current.DefaultWastePercent=input.DefaultWastePercent;current.DefaultPackageCost=input.DefaultPackageCost;current.DefaultLabelCost=input.DefaultLabelCost;current.HourlyLaborCost=input.HourlyLaborCost;current.EstimatedElectricityCost=input.EstimatedElectricityCost;}
        await db.SaveChangesAsync(ct);return current;
    }
}
