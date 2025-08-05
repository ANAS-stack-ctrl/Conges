using CongesApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Ajouter les services au container
builder.Services.AddControllers();

// Configurer la connexion � la base de donn�es SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurer CORS pour autoriser les requ�tes depuis React (localhost:3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Si tu envoies des cookies ou des headers d�authentification
    });
});

// Ajouter Swagger pour la documentation en d�veloppement
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Activer Swagger en environnement de d�veloppement
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

// Ajouter les contr�leurs (API)
app.MapControllers();

app.Run();
