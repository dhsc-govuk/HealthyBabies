import React, { useState } from 'react';
import { useQuery } from 'react-query';
import { Link } from 'react-router-dom';
import { LoadingBox } from 'govuk-react';
import { GovUKDetails, GovUKTableScroll } from '../../../../../components/GovUKComponents';
import { defaultStaleTime } from '../../../../../helpers/queriesParams';
import { getOrganisationDeliverySummary } from '../queries';
import { ALLOCATION_STRANDS, ROLLOVER_STRAND, formatWholeMoney } from '../../../../../components/Global/DeliveryPlanAnalytics/constants';
import type { OffTrackActivityDto } from '../../../../../components/Global/DeliveryPlanAnalytics/types';
import StatCard from '../../../../../components/Global/DeliveryPlanAnalytics/StatCard';
import { ROUTES } from '../../../../../constants/routes';
import '../../../../../components/Global/DeliveryPlanAnalytics/styles.css';

interface DeliveryDataTabProps {
  organisationId: string;
}

const STRAND_OPTIONS = [{ key: 'all', label: 'All strands' }, ...ALLOCATION_STRANDS, ROLLOVER_STRAND];

const DeliveryDataTab = ({ organisationId }: DeliveryDataTabProps): React.ReactElement => {
  const [strand, setStrand] = useState<string>('all');

  const { data, isLoading } = useQuery({
    queryKey: ['organisation-delivery-summary', organisationId, strand],
    queryFn: () => getOrganisationDeliverySummary(organisationId, strand),
    staleTime: defaultStaleTime,
    keepPreviousData: true,
  });

  const summary = data?.data;
  const totalActivities = summary
    ? summary.onTrack.activityCount + summary.offTrack.activityCount + summary.notStarted + summary.started + summary.delayed + summary.completed
    : 0;

  // The review route's ":submissionId" is the data collection id and ":localAuthorityId" is the
  // organisation id. A row without an activity id comes from a module that holds no activities list,
  // so it is addressed by section instead.
  const activityHref = (activity: OffTrackActivityDto): string =>
    activity.activityId
      ? ROUTES.admin.submissions.reviewActivity(activity.dataCollectionId, organisationId, activity.formModuleId, activity.activityId)
      : ROUTES.admin.submissions.reviewSection(activity.dataCollectionId, organisationId, activity.formModuleId);

  return (
    <>
      <div className="govuk-form-group">
        <label className="govuk-label" htmlFor="delivery-strand">
          Filter by strand
        </label>
        <select className="govuk-select" id="delivery-strand" value={strand} onChange={(e) => setStrand(e.target.value)}>
          {STRAND_OPTIONS.map((option) => (
            <option key={option.key} value={option.key}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <LoadingBox loading={isLoading}>
        {!summary || totalActivities === 0 ? (
          <div className="govuk-inset-text">No delivery data has been submitted for this selection.</div>
        ) : (
          <>
            <h2 className="govuk-heading-m">Activity status</h2>
            <div className="la-stat-grid">
              <StatCard value={String(summary.onTrack.activityCount)} label="On track" sublabel="Nr of activities" variant="green" />
              <StatCard value={String(summary.offTrack.activityCount)} label="Off track" sublabel="Nr of activities" variant="yellow" />
              <StatCard value={formatWholeMoney(summary.onTrack.totalActualSpend)} label="On track" sublabel="Total actual spend" variant="green" />
              <StatCard value={formatWholeMoney(summary.offTrack.totalActualSpend)} label="Off track" sublabel="Total actual spend" variant="yellow" />
            </div>

            {summary.offTrackActivities.length > 0 && (
              <GovUKDetails summary="Show off-track activities">
                <GovUKTableScroll label="Off-track activities table">
                <table className="govuk-table">
                  <caption className="govuk-visually-hidden">Off-track activities</caption>
                  <thead className="govuk-table__head">
                    <tr className="govuk-table__row">
                      <th scope="col" className="govuk-table__header">
                        Activity
                      </th>
                      <th scope="col" className="govuk-table__header">
                        Strand
                      </th>
                      <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                        Actual spend
                      </th>
                      <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                        <span className="govuk-visually-hidden">Actions</span>
                      </th>
                    </tr>
                  </thead>
                  <tbody className="govuk-table__body">
                    {summary.offTrackActivities.map((activity, index) => (
                      <tr key={activity.activityId ?? `${activity.module}-${index}`} className="govuk-table__row">
                        <td className="govuk-table__cell">{activity.activityName}</td>
                        <td className="govuk-table__cell">{activity.strandDisplayName}</td>
                        <td className="govuk-table__cell govuk-table__cell--numeric">{formatWholeMoney(activity.actualSpend)}</td>
                        <td className="govuk-table__cell govuk-table__cell--numeric">
                          <Link className="govuk-link govuk-link--no-visited-state" to={activityHref(activity)}>
                            View<span className="govuk-visually-hidden"> {activity.activityName}</span>
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                </GovUKTableScroll>
              </GovUKDetails>
            )}

            {summary.activitiesLink && (
              <p className="govuk-body govuk-!-text-align-right">
                <Link
                  className="govuk-link govuk-link--no-visited-state"
                  to={
                    summary.activitiesLink.formModuleId
                      ? ROUTES.admin.submissions.reviewSection(summary.activitiesLink.dataCollectionId, organisationId, summary.activitiesLink.formModuleId)
                      : ROUTES.admin.submissions.localAuthority(summary.activitiesLink.dataCollectionId, organisationId)
                  }>
                  View activities
                </Link>
              </p>
            )}

            <h2 className="govuk-heading-m govuk-!-margin-top-6">Delivery status</h2>
            <div className="la-stat-grid la-stat-grid--four">
              <StatCard value={String(summary.notStarted)} label="Not started" sublabel="Nr of activities" />
              <StatCard value={String(summary.started)} label="Started" sublabel="Nr of activities" />
              <StatCard value={String(summary.delayed)} label="Delayed" sublabel="Nr of activities" />
              <StatCard value={String(summary.completed)} label="Completed" sublabel="Nr of activities" />
            </div>
          </>
        )}
      </LoadingBox>
    </>
  );
};

export default DeliveryDataTab;
