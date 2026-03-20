using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Entity Framework Core with SQL Server
builder.Services.AddDbContext<RpgContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=RpgDatabase;Trusted_Connection=true;";
    options.UseSqlServer(connectionString);
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RpgContext>();
    db.Database.EnsureCreated();

    // Seed character classes if not already present
    if (!db.Classes.Any())
    {
        var classes = new List<RpgApi.Models.Class>
        {
            new RpgApi.Models.Mage(),
            new RpgApi.Models.Warrior(),
            new RpgApi.Models.Archer(),
            new RpgApi.Models.Rogue()
        };

        db.Classes.AddRange(classes);
        db.SaveChanges();
    }
}

app.Run();
