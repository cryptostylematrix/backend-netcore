using Microsoft.EntityFrameworkCore;
using ReferalProgram.Core.ProfileVolumeAggregate;
using ReferalProgram.Infrastructure.Persistence;

namespace ReferalProgram.Infrastructure.Repositories;

internal sealed class ProfileVolumeRepository(DataContext dataContext)
    : IProfileVolumeRepository
{
    public Task IncreasePersonalAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint amount,
        CancellationToken cancellationToken) =>
        IncreaseAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            amount,
            personal: true,
            cancellationToken);

    public Task IncreaseReferralAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint amount,
        CancellationToken cancellationToken) =>
        IncreaseAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            amount,
            personal: false,
            cancellationToken);

    private Task IncreaseAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint amount,
        bool personal,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketingAddr);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileAddr);
        if (amount == 0)
            return Task.CompletedTask;

        var personalAmount = personal ? amount : 0u;
        var referralAmount = personal ? 0u : amount;

        return dataContext.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO public.profile_volumes
            (
                marketing_addr,
                structure_number,
                profile_addr,
                personal_volume,
                referral_volume,
                group_volume
            )
            VALUES
            (
                {{marketingAddr}},
                {{checked((short)structureNumber)}},
                {{profileAddr}},
                {{checked((long)personalAmount)}},
                {{checked((long)referralAmount)}},
                0
            )
            ON CONFLICT (marketing_addr, structure_number, profile_addr)
            DO UPDATE SET
                personal_volume = profile_volumes.personal_volume
                    + EXCLUDED.personal_volume,
                referral_volume = profile_volumes.referral_volume
                    + EXCLUDED.referral_volume;
            """, cancellationToken);
    }
}
