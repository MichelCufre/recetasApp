using BarFlow.Domain;

namespace BarFlow.Domain.Tests;

public class CalculatorTests
{
    private readonly UnitConversionService _units = new();

    [Fact] public void Converts_mass_and_volume_units()
    {
        Assert.Equal(20000m, _units.Convert(20m, Unit.Kg, Unit.G));
        Assert.Equal(3300m, _units.Convert(3.3m, Unit.Kg, Unit.G));
        Assert.Equal(920m, _units.Convert(1m, Unit.L, Unit.G, .92m));
    }

    [Fact] public void Rejects_incompatible_units_without_density() =>
        Assert.Throws<InvalidOperationException>(() => _units.Convert(1, Unit.Ml, Unit.G));

    [Fact] public void Calculates_cost_per_base_unit_with_vat()
    {
        var ingredient = Ingredient(net:1750, package:3.3m, vat:22);
        Assert.Equal(0.646970m, Math.Round(_units.CostPerBaseUnit(ingredient), 6));
    }

    [Fact] public void Calculates_recipe_cost_and_cost_per_bar()
    {
        var recipe = Recipe(185, 12); var result = new RecipeCalculator().Analyze(recipe);
        Assert.Equal(112.85m, result.Cost.Ingredients);
        Assert.Equal(Math.Round(result.Cost.Total / 12, 6), Math.Round(result.Cost.PerBar, 6));
    }

    [Fact] public void Calculates_nutrition_total_per_bar_and_per_100g()
    {
        var result = new RecipeCalculator().Analyze(Recipe(200, 10));
        Assert.Equal(180m, result.TotalNutrition.Protein);
        Assert.Equal(18m, result.PerBar.Protein);
        Assert.Equal(90m, result.Per100G.Protein);
    }

    [Fact] public void Uses_real_weight_for_per_100g_values()
    {
        var recipe = Recipe(200, 10); recipe.FinalBatchWeight = 180;
        var result = new RecipeCalculator().Analyze(recipe);
        Assert.Equal(100m, Math.Round(result.Per100G.Protein, 6));
        Assert.Equal(10m, result.RealWastePercent);
    }

    [Fact] public void Calculates_margin_and_markup_separately()
    {
        var p = new RecipeCalculator().Profit(120, 40, 12);
        Assert.Equal(66.666667m, Math.Round(p.MarginPercent, 6));
        Assert.Equal(200m, p.MarkupPercent);
        Assert.Equal(960m, p.ProfitPerBatch);
    }

    [Fact] public void Calculates_waste_cost_and_indirect_cost_without_early_rounding()
    {
        var recipe = Recipe(100, 10); recipe.WastePercent=5; recipe.IndirectCostsPercent=10;
        var result = new RecipeCalculator().Analyze(recipe);
        Assert.Equal(result.Cost.Ingredients*.05m,result.Cost.Waste);
        Assert.Equal((result.Cost.Ingredients+result.Cost.Waste)*.10m,result.Cost.Indirect);
    }

    [Fact] public void Suggests_protein_ingredient_quantity_for_target()
    {
        var recipe=Recipe(100,10); recipe.TargetProteinPerBar=12;
        var suggestion=new RecipeCalculator().ProteinIngredientSuggestion(recipe,recipe.Ingredients[0].Ingredient);
        Assert.Equal(33.333333m,Math.Round(suggestion,6));
    }

    private static Ingredient Ingredient(decimal net=500,decimal package=1,decimal vat=22) => new()
    {
        Name="Aislado",NetPrice=net,PackageQuantity=package,PurchaseUnit=Unit.Kg,BaseUnit=Unit.G,VatPercent=vat,
        Nutrition=new(){Protein=90,Kcal=380,Carbohydrates=2,TotalFat=1}
    };
    private static Recipe Recipe(decimal grams,int bars)
    {
        var ingredient=Ingredient();
        return new(){Name="Prueba",ExpectedBars=bars,WastePercent=0,Ingredients=[new(){IngredientId=ingredient.Id,Ingredient=ingredient,Quantity=grams,Unit=Unit.G}]};
    }
}
