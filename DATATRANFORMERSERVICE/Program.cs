using DATATRANFORMERSERVICE;
using DVGA25_Datatransformer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<Consumer>();
builder.Services.AddSingleton<Producer>();
builder.Services.AddSingleton<dataTransformerService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
