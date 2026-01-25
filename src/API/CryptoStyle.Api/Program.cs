using Contracts.Infrastructure;
using dotenv.net;
using FastEndpoints;
using Matrix.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Load env based on environment
var envFile = builder.Environment.IsDevelopment()
    ? ".env.development"
    : ".env";

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

// Distributed cache (choose ONE):
// 1) In-memory distributed cache (dev / single instance)
builder.Services.AddDistributedMemoryCache();

// 2) When you move to Redis later, replace the above with:
// services.AddStackExchangeRedisCache(o =>
// {
//     o.Configuration = configuration.GetConnectionString("Redis");
//     o.InstanceName = "contracts:";
// });


builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCors", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    return true;

                if (uri.Scheme == Uri.UriSchemeHttps &&
                    uri.Host.Equals("cryptostylematrix.github.io", StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            })
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization", "X-Requested-With");
    });

    // 2Open policy (allow all hosts)
    options.AddPolicy("OpenCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseCors("RestrictedCors");
app.UseCors("OpenCors");

app.UseFastEndpoints();

app.Run();