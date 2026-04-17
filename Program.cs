using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using WeatherService.Data;
using WeatherService.Options;
using WeatherService.Providers;
using WeatherService.Services;

var builder = WebApplication.CreateBuilder(args);
var weatherProviderOptions = builder.Configuration.GetSection(WeatherProviderOptions.SectionName).Get<WeatherProviderOptions>() ?? new WeatherProviderOptions();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<WeatherProviderOptions>(builder.Configuration.GetSection(WeatherProviderOptions.SectionName));
builder.Services.Configure<WeatherCacheOptions>(builder.Configuration.GetSection(WeatherCacheOptions.SectionName));

builder.Services.AddMemoryCache();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("WeatherDatabase") ?? "Data Source=weather.db"));

builder.Services.AddHttpClient<IWeatherProvider, OpenMeteoProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(weatherProviderOptions.TimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherService/1.0");
});

builder.Services.AddScoped<IWeatherService, WeatherServiceLogic>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather Service API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".png"] = "image/png";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "/static",
    ContentTypeProvider = contentTypeProvider
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapControllers();

app.Run();
