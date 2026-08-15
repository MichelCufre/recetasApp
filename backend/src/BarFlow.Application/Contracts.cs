using BarFlow.Domain;

namespace BarFlow.Application;

public record IngredientInput(string Name, string Category, string? Brand, string? Supplier,
    Unit PurchaseUnit, decimal PackageQuantity, decimal NetPrice, decimal VatPercent, Unit BaseUnit,
    decimal? DensityGPerMl, string? Notes, bool Active, decimal StockCurrent, decimal StockMinimum,
    NutritionInput Nutrition);
public record NutritionInput(decimal Kcal, decimal Protein, decimal Carbohydrates, decimal Sugars,
    decimal Fiber, decimal TotalFat, decimal SaturatedFat, decimal TransFat, decimal SodiumMg,
    decimal CholesterolMg, decimal Polyols, NutritionSource SourceType, string? Source, string? SourceUrl);
public record IngredientView(Guid Id, string Name, string Category, string? Brand, string? Supplier,
    Unit PurchaseUnit, decimal PackageQuantity, decimal NetPrice, decimal VatPercent, decimal GrossPrice,
    Unit BaseUnit, decimal CostPerBaseUnit, decimal? DensityGPerMl, DateTime PriceUpdatedAt, string? Notes,
    bool Active, decimal StockCurrent, decimal StockMinimum, NutritionInput Nutrition);

public record RecipeLineInput(Guid IngredientId, decimal Quantity, Unit Unit, RecipeSection Section,
    bool Optional, string? Observation);
public record SalePriceInput(PriceChannel Channel, decimal Price);
public record RecipeInput(string Name, string? Description, string? Flavor, string Category, bool Keto,
    bool SugarFree, bool Vegan, bool Active, int ExpectedBars, decimal TargetWeightPerBar,
    decimal? FinalBatchWeight, decimal TargetProteinPerBar, decimal WastePercent,
    decimal PackageCostPerBar, decimal LabelCostPerBar, decimal LaborPerBatch,
    decimal ElectricityPerBatch, decimal TransportPerBatch, decimal OtherCostsPerBatch,
    decimal IndirectCostsPercent, string? Notes, List<RecipeLineInput> Ingredients,
    List<SalePriceInput> SalePrices);
public record RecipeSummary(Guid Id, string Name, string? Flavor, int Bars, decimal CostPerBar,
    decimal ProteinPerBar, decimal KcalPerBar, decimal? DirectPrice, decimal? GymPrice,
    decimal BestMargin, bool Active);
public record RecipeDetail(Recipe Recipe, RecipeAnalysis Analysis,
    IReadOnlyDictionary<PriceChannel, Profitability> Profitability, IReadOnlyList<string> Alerts);
public record DashboardView(int IngredientCount, int RecipeCount, decimal AverageCost,
    decimal AveragePrice, decimal AverageMargin, string? MostProfitable, string? MostExpensive,
    string? HighestProtein, string? LowestCalories, IReadOnlyList<RecipeSummary> Recipes,
    IReadOnlyList<IngredientContribution> CostDistribution);
public record BatchInput(Guid RecipeId, decimal Batches, int ExpectedBars, int ActualBars,
    int DiscardedBars, decimal ActualWeight, bool DeductStock, DateTime? ProducedAt, string? Notes);
public record BatchView(Guid Id, Guid RecipeId, string RecipeName, DateTime ProducedAt, int ActualBars,
    int DiscardedBars, decimal ActualWeight, decimal TotalCost, decimal CostPerGoodBar, decimal WastePercent);
public record SimulatorLine(string Label, int UnitsPerDay, int DaysPerWeek, decimal Price, decimal Cost);
public record SimulatorResult(decimal DailyRevenue, decimal DailyCost, decimal DailyProfit,
    decimal WeeklyRevenue, decimal WeeklyProfit, decimal MonthlyRevenue, decimal MonthlyProfit,
    decimal AnnualRevenue, decimal AnnualProfit);

public interface IIngredientService
{
    Task<IReadOnlyList<IngredientView>> List(string? search, bool includeInactive, CancellationToken ct);
    Task<IngredientView?> Get(Guid id, CancellationToken ct);
    Task<IngredientView> Create(IngredientInput input, CancellationToken ct);
    Task<IngredientView?> Update(Guid id, IngredientInput input, CancellationToken ct);
    Task<IReadOnlyList<IngredientPriceHistory>> PriceHistory(Guid id, CancellationToken ct);
}
public interface IRecipeService
{
    Task<IReadOnlyList<RecipeSummary>> List(CancellationToken ct);
    Task<RecipeDetail?> Get(Guid id, CancellationToken ct);
    Task<RecipeDetail> Create(RecipeInput input, CancellationToken ct);
    Task<RecipeDetail?> Update(Guid id, RecipeInput input, CancellationToken ct);
    Task<DashboardView> Dashboard(CancellationToken ct);
    Task<IReadOnlyList<RecipeDetail>> Compare(IEnumerable<Guid> ids, CancellationToken ct);
}
public interface IProductionService
{
    Task<IReadOnlyList<BatchView>> List(CancellationToken ct);
    Task<BatchView> Create(BatchInput input, CancellationToken ct);
}
public interface ISettingsService
{
    Task<AppSetting> Get(CancellationToken ct);
    Task<AppSetting> Update(AppSetting input, CancellationToken ct);
}

public static class SalesSimulator
{
    public static SimulatorResult Calculate(IEnumerable<SimulatorLine> lines)
    {
        var data = lines.ToList();
        var weeklyRevenue = data.Sum(x => x.UnitsPerDay * x.DaysPerWeek * x.Price);
        var weeklyCost = data.Sum(x => x.UnitsPerDay * x.DaysPerWeek * x.Cost);
        var workingDays = data.Sum(x => x.DaysPerWeek) == 0 ? 1 : 7m;
        return new(weeklyRevenue / workingDays, weeklyCost / workingDays,
            (weeklyRevenue - weeklyCost) / workingDays, weeklyRevenue, weeklyRevenue - weeklyCost,
            weeklyRevenue * 52m / 12m, (weeklyRevenue-weeklyCost) * 52m / 12m,
            weeklyRevenue * 52m, (weeklyRevenue-weeklyCost) * 52m);
    }
}
