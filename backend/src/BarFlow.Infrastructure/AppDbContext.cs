using BarFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarFlow.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<IngredientNutrition> IngredientNutritions => Set<IngredientNutrition>();
    public DbSet<IngredientPriceHistory> IngredientPriceHistory => Set<IngredientPriceHistory>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<SalePrice> SalePrices => Set<SalePrice>();
    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();
    public DbSet<ProductionBatchIngredient> ProductionBatchIngredients => Set<ProductionBatchIngredient>();
    public DbSet<ProductSale> ProductSales => Set<ProductSale>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Ingredient>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Ingredient>().HasOne(x => x.Nutrition).WithOne().HasForeignKey<IngredientNutrition>(x => x.IngredientId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Ingredient>().HasMany(x => x.PriceHistory).WithOne().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Recipe>().HasMany(x => x.Ingredients).WithOne().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Recipe>().HasMany(x => x.SalePrices).WithOne().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RecipeIngredient>().HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SalePrice>().HasIndex(x => new { x.RecipeId, x.Channel }).IsUnique();
        b.Entity<ProductionBatch>().HasMany(x => x.Ingredients).WithOne().HasForeignKey(x => x.ProductionBatchId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProductionBatch>().HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProductSale>().HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProductSale>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProductSale>().HasIndex(x => x.OrderId);
        b.Entity<Customer>().HasIndex(x => x.Name);
        foreach (var p in b.Model.GetEntityTypes().SelectMany(e => e.GetProperties()).Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        { p.SetPrecision(18); p.SetScale(6); }
    }
}
