using CongesApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Ajouter les services au container
builder.Services.AddControllers();

// Configurer la connexion à la base de données SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurer CORS pour autoriser les requêtes depuis React (localhost:3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Si tu envoies des cookies ou des headers d’authentification
    });
});

// Ajouter Swagger pour la documentation en développement
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Activer Swagger en environnement de développement
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirection vers HTTPS
app.UseHttpsRedirection();

// Activer le CORS (?? toujours avant Authorization)
app.UseCors("AllowReactApp");

app.UseAuthorization();

// Ajouter les contrôleurs (API)
app.MapControllers();

app.Run();
