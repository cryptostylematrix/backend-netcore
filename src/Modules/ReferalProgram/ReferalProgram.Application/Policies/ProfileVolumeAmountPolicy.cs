using ReferalProgram.Application.Abstractions;
using ReferalProgram.Core.ProfileVolumeAggregate;

namespace ReferalProgram.Application.Policies;

public sealed class ProfileVolumeAmountPolicy : IProfileVolumeAmountPolicy
{
    public uint Resolve(ProfileVolumeOperation operation) => operation switch
    {
        ProfileVolumeOperation.BuyFirstPlace => 1,
        ProfileVolumeOperation.BuyPlace => 1,
        ProfileVolumeOperation.ActivatePlace => 1,
        ProfileVolumeOperation.CreateClone => 1,
        ProfileVolumeOperation.CreateReinvest => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
