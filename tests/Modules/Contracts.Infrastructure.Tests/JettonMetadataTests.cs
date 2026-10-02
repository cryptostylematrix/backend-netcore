using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Contracts.Dto;
using Contracts.Infrastructure.Metadata;
using Contracts.Infrastructure.Queries;
using TonSdk.Core;
using TonSdk.Core.Boc;

namespace Contracts.Infrastructure.Tests;

public sealed class JettonMetadataTests
{
    private static Cell Snake(string text) => new CellBuilder().StoreUInt(0, 8).StoreBytes(Encoding.UTF8.GetBytes(text)).Build();
    private static Cell OnChain(params (string Key, Cell Value)[] values)
    {
        var dictionary = new HashmapE<Bits, Cell>(new()
        {
            KeySize = 256,
            Serializers = new() { Key = key => key, Value = value => new CellBuilder().StoreRef(value).Build() }
        });
        foreach (var (key, value) in values) dictionary.Set(new Bits(SHA256.HashData(Encoding.UTF8.GetBytes(key))), value);
        return new CellBuilder().StoreUInt(0, 8).StoreDict(dictionary).Build();
    }

    [Fact]
    public void Reads_name_symbol_and_zero_decimals_from_the_contract()
    {
        var result = JettonContentMetadata.Parse(OnChain(("name", Snake("Example Coin")), ("symbol", Snake("EXM")), ("decimals", Snake("0"))));
        Assert.Equal("Example Coin", result.Name);
        Assert.Equal("EXM", result.Symbol);
        Assert.Equal((byte)0, result.Decimals);
    }

    [Fact]
    public void Reads_an_off_chain_uri_across_snake_cells()
    {
        var tail = new CellBuilder().StoreBytes(Encoding.UTF8.GetBytes("token.json")).Build();
        var content = new CellBuilder().StoreUInt(1, 8).StoreBytes(Encoding.UTF8.GetBytes("https://example.com/")).StoreRef(tail).Build();
        Assert.Equal("https://example.com/token.json", JettonContentMetadata.Parse(content).Uri);
    }

    [Fact]
    public void Reads_chunked_values()
    {
        var chunks = new HashmapE<uint, Cell>(new()
        {
            KeySize = 32,
            Serializers = new() { Key = key => new BitsBuilder(32).StoreUInt(key, 32).Build(), Value = cell => new CellBuilder().StoreRef(cell).Build() }
        });
        chunks.Set(0, new CellBuilder().StoreBytes(Encoding.UTF8.GetBytes("Exa")).Build());
        chunks.Set(1, new CellBuilder().StoreBytes(Encoding.UTF8.GetBytes("mple")).Build());
        var value = new CellBuilder().StoreUInt(1, 8).StoreDict(chunks).Build();
        Assert.Equal("Example", JettonContentMetadata.Parse(OnChain(("name", value))).Name);
    }

    [Fact]
    public void On_chain_fields_take_precedence_and_missing_fields_use_external_json()
    {
        using var external = JsonDocument.Parse("""{"name":"External Name","symbol":"EXT","decimals":"18"}""");
        var result = JettonMetadataQueries.Merge("minter", new() { Symbol = "CHAIN", Decimals = 6 }, external.RootElement);
        Assert.Equal("External Name", result.Name);
        Assert.Equal("CHAIN", result.Symbol);
        Assert.Equal(6, result.Decimals);
    }

    [Theory]
    [InlineData("{\"decimals\":\"0\"}", 0)]
    [InlineData("{\"decimals\":6}", 6)]
    [InlineData("{}", 9)]
    public void Supports_numeric_and_string_decimals_and_the_standard_default(string json, byte expected)
    {
        using var external = JsonDocument.Parse(json);
        Assert.Equal(expected, JettonMetadataQueries.Merge("minter", new(), external.RootElement).Decimals);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("256")]
    [InlineData("null")]
    [InlineData("1.5")]
    public void Invalid_decimals_are_not_silently_used_to_format_money(string value)
    {
        using var external = JsonDocument.Parse("{\"decimals\":" + value + "}");
        Assert.Throws<FormatException>(() => JettonMetadataQueries.Merge("minter", new(), external.RootElement));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    public void Rejects_internal_metadata_destinations(string address) => Assert.False(JettonMetadataHttpClient.IsPublicAddress(IPAddress.Parse(address)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void Allows_public_addresses(string address) => Assert.True(JettonMetadataHttpClient.IsPublicAddress(IPAddress.Parse(address)));

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://localhost/metadata")]
    [InlineData("https://user:password@example.com/")]
    [InlineData("https://example.com:5004/")]
    [InlineData("http://169.254.169.254/latest/")]
    public void Rejects_unsafe_uris(string uri) => Assert.Throws<HttpRequestException>(() => JettonMetadataHttpClient.GetPublicUri(uri));

    [Theory]
    [InlineData("ipfs://bafyexample/token.json")]
    [InlineData("ipfs://ipfs/bafyexample/token.json")]
    public void Normalizes_ipfs_links(string uri) => Assert.Equal("https://ipfs.io/ipfs/bafyexample/token.json", JettonMetadataHttpClient.GetPublicUri(uri).AbsoluteUri);
}
