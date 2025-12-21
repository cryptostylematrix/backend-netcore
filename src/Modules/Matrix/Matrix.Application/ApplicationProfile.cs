using Matrix.Application.Features.Locks;
using Matrix.Application.Features.Places;
using Matrix.Domain.Aggregates;

namespace Matrix.Application;

public sealed class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        CreateMap<MultiPlace, PlaceResponse>()
            .ForMember(a => a.FillCount, b =>
                b.MapFrom(c => c.Filling2))
            .ForMember(a => a.Login, b =>
                b.MapFrom(c => c.ProfileLogin));

        CreateMap<MultiLock, LockResponse>();
    }
}