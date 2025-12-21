using Contracts.Infrastructure;
using dotenv.net;
using Matrix.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Load env based on environment
var envFile = builder.Environment.IsDevelopment()
    ? ".env.development"
    : ".env.production";

DotEnv.Load(options: new DotEnvOptions(
    envFilePaths: [ envFile ],
    ignoreExceptions: true
));

// IMPORTANT: re-add environment variables so configuration sees what DotEnv just loaded
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddContractsModule(builder.Configuration);
builder.Services.AddMatrixModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

ContractsModule.MapEndpoints(app);


app.Run();
