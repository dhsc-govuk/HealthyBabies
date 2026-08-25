using Application.DataCollections.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IDeliveryPlanReviewService
{
    Task<Option<ReviewSectionDetailDto>> GetSection(
        Guid dataCollectionId,
        Guid localAuthorityId,
        Guid formModuleId,
        CancellationToken cancellationToken);

    Task<Option<ReviewActivityDetailDto>> GetActivity(
        Guid dataCollectionId,
        Guid localAuthorityId,
        Guid formModuleId,
        Guid activityId,
        CancellationToken cancellationToken);
}