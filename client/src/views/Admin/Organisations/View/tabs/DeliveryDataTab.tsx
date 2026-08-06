import React, { useMemo, useState } from 'react';
import { LoadingBox } from 'govuk-react';
import { GovUKDetails } from '../../../../../components/GovUKComponents';
import { ALLOCATION_STRANDS, ROLLOVER_STRAND, formatWholeMoney } from './constants';
import { generateDeliverySummary } from './mockData';
import StatCard from './StatCard';
import './styles.css';

interface DeliveryDataTabProps {
  organisationId: string;
}

const STRAND_OPTIONS = [{ key: 'all', label: 'All strands' }, ...ALLOCATION_STRANDS, ROLLOVER_STRAND];

const DeliveryDataTab = ({ organisationId }: DeliveryDataTabProps): React.ReactElement => {
  const [strand, setStrand] = useState<string>('all');

  // STUB DATA: no agreed source for these figures yet. Swap back to the real
  // endpoint when available:
  //   const { data } = useQuery(['organisation-delivery-summary', organisationId, strand],
  //     () => getOrganisationDeliverySummary(organisationId, strand));
  //   const summary = data?.data;
  const summary = useMemo(() => generateDeliverySummary(organisationId, strand), [organisationId, strand]);
  const isLoading = false;
  const totalActivities =
    summary.onTrack.activityCount + summary.offTrack.activityCount + summary.notStarted + summary.started + summary.delayed + summary.completed;

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
                <table className="govuk-table">
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
                    </tr>
                  </thead>
                  <tbody className="govuk-table__body">
                    {summary.offTrackActivities.map((activity, index) => (
                      <tr key={index} className="govuk-table__row">
                        <td className="govuk-table__cell">{activity.activityName}</td>
                        <td className="govuk-table__cell">{activity.strandDisplayName}</td>
                        <td className="govuk-table__cell govuk-table__cell--numeric">{formatWholeMoney(activity.actualSpend)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </GovUKDetails>
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
