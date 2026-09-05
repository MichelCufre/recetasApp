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
        services.AddScoped<BarFlow.Application.IProductSaleService, ProductSaleService>();
        services.AddScoped<BarFlow.Application.ISettingsService, SettingsService>();
        return services;
    }

    public static async Task InitializeDatabase(this IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsSqlite()) await db.Database.EnsureCreatedAsync();
        else await db.Database.MigrateAsync();
        if (db.Database.IsSqlite()) await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ProductSales" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ProductSales" PRIMARY KEY,
                "RecipeId" TEXT NOT NULL,
                "SoldAt" TEXT NOT NULL,
                "Quantity" INTEGER NOT NULL,
                "UnitPrice" TEXT NOT NULL,
                "UnitCostSnapshot" TEXT NOT NULL,
                "Channel" TEXT NULL,
                "Notes" TEXT NULL,
                CONSTRAINT "FK_ProductSales_Recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES "Recipes" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_ProductSales_RecipeId" ON "ProductSales" ("RecipeId");
            CREATE TABLE IF NOT EXISTS "Customers" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Customers" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Phone" TEXT NULL,
                "Notes" TEXT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_Customers_Name" ON "Customers" ("Name");
            """);
        if (db.Database.IsSqlite())
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM pragma_table_info('ProductSales') WHERE name='CustomerId'";
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) == 0)
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProductSales\" ADD COLUMN \"CustomerId\" TEXT NULL REFERENCES \"Customers\"(\"Id\") ON DELETE RESTRICT");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_ProductSales_CustomerId\" ON \"ProductSales\" (\"CustomerId\")");
            }
            await using var orderCheck = connection.CreateCommand();
            orderCheck.CommandText = "SELECT COUNT(*) FROM pragma_table_info('ProductSales') WHERE name='OrderId'";
            if (Convert.ToInt32(await orderCheck.ExecuteScalarAsync()) == 0)
            {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProductSales\" ADD COLUMN \"OrderId\" TEXT NULL");
                await db.Database.ExecuteSqlRawAsync("UPDATE \"ProductSales\" SET \"OrderId\" = \"Id\" WHERE \"OrderId\" IS NULL");
                await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_ProductSales_OrderId\" ON \"ProductSales\" (\"OrderId\")");
            }
        }
        await db.Database.ExecuteSqlRawAsync("UPDATE \"Recipes\" SET \"Category\" = CASE WHEN lower(\"Name\") LIKE '%granola%' THEN 'Granolas' ELSE 'Barritas' END WHERE \"Category\" IS NULL OR \"Category\" = '' OR \"Category\" IN ('Barrita proteica', 'Granola')");
        if (await db.Ingredients.AnyAsync()) return;
        var items = new[] {
            N("Proteína aislada de soja","Proteínas",8500,335,88.32m,0,0,0,3.39m,0.42m,1005), N("Whey vainilla","Proteínas",1250,371,78,8,5,0,3,1.5m,400,true),
            N("Harina de avena","Harinas",180,404,14.66m,65.7m,0.8m,6.5m,9.12m,1.61m,3), N("Inulina","Fibras",650,200,0,97,8,90,0,0,0,true),
            N("Dátiles","Frutas",280,282,2.45m,75.03m,63.35m,8,0.39m,0.03m,2), N("Mantequilla de maní","Frutos secos",390,588,25.09m,20,9.22m,6,50.39m,10.29m,17),
            N("Aceite de coco","Aceites",420,892,0,0,0,0,99.06m,82.48m,0,false,0.92m), N("Glicerina vegetal","Endulzantes",310,432,0,100,0,0,0,0,0,true,1.26m),
            N("Jarabe de glucosa","Endulzantes",240,316,0,79.38m,79.38m,0,0,0,70,true), N("Maní tostado","Frutos secos",360,587,24.35m,21.26m,4.9m,8.4m,49.66m,6.89m,6),
            N("Chocolate negro","Coberturas",2135,598,7.79m,45.9m,24,10.9m,42.63m,24.49m,20), N("Chocolate sin azúcar","Coberturas",2450,642,14.32m,28.4m,0.91m,16.6m,52.31m,32.27m,14,true),
            N("Cacao","Cacao",480,228,19.6m,57.9m,1.75m,37,13.7m,8.07m,21), N("Chía","Semillas",420,486,16.54m,42.12m,0,34.4m,30.74m,3.33m,16),
            N("Lino","Semillas",290,534,18.29m,28.88m,1.55m,27.3m,42.16m,3.66m,30), N("Nueces","Frutos secos",520,654,15.23m,13.71m,2.61m,6.7m,65.21m,6.13m,2),
            N("Harina de almendras","Harinas",680,579,21.15m,21.55m,4.35m,12.5m,49.93m,3.8m,1), N("Miel","Endulzantes",320,304,0.3m,82.4m,82.12m,0.2m,0,0,4),
            N("Naranja confitada","Frutas",450,322,0.34m,82.74m,80.68m,1.8m,0.07m,0.02m,24,true), N("Canela","Saborizantes",220,247,3.99m,80.59m,2.17m,53.1m,1.24m,0.35m,10),
            N("Vainilla","Saborizantes",900,288,0.06m,12.65m,12.65m,0,0.06m,0.01m,9), N("Sal","Saborizantes",70,0,0,0,0,0,0,0,38758),
            N("Flor de sal","Saborizantes",160,0,0,0,0,0,0,0,38758), N("Stevia","Endulzantes",390,0,0,0,0,0,0,0,0,true),
            N("Banana","Frutas",0,89,1.09m,22.84m,12.23m,2.6m,0.33m,0.11m,1), N("Banana deshidratada","Frutas",0,346,3.89m,88.28m,47.3m,9.9m,1.81m,0.7m,3)
        };
        db.Ingredients.AddRange(items); db.AppSettings.Add(new AppSetting()); var byName=items.ToDictionary(x=>x.Name);
        db.Recipes.AddRange(
            R("Caramelo Salado & Maní",12,120,[("Whey vainilla",40,RecipeSection.Dry),("Proteína aislada de soja",185,RecipeSection.Dry),("Harina de avena",35,RecipeSection.Dry),("Inulina",25,RecipeSection.Dry),("Dátiles",120,RecipeSection.Wet),("Glicerina vegetal",60,RecipeSection.Wet),("Jarabe de glucosa",60,RecipeSection.Wet),("Mantequilla de maní",100,RecipeSection.Filling),("Aceite de coco",20,RecipeSection.Wet),("Maní tostado",80,RecipeSection.Inclusions),("Chocolate negro",120,RecipeSection.Coating),("Vainilla",4,RecipeSection.Other),("Flor de sal",2,RecipeSection.Other)],byName),
            R("Dátil, Cacao & Canela",12,110,[("Proteína aislada de soja",180,RecipeSection.Dry),("Dátiles",240,RecipeSection.Wet),("Cacao",50,RecipeSection.Dry),("Harina de avena",70,RecipeSection.Dry),("Canela",5,RecipeSection.Other),("Chocolate negro",100,RecipeSection.Coating)],byName),
            R("Brownie Keto Premium",12,145,[("Proteína aislada de soja",170,RecipeSection.Dry),("Harina de almendras",150,RecipeSection.Dry),("Cacao",60,RecipeSection.Dry),("Mantequilla de maní",100,RecipeSection.Wet),("Aceite de coco",45,RecipeSection.Wet),("Nueces",90,RecipeSection.Inclusions),("Chocolate sin azúcar",120,RecipeSection.Coating)],byName,true,true),
            R("Chocolate & Naranja",12,130,[("Proteína aislada de soja",185,RecipeSection.Dry),("Harina de avena",60,RecipeSection.Dry),("Dátiles",190,RecipeSection.Wet),("Naranja confitada",70,RecipeSection.Inclusions),("Cacao",35,RecipeSection.Dry),("Chocolate negro",130,RecipeSection.Coating)],byName));
        await db.SaveChangesAsync();
    }
    private static Ingredient N(string name,string category,decimal net,decimal kcal,decimal protein,decimal carbs,decimal sugar,decimal fiber,decimal fat,decimal saturated,decimal sodium,bool estimated=false,decimal? density=null)=>new(){Name=name,Category=category,PackageQuantity=category=="Proteínas"?20:1,PurchaseUnit=Unit.Kg,BaseUnit=Unit.G,NetPrice=net,VatPercent=22,DensityGPerMl=density,StockCurrent=10000,StockMinimum=1000,Nutrition=new(){Kcal=kcal,Protein=protein,Carbohydrates=carbs,Sugars=sugar,Fiber=fiber,TotalFat=fat,SaturatedFat=saturated,SodiumMg=sodium,SourceType=estimated?NutritionSource.Estimated:NutritionSource.Usda,Source=estimated?"Referencia genérica; verificar etiqueta del proveedor":"USDA FoodData Central, valores por 100 g",SourceUrl=$"https://fdc.nal.usda.gov/food-search/?query={Uri.EscapeDataString(name)}"}};
    private static Recipe R(string name,int bars,decimal direct,(string name,decimal qty,RecipeSection section)[] lines,Dictionary<string,Ingredient> items,bool keto=false,bool sugarFree=false)=>new(){Name=name,Flavor=name,ExpectedBars=bars,TargetWeightPerBar=60,TargetProteinPerBar=20,WastePercent=3,PackageCostPerBar=2,LabelCostPerBar=1,LaborPerBatch=24,ElectricityPerBatch=10,IndirectCostsPercent=5,Keto=keto,SugarFree=sugarFree,Ingredients=lines.Select(x=>new RecipeIngredient{Ingredient=items[x.name],IngredientId=items[x.name].Id,Quantity=x.qty,Unit=Unit.G,Section=x.section}).ToList(),SalePrices=[new(){Channel=PriceChannel.Direct,Price=direct},new(){Channel=PriceChannel.Gym,Price=direct*0.75m},new(){Channel=PriceChannel.Wholesale,Price=direct*0.68m}]};
}
