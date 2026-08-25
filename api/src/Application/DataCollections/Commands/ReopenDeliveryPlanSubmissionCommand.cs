using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

/// <summary>
/// Reopens a local authority's submitted delivery plan so it can be amended and resubmitted.
///
/// Surfaced to users as "Reopen submission", but the recorded domain action is
/// <see cref="DataCollectionSubmission.Reject"/> - that is what captures the mandatory reason,
/// the reviewing user and the timestamp. Do not route this through
/// <see cref="DataCollectionSubmission.Reopen"/>, which belongs to the management information
/// quarterly flow and records none of that.
///
/// The new resubmit-by date is not part of the submission aggregate: it lives on
/// DataCollectionLocalAuthority.EndDate and is written through its own repository, parent first,
/// per the nested-entity rules.
/// </summary>
public record ReopenDeliveryPlanSubmissionCommand : IRequest<Either<DataCollectionSubmissionException, DataCollectionSubmission>>
{
    public Guid DataCollectionId { get; init; }
    public Guid LocalAuthorityId { get; init; }
    public Guid ReviewedByUserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime ResubmitBy { get; init; }
    public IReadOnlyList<Guid> SectionsToReview { get; init; } = [];

    /// <summary>
    /// The submission's current state, exported to JSON by the caller before this command runs
    /// (see DeliveryPlanApprovalController.Reopen). Read here, not built here - ISubmissionExportService
    /// lives in the Api project and this command must not depend on it. Frozen into a
    /// DataCollectionSubmissionVersion in the same transaction as the rejection itself.
    /// </summary>
    public string Content { get; init; } = string.Empty;
}

public class ReopenDeliveryPlanSubmissionCommandHandler(
    IDataCollectionSubmissionRepository submissionRepository,
    IDataCollectionRepository dataCollectionRepository,
    IDataCollectionSubmissionVersionRepository versionRepository)
    : IRequestHandler<ReopenDeliveryPlanSubmissionCommand, Either<DataCollectionSubmissionException, DataCollectionSubmission>>
{
    public async Task<Either<DataCollectionSubmissionException, DataCollectionSubmission>> Handle(
        ReopenDeliveryPlanSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.GetByDataCollectionAndOrganisationAsync(
            new DataCollectionId(request.DataCollectionId),
            new OrganisationId(request.LocalAuthorityId),
            cancellationToken);

        return await submission.MatchAsync(
            s => Reopen(s, request, cancellationToken),
            () => new DataCollectionSubmissionNotFoundException(request.DataCollectionId, request.LocalAuthorityId));
    }

    private async Task<Either<DataCollectionSubmissionException, DataCollectionSubmission>> Reopen(
        DataCollectionSubmission submission,
        ReopenDeliveryPlanSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.DataCollectionId);
        var dataCollection = (await dataCollectionRepository.GetByIdWithLocalAuthoritiesAsync(
                dataCollectionId, cancellationToken))
            .MatchUnsafe(dc => dc, () => (DataCollection?)null);

        if (dataCollection is null)
        {
            return new DataCollectionSubmissionUnknownException(
                new InvalidOperationException("The data collection for this submission could not be found."));
        }

        // Reopening here is the Delivery Plan approval flow only. A management information
        // quarter's self-service reopen (SubmissionsControllerService.ReopenSubmission) is the
        // correct path for MI and does not record a reason, reviewer, or per-authority due date.
        if (dataCollection.Type.Value != DataCollectionTypeConstants.DeliveryPlan)
        {
            return new DataCollectionSubmissionWrongTypeException(dataCollection.Type.Value);
        }

        if (submission.Status != DataCollectionSubmissionStatus.Submitted)
        {
            return new DataCollectionSubmissionNotAwaitingReviewException(submission.Status.ToString());
        }

        try
        {
            var reviewedById = new UserId(request.ReviewedByUserId);
            var reviewedAt = DateTime.UtcNow;

            // All three writes are staged (saveChanges: false) and committed together in one
            // SaveChangesAsync call, so a failure partway through leaves none of them committed -
            // never a rejected submission with no version snapshot, or stuck with the old deadline.
            submission.Reject(reviewedById, request.Reason, request.SectionsToReview);
            var updated = await submissionRepository.UpdateAsync(submission, cancellationToken, saveChanges: false);

            dataCollection.UpdateLocalAuthorityEndDate(new OrganisationId(request.LocalAuthorityId), request.ResubmitBy);
            await dataCollectionRepository.UpdateAsync(dataCollection, cancellationToken, saveChanges: false);

            var versionNumber = await versionRepository.GetCountByDataCollectionSubmissionIdAsync(submission.Id, cancellationToken) + 1;
            var version = DataCollectionSubmissionVersion.Create(
                submission.Id,
                versionNumber,
                request.Reason,
                request.SectionsToReview,
                request.ResubmitBy,
                reviewedById,
                reviewedAt,
                submission.SubmittedById,
                submission.SubmittedAt,
                request.Content);
            await versionRepository.AddAsync(version, cancellationToken, saveChanges: false);

            await submissionRepository.SaveChangesAsync(cancellationToken);

            return updated;
        }
        catch (Exception exception)
        {
            return new DataCollectionSubmissionUnknownException(exception);
        }
    }
}