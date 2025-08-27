using CongesApi.Data;
using CongesApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

var builder = WebApplication.CreateBuilder(args);

// Db
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (si besoin)
const string FrontendCors = "FrontendCors";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(FrontendCors, p => p
        .WithOrigins("http://localhost:3000", "https://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// Playwright + Browser
builder.Services.AddSingleton<IPlaywright>(sp =>
    Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult());

builder.Services.AddSingleton<IBrowser>(sp =>
{
    var pw = sp.GetRequiredService<IPlaywright>();
    return pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
        Headless = true,
        Args = new[] { "--no-sandbox" }
    }).GetAwaiter().GetResult();
});

// 🔑 DI de l’interface Pdf
builder.Services.AddSingleton<IPdfRenderer, PdfRenderer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Sert wwwroot (assets, signatures…)
app.UseStaticFiles();

app.UseCors(FrontendCors);
app.UseAuthorization();
app.MapControllers();
app.Run();
