using BarFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BarFlow.Infrastructure;

public static class InfrastructureSetup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(o =>
        {
            if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)) o.UseSqlite(connectionString);
            else o.UseNpgsql(connectionString);
        });
        services.AddScoped<BarFlow.Application.IIngredientService, IngredientService>();
        services.AddScoped<BarFlow.Application.IRecipeService, RecipeService>();
        services.AddScoped<BarFlow.Application.IProductionService, ProductionService>();
        services.AddScoped<BarFlow.Application.ISettingsService, SettingsService>();
        return services;
    }

    public static async Task InitializeDatabase(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsSqlite()) await db.Database.EnsureCreatedAsync();
        else await db.Database.MigrateAsync();
        if (await db.Ingredients.AnyAsync()) return;
        var items = new[] {
            I("Proteína aislada de soja","Proteínas",8500,90,2,1,0.5m), I("Whey vainilla","Proteínas",1250,78,8,5,5), I("Harina de avena","Harinas",180,13,67,1,7), I("Inulina","Fibras",650,0,8,0,90),
            I("Dátiles","Frutas",280,2,75,66,8), I("Mantequilla de maní","Frutos secos",390,25,20,6,8,50), I("Aceite de coco","Aceites",420,0,0,0,0,100,82,0.92m), I("Glicerina vegetal","Endulzantes",310,0,100,0,0,0,0,1.26m),
            I("Jarabe de glucosa","Endulzantes",240,0,80,80,0), I("Maní tostado","Frutos secos",360,26,16,5,9,49), I("Chocolate negro","Coberturas",2135,8,46,24,11,43,26), I("Chocolate sin azúcar","Coberturas",2450,9,38,1,12,48,29),
            I("Cacao","Cacao",480,20,58,2,33,14), I("Chía","Semillas",420,17,42,0,34,31), I("Lino","Semillas",290,18,29,2,27,42), I("Nueces","Frutos secos",520,15,14,3,7,65),
            I("Harina de almendras","Harinas",680,21,20,4,11,50), I("Miel","Endulzantes",320,0.3m,82,82,0), I("Naranja confitada","Frutas",450,1,79,72,3), I("Canela","Saborizantes",220,4,81,2,53),
            I("Vainilla","Saborizantes",900,0,13,13,0), I("Sal","Saborizantes",70,0,0,0,0,0,0), I("Flor de sal","Saborizantes",160,0,0,0,0,0,0), I("Stevia","Endulzantes",390,0,0,0,0)
        };
        db.Ingredients.AddRange(items); db.AppSettings.Add(new AppSetting()); var byName=items.ToDictionary(x=>x.Name);
        db.Recipes.AddRange(
            R("Caramelo Salado & Maní",12,120,[("Whey vainilla",40,RecipeSection.Dry),("Proteína aislada de soja",185,RecipeSection.Dry),("Harina de avena",35,RecipeSection.Dry),("Inulina",25,RecipeSection.Dry),("Dátiles",120,RecipeSection.Wet),("Glicerina vegetal",60,RecipeSection.Wet),("Jarabe de glucosa",60,RecipeSection.Wet),("Mantequilla de maní",100,RecipeSection.Filling),("Aceite de coco",20,RecipeSection.Wet),("Maní tostado",80,RecipeSection.Inclusions),("Chocolate negro",120,RecipeSection.Coating),("Vainilla",4,RecipeSection.Other),("Flor de sal",2,RecipeSection.Other)],byName),
            R("Dátil, Cacao & Canela",12,110,[("Proteína aislada de soja",180,RecipeSection.Dry),("Dátiles",240,RecipeSection.Wet),("Cacao",50,RecipeSection.Dry),("Harina de avena",70,RecipeSection.Dry),("Canela",5,RecipeSection.Other),("Chocolate negro",100,RecipeSection.Coating)],byName),
            R("Brownie Keto Premium",12,145,[("Proteína aislada de soja",170,RecipeSection.Dry),("Harina de almendras",150,RecipeSection.Dry),("Cacao",60,RecipeSection.Dry),("Mantequilla de maní",100,RecipeSection.Wet),("Aceite de coco",45,RecipeSection.Wet),("Nueces",90,RecipeSection.Inclusions),("Chocolate sin azúcar",120,RecipeSection.Coating)],byName,true,true),
            R("Chocolate & Naranja",12,130,[("Proteína aislada de soja",185,RecipeSection.Dry),("Harina de avena",60,RecipeSection.Dry),("Dátiles",190,RecipeSection.Wet),("Naranja confitada",70,RecipeSection.Inclusions),("Cacao",35,RecipeSection.Dry),("Chocolate negro",130,RecipeSection.Coating)],byName));
        await db.SaveChangesAsync();
    }
    private static Ingredient I(string name,string category,decimal net,decimal protein,decimal carbs,decimal sugar,decimal fiber,decimal fat=2,decimal saturated=0,decimal? density=null)=>new(){Name=name,Category=category,PackageQuantity=category=="Proteínas"?20:1,PurchaseUnit=Unit.Kg,BaseUnit=Unit.G,NetPrice=net,VatPercent=22,DensityGPerMl=density,StockCurrent=10000,StockMinimum=1000,Nutrition=new(){Kcal=protein*4+(carbs-fiber)*4+fat*9,Protein=protein,Carbohydrates=carbs,Sugars=sugar,Fiber=fiber,TotalFat=fat,SaturatedFat=saturated,SourceType=NutritionSource.Estimated,Source="Datos iniciales editables"}};
    private static Recipe R(string name,int bars,decimal direct,(string name,decimal qty,RecipeSection section)[] lines,Dictionary<string,Ingredient> items,bool keto=false,bool sugarFree=false)=>new(){Name=name,Flavor=name,ExpectedBars=bars,TargetWeightPerBar=60,TargetProteinPerBar=20,WastePercent=3,PackageCostPerBar=2,LabelCostPerBar=1,LaborPerBatch=24,ElectricityPerBatch=10,IndirectCostsPercent=5,Keto=keto,SugarFree=sugarFree,Ingredients=lines.Select(x=>new RecipeIngredient{Ingredient=items[x.name],IngredientId=items[x.name].Id,Quantity=x.qty,Unit=Unit.G,Section=x.section}).ToList(),SalePrices=[new(){Channel=PriceChannel.Direct,Price=direct},new(){Channel=PriceChannel.Gym,Price=direct*0.75m},new(){Channel=PriceChannel.Wholesale,Price=direct*0.68m}]};
}
