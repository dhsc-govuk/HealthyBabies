import React from 'react';
import { useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import StatCard from '../../../../../components/Global/DeliveryPlanAnalytics/StatCard';
import { formatWholeMoney } from '../../../../../components/Global/DeliveryPlanAnalytics/constants';
import { defaultStaleTime } from '../../../../../helpers/queriesParams';
import { getDeliveryOverviewActivityStatus } from '../queries';
import { DeliveryOverviewStatusGroupDto } from '../types';

interface ActivityStatusTabProps {
  yearRange: string;
  strand: string;
}

const ActivityStatusTab = ({ yearRange, strand }: ActivityStatusTabProps): React.ReactElement => {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['delivery-overview-activity-status', yearRange, strand],
    queryFn: () => getDeliveryOverviewActivityStatus(yearRange, strand),
    staleTime: defaultStaleTime,
    keepPreviousData: true,
  });

  const status = data?.data;
  const hasData = !!status && status.notStarted.activityCount + status.started.activityCount + status.delayed.activityCount + status.completed.activityCount > 0;

  const groups: { key: string; label: string; group: DeliveryOverviewStatusGroupDto }[] = status
    ? [
        { key: 'not-started', label: 'Not started', group: status.notStarted },
        { key: 'started', label: 'Started', group: status.started },
        { key: 'delayed', label: 'Delayed', group: status.delayed },
        { key: 'completed', label: 'Completed', group: status.completed },
      ]
    : [];

  return (
    <LoadingBox loading={isLoading}>
      {isError ? (
        <div className="govuk-inset-text">There was a problem loading this data. Try again later.</div>
      ) : !hasData ? (
        <div className="govuk-inset-text">No delivery data has been submitted for this selection.</div>
      ) : (
        <>
          <div className="la-stat-grid la-stat-grid--four">
            {groups.map(({ key, label, group }) => (
              <StatCard key={key} value={String(group.activityCount)} percent={`${group.activityPercentage.toFixed(1)}%`} label={label} sublabel={`Nr of activities, ${yearRange}`} />
            ))}
          </div>
          <div className="la-stat-grid la-stat-grid--four govuk-!-margin-top-4">
            {groups.map(({ key, label, group }) => (
              <StatCard key={key} value={formatWholeMoney(group.totalActualSpend)} percent={`${group.spendPercentage.toFixed(1)}%`} label={label} sublabel={`Total actual spend, ${yearRange}`} />
            ))}
          </div>
        </>
      )}
    </LoadingBox>
  );
};

export default ActivityStatusTab;
