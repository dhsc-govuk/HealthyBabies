import React from 'react';
import { useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GovUKDetails, GovUKTableScroll } from '../../../../../components/GovUKComponents';
import StatCard from '../../../../../components/Global/DeliveryPlanAnalytics/StatCard';
import { defaultStaleTime } from '../../../../../helpers/queriesParams';
import { getDeliveryOverviewSummary } from '../queries';
import UnderOverspendChart from '../UnderOverspendChart';

interface SummaryTabProps {
  yearRange: string;
  strand: string;
}

const formatSigned = (value: number): string => `${value > 0 ? '+' : ''}${value.toFixed(2)}%`;

const SummaryTab = ({ yearRange, strand }: SummaryTabProps): React.ReactElement => {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['delivery-overview-summary', yearRange, strand],
    queryFn: () => getDeliveryOverviewSummary(yearRange, strand),
    staleTime: defaultStaleTime,
    keepPreviousData: true,
  });

  const summary = data?.data;
  const hasData = !!summary && (summary.localAuthorities.length > 0 || summary.varianceByStrand.length > 0);

  const offTrackLocalAuthorities = summary?.localAuthorities.filter((la) => la.isBudgetOffTrack).sort((a, b) => b.offTrackActivityPercentage - a.offTrackActivityPercentage) ?? [];
  // isUnderspend/isOverspend come from the API (computed from the same threshold as the stat-card
  // counts above), rather than re-deriving a threshold here, so the two can never disagree.
  const spendVarianceLocalAuthorities = summary?.localAuthorities.filter((la) => la.isUnderspend || la.isOverspend).sort((a, b) => Math.abs(b.variancePercentage) - Math.abs(a.variancePercentage)) ?? [];

  return (
    <LoadingBox loading={isLoading}>
      {isError ? (
        <div className="govuk-inset-text">There was a problem loading this data. Try again later.</div>
      ) : !hasData ? (
        <div className="govuk-inset-text">No delivery data has been submitted for this selection.</div>
      ) : (
        <>
          <h2 className="govuk-heading-m">Local authorities with off-track budget</h2>
          <div className="la-stat-grid la-stat-grid--full">
            <StatCard value={String(summary.offTrackBudgetLaCount)} label="Over 20% off-track budget" sublabel="Nr of local authorities" />
          </div>

          {offTrackLocalAuthorities.length > 0 && (
            <GovUKDetails summary="Show local authorities">
              <GovUKTableScroll label="Local authorities with off-track budget table">
                <table className="govuk-table">
                  <caption className="govuk-visually-hidden">Local authorities with off-track budget</caption>
                  <thead className="govuk-table__head">
                    <tr className="govuk-table__row">
                      <th scope="col" className="govuk-table__header">
                        Local authority
                      </th>
                      <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                        Off-track activities
                      </th>
                      <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                        % of all LA's activities
                      </th>
                    </tr>
                  </thead>
                  <tbody className="govuk-table__body">
                    {offTrackLocalAuthorities.map((la) => (
                      <tr key={la.organisationId} className="govuk-table__row">
                        <td className="govuk-table__cell">{la.organisationName}</td>
                        <td className="govuk-table__cell govuk-table__cell--numeric">{la.offTrackActivityCount}</td>
                        <td className="govuk-table__cell govuk-table__cell--numeric">{la.offTrackActivityPercentage.toFixed(2)}%</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </GovUKTableScroll>
            </GovUKDetails>
          )}

          <h2 className="govuk-heading-m govuk-!-margin-top-6">Local authorities with under- or overspend</h2>
          <div className="la-stat-grid">
            <StatCard value={String(summary.underspendLaCount)} label="Over 10% underspend" sublabel="Nr of local authorities" />
            <StatCard value={String(summary.overspendLaCount)} label="Over 10% overspend" sublabel="Nr of local authorities" />
          </div>

          {spendVarianceLocalAuthorities.length > 0 && (
            <GovUKDetails summary="Show local authorities">
              <GovUKTableScroll label="Local authorities with under- or overspend table">
                <table className="govuk-table">
                  <caption className="govuk-visually-hidden">Local authorities with under- or overspend</caption>
                  <thead className="govuk-table__head">
                    <tr className="govuk-table__row">
                      <th scope="col" className="govuk-table__header">
                        Local authority
                      </th>
                      <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                        Under-/overspend
                      </th>
                    </tr>
                  </thead>
                  <tbody className="govuk-table__body">
                    {spendVarianceLocalAuthorities.map((la) => (
                      <tr key={la.organisationId} className="govuk-table__row">
                        <td className="govuk-table__cell">{la.organisationName}</td>
                        <td className="govuk-table__cell govuk-table__cell--numeric">{formatSigned(la.variancePercentage)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </GovUKTableScroll>
            </GovUKDetails>
          )}

          <h2 className="govuk-heading-m govuk-!-margin-top-6">Under- and overspend by strand, %</h2>
          <UnderOverspendChart data={summary.varianceByStrand} />

          <GovUKDetails summary="Show data table">
            <GovUKTableScroll label="Under- and overspend by strand table">
              <table className="govuk-table">
                <caption className="govuk-visually-hidden">Under- and overspend by strand</caption>
                <thead className="govuk-table__head">
                  <tr className="govuk-table__row">
                    <th scope="col" className="govuk-table__header">
                      Strand
                    </th>
                    <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                      Total forecast
                    </th>
                    <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                      Total actual
                    </th>
                    <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                      Under-/overspend
                    </th>
                  </tr>
                </thead>
                <tbody className="govuk-table__body">
                  {summary.varianceByStrand.map((strandVariance) => (
                    <tr key={strandVariance.module} className="govuk-table__row">
                      <td className="govuk-table__cell">{strandVariance.displayName}</td>
                      <td className="govuk-table__cell govuk-table__cell--numeric">£{strandVariance.totalForecast.toLocaleString('en-GB')}</td>
                      <td className="govuk-table__cell govuk-table__cell--numeric">£{strandVariance.totalActual.toLocaleString('en-GB')}</td>
                      <td className="govuk-table__cell govuk-table__cell--numeric">{formatSigned(strandVariance.variancePercentage)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </GovUKTableScroll>
          </GovUKDetails>
        </>
      )}
    </LoadingBox>
  );
};

export default SummaryTab;
