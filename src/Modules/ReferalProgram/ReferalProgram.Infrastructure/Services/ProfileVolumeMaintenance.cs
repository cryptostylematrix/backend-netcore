using Microsoft.EntityFrameworkCore;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Infrastructure.Persistence;

namespace ReferalProgram.Infrastructure.Services;

internal sealed class ProfileVolumeMaintenance(DataContext dataContext)
    : IProfileVolumeMaintenance
{
    public async Task RecalculateReferralAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dataContext.Database.BeginTransactionAsync(
            cancellationToken);

        await ResetReferralAsync(marketingAddr, structureNumber, cancellationToken);

        await dataContext.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO public.profile_volumes
            (
                marketing_addr,
                structure_number,
                profile_addr,
                personal_volume,
                referral_volume,
                group_volume
            )
            SELECT {{marketingAddr}},
                   {{checked((short)structureNumber)}},
                   invite.parent_profile_addr,
                   0,
                   COUNT(*)::bigint,
                   0
            FROM public.places place
            JOIN public.places invite
              ON invite.marketing_addr = place.marketing_addr
             AND invite.structure_number = 0
             AND invite.place_number = 1
             AND invite.profile_addr = place.profile_addr
            WHERE place.marketing_addr = {{marketingAddr}}
              AND place.structure_number = {{checked((short)structureNumber)}}
              AND place.profile_addr IS NOT NULL
              AND place.activated_at IS NOT NULL
              AND invite.parent_profile_addr IS NOT NULL
            GROUP BY invite.parent_profile_addr
            ON CONFLICT (marketing_addr, structure_number, profile_addr)
            DO UPDATE SET referral_volume = EXCLUDED.referral_volume;
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public Task ResetReferralAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken) =>
        dataContext.Database.ExecuteSqlInterpolatedAsync($$"""
            UPDATE public.profile_volumes
            SET referral_volume = 0
            WHERE marketing_addr = {{marketingAddr}}
              AND structure_number = {{checked((short)structureNumber)}};
            """, cancellationToken);
}
