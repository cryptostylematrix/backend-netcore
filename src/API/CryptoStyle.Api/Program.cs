using Contracts.Infrastructure;
using Matrix.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
