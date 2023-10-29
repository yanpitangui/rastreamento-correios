using System.Text.Json.Serialization;
using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Routing;
using RastreamentoCorreios.Api;
using RastreamentoCorreios.Domain.PackageTracking;
using RastreamentoCorreios.Domain.Scraping;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// remove default logging providers
builder.Logging.ClearProviders();
// Serilog configuration        
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Register Serilog
builder.Logging.AddSerilog(logger);

builder.Services.AddAkka("package-tracking", (akkaBuilder, sp) =>
{
    var defaultShardOptions = new ShardOptions()
    {
        Role = "package-tracking",
        PassivateIdleEntityAfter = TimeSpan.FromMinutes(1),
        ShouldPassivateIdleEntities = true,
    };
    var packageMessageExtractor = new PackageMessageExtractor(50);
    akkaBuilder
        .BootstrapNetwork(builder.Configuration, "package-tracking", logger)
        .WithActors((system, registry) =>
    {
        var router = system.ActorOf(Props.Create<ScraperActor>()
            .WithRouter(new RoundRobinPool(5, new DefaultResizer(1, 10))));
        
        registry.Register<ScraperActor>(router);

    }).WithShardRegion<PackageActor>(nameof(RastreamentoCorreios.Domain.PackageTracking), (_, registry) => code =>
        PackageActor.Props(code, registry.Get<ScraperActor>()),
    packageMessageExtractor,
    defaultShardOptions);
});

builder.Services.AddHttpClient();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("track", async (string trackingCode, ActorRegistry registry) =>
{
    var tracker = registry.Get<PackageActor>();
    return await tracker.Ask<PackageState>(new PackageQueries.GetPackage(trackingCode));
});

app.UseHttpsRedirection();

app.Run();