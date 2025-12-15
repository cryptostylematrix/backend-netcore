using Contracts.Application.Features.Invite;
using Contracts.Application.Features.Multi;
using Contracts.Application.Features.Place;
using Contracts.Application.Features.ProfileItem;
using Contracts.Domain.Aggregates.Invite;
using Contracts.Domain.Aggregates.Multi;
using Contracts.Domain.Aggregates.Place;
using Contracts.Domain.Aggregates.ProfileItem;

namespace Contracts.Application;

public sealed class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        // Multi
        CreateMap<MinQueueTask, MinQueueTaskResponse>();

        CreateMap<MultiTaskItem, MultiTaskItemResponse>()
            .ForMember(a => a.ProfileAddr, b =>
                b.MapFrom(c => c.Profile.ToString()));

        CreateMap<MultiTaskCreatePlacePayload, MultiTaskPayloadResponse>()
            .ForMember(a => a.SourceAddr, b =>
                b.MapFrom(c => c.Source.ToString()));

        CreateMap<MultiTaskCreateClonePayload, MultiTaskPayloadResponse>();
        
        CreateMap<MultiTaskLockPosPayload, MultiTaskPayloadResponse>()
            .ForMember(a => a.SourceAddr, b =>
                b.MapFrom(c => c.Source.ToString()));
        
        CreateMap<MultiTaskUnlockPosPayload, MultiTaskPayloadResponse>()
            .ForMember(a => a.SourceAddr, b =>
                b.MapFrom(c => c.Source.ToString()));

        CreateMap<PlacePosData, PlacePosDataResponse>()
            .ForMember(a => a.ParentAddr, b =>
                b.MapFrom(c => c.Parent.ToString()));
        
        
        CreateMap<MultiData, MultiDataResponse>()
            .ForMember(a=> a.ProcessorAddr, b=> 
                b.MapFrom(c => c.Processor.ToString()));

        CreateMap<MultiFeesData, MultiFeesDataResponse>()
            .ForMember(a => a.M1, b =>
                b.MapFrom(c => c.M1.ToDecimal()))
            .ForMember(a => a.M2, b =>
                b.MapFrom(c => c.M2.ToDecimal()))
            .ForMember(a => a.M3, b =>
                b.MapFrom(c => c.M3.ToDecimal()))
            .ForMember(a => a.M4, b =>
                b.MapFrom(c => c.M4.ToDecimal()))
            .ForMember(a => a.M5, b =>
                b.MapFrom(c => c.M5.ToDecimal()))
            .ForMember(a => a.M6, b =>
                b.MapFrom(c => c.M6.ToDecimal()));
        
        CreateMap<MultiSecurityData, MultiSecurityDataResponse>()
            .ForMember(a=>a.AdminAddr, b =>
                b.MapFrom( c=>c.Admin.ToString()));
        
        CreateMap<MultiQueueItem, MultiQueueItemResponse>();
        
        
        // Invite
        CreateMap<InviteData, InviteDataResponse>()
            .ForMember(a => a.AdminAddr, b =>
                b.MapFrom(c => c.Admin.ToString()))
            .ForMember(a => a.ParentAddr, b =>
                b.MapFrom(c => c.Parent.ToString()));

        CreateMap<InviteOwner, InviteOwnerResponse>()
            .ForMember(a => a.OwnerAddr, b =>
                b.MapFrom(c => c.Owner.ToString()));
        
        // ProfileItem
        CreateMap<ProfileData, ProfileDataResponse>()
            .ForMember(a=> a.CollectionAddr, b=> 
                b.MapFrom(c => c.Collection.ToString()))
            .ForMember(a=>a.OwnerAddr, b=> 
                b.MapFrom(c=>c.Owner.ToString()));

        CreateMap<ProfileContent, ProfileContentResponse>();
        
        CreateMap<ProfilePrograms, ProfileProgramsResponse>();
        CreateMap<ProgramData, ProgramDataResponse>()
            .ForMember(a=>a.InviterAddr, b=>
                b.MapFrom(c => c.Inviter.ToString()))
            .ForMember(a=>a.InviteAddr, b=> 
                b.MapFrom(c => c.Invite.ToString()));
        
        // Place
        CreateMap<PlaceData, PlaceDataResponse>()
            .ForMember(a => a.MarketingAddr, b =>
                b.MapFrom(c => c.Marketing.ToString()))
            .ForMember(a => a.ParentAddr, b =>
                b.MapFrom(c => c.Parent.ToString()));

        CreateMap<PlaceProfiles, PlaceProfilesResponse>()
            .ForMember(a => a.ProfileAddr, b =>
                b.MapFrom(c => c.Profile.ToString()))
            .ForMember(a => a.InviterProfileAddr, b =>
                b.MapFrom(c => c.InviterProfile.ToString()));

        CreateMap<PlaceSecurity, PlaceSecurityResponse>()
            .ForMember(a => a.AdminAddr, b =>
                b.MapFrom(c => c.Admin.ToString()));

        CreateMap<PlaceChildren, PlaceChildrenResponse>()
            .ForMember(a => a.LeftAddr, b =>
                b.MapFrom(c => c.Left.ToString()))
            .ForMember(a => a.RightAddr, b =>
                b.MapFrom(c => c.Right.ToString()));

    }
}