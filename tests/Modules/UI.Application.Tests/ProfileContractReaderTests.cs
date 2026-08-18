using Ardalis.Result;
using Contracts.Application.Features.ProfileCollection;
using Contracts.Application.Features.ProfileItem;
using Contracts.Dto;
using MediatR;
using UI.Application.Services;
using Xunit;

namespace UI.Application.Tests;

public sealed class ProfileContractReaderTests
{
    [Fact]
    public async Task Reads_current_profile_data_without_using_the_cached_query()
    {
        var sender = new SenderStub();
        var reader = new ProfileContractReader(sender);

        var result = await reader.GetByLoginAsync(" Alice ", default);

        Assert.True(result.IsSuccess);
        Assert.Equal("wallet", result.Profile!.OwnerAddr);
        Assert.True(sender.FreshProfileDataRequested);
    }

    private sealed class SenderStub : ISender
    {
        public bool FreshProfileDataRequested { get; private set; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            object response = request switch
            {
                GetNftAddressByLoginQuery query
                    when query.Login == "alice" => Result.Success(new NftAddressResponse
                    {
                        Addr = "profile"
                    }),
                GetFreshNftDataQuery => FreshProfileData(),
                GetNftDataQuery => throw new InvalidOperationException(
                    "The cached NFT data query must not be used for ownership checks."),
                _ => throw new NotSupportedException(request.GetType().Name)
            };

            return Task.FromResult((TResponse)response);
        }

        private Result<ProfileDataResponse> FreshProfileData()
        {
            FreshProfileDataRequested = true;
            return Result.Success(new ProfileDataResponse
            {
                OwnerAddr = "wallet",
                Content = new ProfileContentResponse
                {
                    Login = "alice"
                }
            });
        }

        public Task Send<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();

        public Task<object?> Send(
            object request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
