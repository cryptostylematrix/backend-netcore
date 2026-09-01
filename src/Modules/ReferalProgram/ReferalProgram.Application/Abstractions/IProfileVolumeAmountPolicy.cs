using ReferalProgram.Core.ProfileVolumeAggregate;

namespace ReferalProgram.Application.Abstractions;

public interface IProfileVolumeAmountPolicy
{
    uint Resolve(ProfileVolumeOperation operation);
}
