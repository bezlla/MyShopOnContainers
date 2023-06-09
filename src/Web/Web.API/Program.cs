using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Serilog;
using Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((host, logger) =>
{
    logger.ReadFrom.Configuration(host.Configuration)
        .Destructure.AsScalar<JObject>()
        .Destructure.AsScalar<JArray>()
        .Enrich.FromLogContext();
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo {Title = "Web.API", Version = "v1"});
});            

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(policyBuilder => policyBuilder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapControllers();

app.Run();