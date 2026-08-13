using Contracts.Infrastructure;
using CryptoStyle.Api.BackgroundServices;
using dotenv.net;
using FastEndpoints;
using Matrix.Infrastructure;
using ReferalProgram.Infrastructure;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using ContractsPresentation = Contracts.Presentation.PresentationReference;
using MarketingPresentation = Marketing.Presentation.PresentationReference;
using MatrixPresentation = Matrix.Presentation.PresentationReference;
using ReferalProgramPresentation = ReferalProgram.Presentation.PresentationReference;

var builder = WebApplication.CreateBuilder(args);

// Load env based on environment
var envFileName = builder.Environment.IsDevelopment()
    ? ".env.development"
    : ".env";

var envFile = new[]
    {
        Path.Combine(builder.Environment.ContentRootPath, envFileName),
        Path.Combine(
            builder.Environment.ContentRootPath,
            "src",
            "API",
            "CryptoStyle.Api",
            envFileName),
        Path.Combine(AppContext.BaseDirectory, envFileName)
    }
    .FirstOrDefault(File.Exists);

if (envFile is not null)
{
    DotEnv.Load(options: new DotEnvOptions(
        envFilePaths: [envFile],
        ignoreExceptions: false));
}

// IMPORTANT: re-add environment variables so configuration sees what DotEnv just loaded
builder.Configuration.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
    SelfLog.Enable(message => Console.Error.WriteLine("Serilog: {0}", message));

builder.Host.UseSerilog((_, _, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var seqUrl = builder.Configuration["SEQ_URL"];
    if (builder.Environment.IsProduction()
        && !string.IsNullOrWhiteSpace(seqUrl))
    {
        loggerConfiguration.WriteTo.Seq(
            seqUrl,
            apiKey: builder.Configuration["SEQ_API_KEY"]);
    }
});

builder.Services.AddFastEndpoints(options =>
{
    options.Assemblies =
    [
        ContractsPresentation.Assembly,
        MatrixPresentation.Assembly,
        MarketingPresentation.Assembly,
        ReferalProgramPresentation.Assembly
    ];
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Multiple modules expose DTOs with the same class name (for example,
    // Matrix.Dto.PlaceResponse and Marketing.Dto.PlaceResponse).
    options.CustomSchemaIds(type =>
        type.FullName?.Replace("+", ".") ?? type.Name);
});

builder.Services.AddContractsModule(builder.Configuration);
builder.Services.AddMatrixModule(builder.Configuration);
builder.Services.AddMarketingModule(builder.Configuration);
builder.Services.AddReferalProgramModule(builder.Configuration);

builder.Services.AddOptions<TaskProcessorOptions>()
    .Bind(builder.Configuration.GetSection(TaskProcessorOptions.SectionName))
    .Validate(options => options.IntervalSeconds > 0,
        "TaskProcessor.IntervalSeconds must be greater than zero.")
    .ValidateOnStart();

//if (!builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<TaskProcessor>();

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

app.Logger.LogInformation(
    "CryptoStyle API logging initialized for {EnvironmentName}; Seq enabled: {SeqEnabled}",
    app.Environment.EnvironmentName,
    app.Environment.IsProduction()
        && !string.IsNullOrWhiteSpace(builder.Configuration["SEQ_URL"]));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseCors("RestrictedCors");
app.UseCors("OpenCors");

app.UseFastEndpoints();

app.Run();
