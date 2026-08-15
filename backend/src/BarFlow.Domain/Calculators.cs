namespace BarFlow.Domain;

public sealed class UnitConversionService
{
    public decimal Convert(decimal value, Unit from, Unit to, decimal? density = null)
    {
        if (from == to) return value;
        if (from == Unit.Kg) return Convert(value * 1000m, Unit.G, to, density);
        if (from == Unit.L) return Convert(value * 1000m, Unit.Ml, to, density);
        if (to == Unit.Kg) return Convert(value, from, Unit.G, density) / 1000m;
        if (to == Unit.L) return Convert(value, from, Unit.Ml, density) / 1000m;
        if (from == Unit.G && to == Unit.Ml && density > 0) return value / density.Value;
        if (from == Unit.Ml && to == Unit.G && density > 0) return value * density.Value;
        throw new InvalidOperationException($"No se puede convertir {from} a {to} sin una densidad válida.");
    }

    public decimal CostPerBaseUnit(Ingredient ingredient) =>
        ingredient.GrossPrice / Convert(ingredient.PackageQuantity, ingredient.PurchaseUnit, ingredient.BaseUnit, ingredient.DensityGPerMl);
}

public record NutritionTotals(decimal Kcal = 0, decimal Protein = 0, decimal Carbohydrates = 0,
    decimal Sugars = 0, decimal Fiber = 0, decimal TotalFat = 0, decimal SaturatedFat = 0,
    decimal TransFat = 0, decimal SodiumMg = 0, decimal CholesterolMg = 0, decimal Polyols = 0)
{
    public NutritionTotals Scale(decimal factor) => new(Kcal * factor, Protein * factor, Carbohydrates * factor,
        Sugars * factor, Fiber * factor, TotalFat * factor, SaturatedFat * factor, TransFat * factor,
        SodiumMg * factor, CholesterolMg * factor, Polyols * factor);
    public static NutritionTotals operator +(NutritionTotals a, NutritionTotals b) => new(a.Kcal+b.Kcal,
        a.Protein+b.Protein, a.Carbohydrates+b.Carbohydrates, a.Sugars+b.Sugars, a.Fiber+b.Fiber,
        a.TotalFat+b.TotalFat, a.SaturatedFat+b.SaturatedFat, a.TransFat+b.TransFat,
        a.SodiumMg+b.SodiumMg, a.CholesterolMg+b.CholesterolMg, a.Polyols+b.Polyols);
}

public record CostTotals(decimal Ingredients, decimal Waste, decimal Packaging, decimal Labor,
    decimal Electricity, decimal Transport, decimal Other, decimal Indirect, decimal Total, decimal PerBar);
public record RecipeAnalysis(decimal TheoreticalWeight, decimal UsedWeight, decimal? RealWastePercent,
    CostTotals Cost, NutritionTotals TotalNutrition, NutritionTotals PerBar, NutritionTotals Per100G,
    IReadOnlyList<IngredientContribution> Contributions);
public record IngredientContribution(Guid IngredientId, string Name, decimal QuantityBase, decimal Cost,
    decimal Protein, decimal Kcal);
public record Profitability(decimal ProfitPerUnit, decimal ProfitPerBatch, decimal MarginPercent, decimal MarkupPercent);

public sealed class RecipeCalculator
{
    private readonly UnitConversionService _units = new();

    public RecipeAnalysis Analyze(Recipe recipe)
    {
        if (recipe.ExpectedBars <= 0) throw new InvalidOperationException("El rendimiento debe ser mayor a cero.");
        decimal weight = 0, ingredientCost = 0;
        var nutrition = new NutritionTotals();
        var parts = new List<IngredientContribution>();
        foreach (var line in recipe.Ingredients)
        {
            var ingredient = line.Ingredient ?? throw new InvalidOperationException("Falta cargar el ingrediente.");
            var baseQty = _units.Convert(line.Quantity, line.Unit, ingredient.BaseUnit, ingredient.DensityGPerMl);
            var grams = _units.Convert(line.Quantity, line.Unit, Unit.G, ingredient.DensityGPerMl);
            var cost = baseQty * _units.CostPerBaseUnit(ingredient);
            var n = From(ingredient.Nutrition).Scale(grams / 100m);
            weight += grams; ingredientCost += cost; nutrition += n;
            parts.Add(new(ingredient.Id, ingredient.Name, baseQty, cost, n.Protein, n.Kcal));
        }
        var waste = ingredientCost * recipe.WastePercent / 100m;
        var packaging = (recipe.PackageCostPerBar + recipe.LabelCostPerBar) * recipe.ExpectedBars;
        var direct = ingredientCost + waste + packaging + recipe.LaborPerBatch + recipe.ElectricityPerBatch + recipe.TransportPerBatch + recipe.OtherCostsPerBatch;
        var indirect = direct * recipe.IndirectCostsPercent / 100m;
        var total = direct + indirect;
        var usedWeight = recipe.FinalBatchWeight is > 0 ? recipe.FinalBatchWeight.Value : weight;
        decimal? realWaste = recipe.FinalBatchWeight is > 0 && weight > 0 ? (weight - recipe.FinalBatchWeight.Value) / weight * 100m : null;
        return new(weight, usedWeight, realWaste,
            new(ingredientCost, waste, packaging, recipe.LaborPerBatch, recipe.ElectricityPerBatch,
                recipe.TransportPerBatch, recipe.OtherCostsPerBatch, indirect, total, total / recipe.ExpectedBars),
            nutrition, nutrition.Scale(1m / recipe.ExpectedBars), nutrition.Scale(100m / usedWeight), parts);
    }

    public Profitability Profit(decimal salePrice, decimal unitCost, int batchSize = 1)
    {
        var profit = salePrice - unitCost;
        return new(profit, profit * batchSize, salePrice == 0 ? 0 : profit / salePrice * 100m,
            unitCost == 0 ? 0 : profit / unitCost * 100m);
    }

    public decimal ProteinIngredientSuggestion(Recipe recipe, Ingredient proteinIngredient)
    {
        var analysis = Analyze(recipe);
        var deficit = recipe.TargetProteinPerBar * recipe.ExpectedBars - analysis.TotalNutrition.Protein;
        var concentration = proteinIngredient.Nutrition.Protein / 100m;
        return deficit <= 0 || concentration <= 0 ? 0 : deficit / concentration;
    }

    private static NutritionTotals From(IngredientNutrition n) => new(n.Kcal, n.Protein, n.Carbohydrates,
        n.Sugars, n.Fiber, n.TotalFat, n.SaturatedFat, n.TransFat, n.SodiumMg, n.CholesterolMg, n.Polyols);
}
