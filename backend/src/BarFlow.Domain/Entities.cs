namespace BarFlow.Domain;

public enum Unit { G, Kg, Ml, L, Unit }
public enum RecipeSection { Dry, Wet, Inclusions, Filling, Coating, Decoration, Other }
public enum NutritionSource { Manufacturer, Estimated, External, Usda, Other }
public enum PriceChannel { Direct, Wholesale, Gym, Promotional }

public sealed class Ingredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Otros";
    public string? Brand { get; set; }
    public string? Supplier { get; set; }
    public Unit PurchaseUnit { get; set; } = Unit.Kg;
    public decimal PackageQuantity { get; set; } = 1;
    public decimal NetPrice { get; set; }
    public decimal VatPercent { get; set; } = 22;
    public Unit BaseUnit { get; set; } = Unit.G;
    public decimal? DensityGPerMl { get; set; }
    public DateTime PriceUpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public bool Active { get; set; } = true;
    public decimal StockCurrent { get; set; }
    public decimal StockMinimum { get; set; }
    public IngredientNutrition Nutrition { get; set; } = new();
    public List<IngredientPriceHistory> PriceHistory { get; set; } = [];
    public decimal GrossPrice => NetPrice * (1 + VatPercent / 100m);
}

public sealed class IngredientNutrition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IngredientId { get; set; }
    public decimal Kcal { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbohydrates { get; set; }
    public decimal Sugars { get; set; }
    public decimal Fiber { get; set; }
    public decimal TotalFat { get; set; }
    public decimal SaturatedFat { get; set; }
    public decimal TransFat { get; set; }
    public decimal SodiumMg { get; set; }
    public decimal CholesterolMg { get; set; }
    public decimal Polyols { get; set; }
    public NutritionSource SourceType { get; set; } = NutritionSource.Manufacturer;
    public string? Source { get; set; }
    public string? SourceUrl { get; set; }
}

public sealed class IngredientPriceHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IngredientId { get; set; }
    public decimal PreviousPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Supplier { get; set; }
    public decimal PackageQuantity { get; set; }
    public Unit PurchaseUnit { get; set; }
}

public sealed class Recipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Flavor { get; set; }
    public string Category { get; set; } = "Barrita proteica";
    public bool Keto { get; set; }
    public bool SugarFree { get; set; }
    public bool Vegan { get; set; }
    public bool Active { get; set; } = true;
    public int ExpectedBars { get; set; } = 12;
    public decimal TargetWeightPerBar { get; set; } = 60;
    public decimal? FinalBatchWeight { get; set; }
    public decimal TargetProteinPerBar { get; set; } = 20;
    public decimal WastePercent { get; set; } = 3;
    public decimal PackageCostPerBar { get; set; }
    public decimal LabelCostPerBar { get; set; }
    public decimal LaborPerBatch { get; set; }
    public decimal ElectricityPerBatch { get; set; }
    public decimal TransportPerBatch { get; set; }
    public decimal OtherCostsPerBatch { get; set; }
    public decimal IndirectCostsPercent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<SalePrice> SalePrices { get; set; } = [];
}

public sealed class RecipeIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public Guid IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public decimal Quantity { get; set; }
    public Unit Unit { get; set; } = Unit.G;
    public RecipeSection Section { get; set; }
    public bool Optional { get; set; }
    public string? Observation { get; set; }
}

public sealed class SalePrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public PriceChannel Channel { get; set; }
    public decimal Price { get; set; }
}

public sealed class ProductionBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public DateTime ProducedAt { get; set; } = DateTime.UtcNow;
    public decimal Batches { get; set; } = 1;
    public int ExpectedBars { get; set; }
    public int ActualBars { get; set; }
    public int DiscardedBars { get; set; }
    public decimal ActualWeight { get; set; }
    public decimal TotalCostSnapshot { get; set; }
    public string? Notes { get; set; }
    public List<ProductionBatchIngredient> Ingredients { get; set; } = [];
}

public sealed class ProductionBatchIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductionBatchId { get; set; }
    public Guid IngredientId { get; set; }
    public string IngredientName { get; set; } = "";
    public decimal QuantityBase { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public decimal TotalCostSnapshot { get; set; }
}

public sealed class ProductSale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime SoldAt { get; set; } = DateTime.UtcNow;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public string? Channel { get; set; }
    public string? Notes { get; set; }
    public decimal Revenue => Quantity * UnitPrice;
    public decimal Cost => Quantity * UnitCostSnapshot;
    public decimal Profit => Revenue - Cost;
}

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AppSetting
{
    public int Id { get; set; } = 1;
    public string Currency { get; set; } = "UYU";
    public decimal DefaultVat { get; set; } = 22;
    public decimal MinimumMargin { get; set; } = 30;
    public decimal DefaultProteinTarget { get; set; } = 20;
    public decimal DefaultWastePercent { get; set; } = 3;
    public decimal DefaultPackageCost { get; set; } = 2;
    public decimal DefaultLabelCost { get; set; } = 1;
    public decimal HourlyLaborCost { get; set; }
    public decimal EstimatedElectricityCost { get; set; }
}
