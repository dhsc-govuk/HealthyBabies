import React, { useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../layouts';
import { GovUKBreadcrumbs, GovUKDetails, GovUKTableScroll, H1, Paragraph } from '../../../components/GovUKComponents';
import StatCard from '../../../components/Global/DeliveryPlanAnalytics/StatCard';
import SpendChart from '../../../components/Global/DeliveryPlanAnalytics/SpendChart';
import { ALLOCATION_YEARS, formatMoney, formatPercent, formatWholeMoney } from '../../../components/Global/DeliveryPlanAnalytics/constants';
import '../../../components/Global/DeliveryPlanAnalytics/styles.css';
import { defaultStaleTime } from '../../../helpers/queriesParams';
import { ROUTES } from '../../../constants/routes';
import usePageTitle from '../../../hooks/usePageTitle';
import { getDeliveryPlanDeliverySummary, getDeliveryPlanFinancialSummary } from './queries';

const Summary = (): React.ReactElement => {
  usePageTitle('Summary');

  const routerLocation = useLocation();
  const area = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';
  const homeUrl = area === 'organisation-admin' ? ROUTES.organisationAdmin.home : ROUTES.deliveryPlan.home;

  const [year, setYear] = useState<string>(ALLOCATION_YEARS[0]);

  const { data: financialData, isLoading: isFinancialLoading } = useQuery({
    queryKey: ['delivery-plan-financial-summary', area, year],
    queryFn: () => getDeliveryPlanFinancialSummary(area, year),
    staleTime: defaultStaleTime,
    keepPreviousData: true,
  });

  const { data: deliveryData, isLoading: isDeliveryLoading } = useQuery({
    queryKey: ['delivery-plan-delivery-summary', area],
    queryFn: () => getDeliveryPlanDeliverySummary(area),
    staleTime: defaultStaleTime,
    keepPreviousData: true,
  });

  const summary = financialData?.data;
  const deliverySummary = deliveryData?.data;
  const hasFinancialData = !!summary && (summary.totalAnnualAllocation > 0 || summary.forecastedSpend.amount > 0);
  const isLoading = isFinancialLoading || isDeliveryLoading;

  return (
    <GeneralLayout>
      <GovUKBreadcrumbs items={[{ label: 'Home', href: homeUrl }, { label: 'Summary' }]} />

      <H1>Delivery plan summary</H1>
      <Paragraph>View key financial information for Best Start Family Hub and Healthy Baby programme in your local authority</Paragraph>

      <div className="govuk-form-group">
        <label className="govuk-label" htmlFor="financial-year">
          Financial year
        </label>
        <select className="govuk-select" id="financial-year" value={year} onChange={(e) => setYear(e.target.value)}>
          {ALLOCATION_YEARS.map((y) => (
            <option key={y} value={y}>
              {y}
            </option>
          ))}
        </select>
      </div>

      <LoadingBox loading={isLoading}>
        {!hasFinancialData || !summary ? (
          <div className="govuk-inset-text">No financial data has been submitted for this local authority for {year}.</div>
        ) : (
          <>
            <h2 className="govuk-heading-m">Spend against allocation</h2>

            <div className="la-stat-grid la-stat-grid--full">
              <StatCard value={formatWholeMoney(summary.totalGrantAllocation)} label="Total grant allocation" sublabel="2026-29" />
            </div>

            <div className="la-stat-grid">
              <StatCard value={formatWholeMoney(summary.totalAnnualAllocation)} label="Total annual allocation" sublabel={year} />
              <StatCard
                value={formatWholeMoney(summary.forecastedSpend.amount)}
                percent={formatPercent(summary.forecastedSpend.percentage, 'total grant allocation')}
                label="Forecasted spend"
                sublabel={year}
              />
              <StatCard
                value={formatWholeMoney(summary.committedSpend.amount)}
                percent={formatPercent(summary.committedSpend.percentage)}
                label="Committed spend"
                sublabel={year}
              />
              <StatCard
                value={formatWholeMoney(summary.actualSpend.amount)}
                percent={formatPercent(summary.actualSpend.percentage)}
                label="Actual spend"
                sublabel={year}
              />
            </div>

            <div className="la-stat-grid la-stat-grid--full">
              <StatCard
                value={formatWholeMoney(summary.actualAndCommittedSpend.amount)}
                percent={formatPercent(summary.actualAndCommittedSpend.percentage)}
                label="Actual and committed spend"
                sublabel={year}
                variant="highlight"
              />
            </div>

            <div className="la-stat-grid">
              <StatCard
                value={formatWholeMoney(summary.unplannedSpend.amount)}
                percent={formatPercent(summary.unplannedSpend.percentage)}
                label="Unplanned spend"
                sublabel={year}
              />
              <StatCard
                value={formatWholeMoney(summary.plannedSpend.amount)}
                percent={formatPercent(summary.plannedSpend.percentage)}
                label="Planned spend"
                sublabel={year}
              />
            </div>

            {deliverySummary && (
              <>
                <h2 className="govuk-heading-m govuk-!-margin-top-6">Activities by status</h2>
                <div className="la-stat-grid la-stat-grid--four">
                  <StatCard value={String(deliverySummary.notStarted)} label="Not started" sublabel="Nr of activities" />
                  <StatCard value={String(deliverySummary.started)} label="Started" sublabel="Nr of activities" />
                  <StatCard value={String(deliverySummary.delayed)} label="Delayed" sublabel="Nr of activities" />
                  <StatCard value={String(deliverySummary.completed)} label="Completed" sublabel="Nr of activities" />
                </div>
              </>
            )}

            <h2 className="govuk-heading-m govuk-!-margin-top-6">Forecast per strand</h2>
            <SpendChart data={summary.forecastByStrand} />

            <GovUKDetails summary="Show data table">
              <GovUKTableScroll label="Forecast per strand table">
                <table className="govuk-table">
                  <caption className="govuk-visually-hidden">Forecasted and allocated spend per strand</caption>
                  <thead className="govuk-table__head">
                    <tr className="govuk-table__row">
                      <th scope="col" className="govuk-table__header">
                        Strand
                      </th>
                      <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                        Forecasted spend, {year} (£)
                      </th>
                    </tr>
                  </thead>
                  <tbody className="govuk-table__body">
                    {summary.forecastByStrand.map((strand) => (
                      <tr key={strand.module} className="govuk-table__row">
                        <th scope="row" className="govuk-table__header">
                          {strand.displayName}
                        </th>
                        <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(strand.forecastedSpend)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </GovUKTableScroll>
            </GovUKDetails>
          </>
        )}
      </LoadingBox>
    </GeneralLayout>
  );
};

export default Summary;
