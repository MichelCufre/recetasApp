using System.Text.Json.Serialization;
using BarFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Falta ConnectionStrings:Default"));
var app = builder.Build();
app.UseExceptionHandler("/error");
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
if (!app.Environment.IsEnvironment("Testing")) await app.Services.InitializeDatabase();
app.Run();

public partial class Program;
