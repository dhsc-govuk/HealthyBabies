import React from 'react';
import { useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import StatCard from '../../../../../components/Global/DeliveryPlanAnalytics/StatCard';
import { formatWholeMoney } from '../../../../../components/Global/DeliveryPlanAnalytics/constants';
import { defaultStaleTime } from '../../../../../helpers/queriesParams';
import { getDeliveryOverviewDeliveryStatus } from '../queries';

interface DeliveryStatusTabProps {
  yearRange: string;
  strand: string;
}

const DeliveryStatusTab = ({ yearRange, strand }: DeliveryStatusTabProps): React.ReactElement => {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['delivery-overview-delivery-status', yearRange, strand],
    queryFn: () => getDeliveryOverviewDeliveryStatus(yearRange, strand),
    staleTime: defaultStaleTime,
    keepPreviousData: true,
  });

  const status = data?.data;
  const hasData = !!status && status.onTrack.activityCount + status.offTrack.activityCount > 0;

  return (
    <LoadingBox loading={isLoading}>
      {isError ? (
        <div className="govuk-inset-text">There was a problem loading this data. Try again later.</div>
      ) : !hasData ? (
        <div className="govuk-inset-text">No delivery data has been submitted for this selection.</div>
      ) : (
        <div className="la-stat-grid">
          <StatCard value={String(status.onTrack.activityCount)} percent={`${status.onTrack.activityPercentage.toFixed(1)}%`} label="On track" sublabel={`Nr of activities, ${yearRange}`} variant="green" />
          <StatCard value={String(status.offTrack.activityCount)} percent={`${status.offTrack.activityPercentage.toFixed(1)}%`} label="Off track" sublabel={`Nr of activities, ${yearRange}`} variant="yellow" />
          <StatCard value={formatWholeMoney(status.onTrack.totalActualSpend)} percent={`${status.onTrack.spendPercentage.toFixed(1)}%`} label="On track" sublabel={`Total actual spend, ${yearRange}`} variant="green" />
          <StatCard value={formatWholeMoney(status.offTrack.totalActualSpend)} percent={`${status.offTrack.spendPercentage.toFixed(1)}%`} label="Off track" sublabel={`Total actual spend, ${yearRange}`} variant="yellow" />
        </div>
      )}
    </LoadingBox>
  );
};

export default DeliveryStatusTab;
