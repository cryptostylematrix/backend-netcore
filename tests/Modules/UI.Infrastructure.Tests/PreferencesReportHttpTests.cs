using System.Net;
using System.Text.Json;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using UI.Application.Abstractions;
using UI.Application.Features.Reports;
using UI.Dto;
using UI.Presentation.Endpoints.Reports;
using Xunit;

namespace UI.Infrastructure.Tests;

public sealed class PreferencesReportHttpTests
{
    [Fact]
    public async Task Get_without_query_or_body_returns_preferences()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddFastEndpoints(options => {
            options.Assemblies = [typeof(PreferencesReportEndpoint).Assembly];
            options.DisableAutoDiscovery = true;
            options.Filter = type => type == typeof(PreferencesReportEndpoint);
        });
        builder.Services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<GetPreferencesReportQuery>());
        builder.Services.AddSingleton(TimeProvider.System);
        var queries = new Queries();
        builder.Services.AddSingleton<IUiReportQueries>(queries);
        await using var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseFastEndpoints();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            using var response = await client.GetAsync("/api/ui/reports/preferences");
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {body}");
            using var json = JsonDocument.Parse(body);
            Assert.Equal(3, json.RootElement.GetProperty("data").GetProperty("total").GetInt64());
            Assert.Equal("en", json.RootElement.GetProperty("data").GetProperty("groups")[0].GetProperty("language").GetString());
            Assert.True(response.Headers.CacheControl!.NoStore);
            Assert.Equal(1, queries.Calls);
        }
        finally { await app.StopAsync(); }
    }
    private sealed class Queries : IUiReportQueries
    {
        public int Calls;
        public Task<PreferenceReport> GetPreferencesAsync(UiReportFilter filter, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new PreferenceReport { Total = 3, Groups = [new PreferenceReportGroup { Language = "en", Count = 3, Percentage = 100 }] });
        }
        public Task<UiReportResponse> GetAsync(UiReportFilter filter, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProfileReport> GetProfilesAsync(UiReportFilter filter, CancellationToken ct) => throw new NotSupportedException();
        public Task<ConnectionReport> GetTonConnectAsync(UiReportFilter filter, CancellationToken ct) => throw new NotSupportedException();
        public Task<ActivityReport> GetActivityAsync(UiReportFilter filter, CancellationToken ct) => throw new NotSupportedException();
    }
}
