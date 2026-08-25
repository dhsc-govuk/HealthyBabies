using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

/// <summary>
/// Accepts a local authority's submitted delivery plan. Surfaced to users as
/// "Accept submission"; the resulting status is Approved, which is displayed as "Accepted".
/// </summary>
public record ApproveDeliveryPlanSubmissionCommand : IRequest<Either<DataCollectionSubmissionException, DataCollectionSubmission>>
{
    public Guid DataCollectionId { get; init; }
    public Guid LocalAuthorityId { get; init; }
    public Guid ReviewedByUserId { get; init; }
    public string? Comment { get; init; }
}

public class ApproveDeliveryPlanSubmissionCommandHandler(
    IDataCollectionSubmissionRepository submissionRepository,
    IDataCollectionQueries dataCollectionQueries)
    : IRequestHandler<ApproveDeliveryPlanSubmissionCommand, Either<DataCollectionSubmissionException, DataCollectionSubmission>>
{
    public async Task<Either<DataCollectionSubmissionException, DataCollectionSubmission>> Handle(
        ApproveDeliveryPlanSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByDataCollectionAndOrganisationAsync(
            new DataCollectionId(request.DataCollectionId),
            new OrganisationId(request.LocalAuthorityId),
            cancellationToken);

        return await submission.MatchAsync(
            s => Approve(s, request, cancellationToken),
            () => new DataCollectionSubmissionNotFoundException(request.DataCollectionId, request.LocalAuthorityId));
    }

    private async Task<Either<DataCollectionSubmissionException, DataCollectionSubmission>> Approve(
        DataCollectionSubmission submission,
        ApproveDeliveryPlanSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        // Accepting is the Delivery Plan approval flow only. A management information quarter's
        // self-service reopen never allows reopening an Approved submission, so approving one here
        // would lock it permanently with no recovery path.
        var dataCollection = (await dataCollectionQueries.GetByIdAsync(
                new DataCollectionId(request.DataCollectionId), cancellationToken))
            .MatchUnsafe(dc => dc, () => (DataCollection?)null);

        if (dataCollection is not null && dataCollection.Type.Value != DataCollectionTypeConstants.DeliveryPlan)
        {
            return new DataCollectionSubmissionWrongTypeException(dataCollection.Type.Value);
        }

        if (submission.Status != DataCollectionSubmissionStatus.Submitted)
        {
            return new DataCollectionSubmissionNotAwaitingReviewException(submission.Status.ToString());
        }

        try
        {
            submission.Approve(new UserId(request.ReviewedByUserId), request.Comment);
            return await submissionRepository.UpdateAsync(submission, cancellationToken);
        }
        catch (Exception exception)
        {
            return new DataCollectionSubmissionUnknownException(exception);
        }
    }
}