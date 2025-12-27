using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add the generated DbContext using the same connection string from appsettings.json
// Note: The DbContext class name is auto-generated based on the database name
// builder.Services.AddDbContext<NorthwindContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGet("/", () => "ASP.NET Core with JD.Efcpt.Build - appsettings.json sample");

// Example endpoint using the generated DbContext:
// app.MapGet("/customers", async (NorthwindContext db) =>
//     await db.Customers.Take(10).ToListAsync());

app.Run();
