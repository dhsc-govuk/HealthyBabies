import React, { useState } from 'react';
import { useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GovUKDetails } from '../../../../../components/GovUKComponents';
import { defaultStaleTime } from '../../../../../helpers/queriesParams';
import { getOrganisationFinancialSummary } from '../queries';
import { ALLOCATION_YEARS, ROLLOVER_YEAR, formatMoney, formatPercent, formatWholeMoney } from './constants';
import StatCard from './StatCard';
import SpendChart from './SpendChart';
import './styles.css';

interface FinancialDataTabProps {
  organisationId: string;
}

const FinancialDataTab = ({ organisationId }: FinancialDataTabProps): React.ReactElement => {
  const [year, setYear] = useState<string>(ALLOCATION_YEARS[0]);

  const { data, isLoading } = useQuery({
    queryKey: ['organisation-financial-summary', organisationId, year],
    queryFn: () => getOrganisationFinancialSummary(organisationId, year),
    staleTime: defaultStaleTime,
    keepPreviousData: true,
  });

  const summary = data?.data;
  const hasData = !!summary && (summary.totalAnnualAllocation > 0 || summary.forecastedSpend.amount > 0);

  return (
    <>
      <div className="govuk-form-group">
        <label className="govuk-label" htmlFor="financial-year">
          Filter by financial year
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
        {!hasData || !summary ? (
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

            <GovUKDetails summary="Show details with rollover spend">
              <table className="govuk-table">
                <caption className="govuk-table__caption govuk-table__caption--s">Rollover spend ({ROLLOVER_YEAR})</caption>
                <tbody className="govuk-table__body">
                  <tr className="govuk-table__row">
                    <th scope="row" className="govuk-table__header">
                      Total rollover spend
                    </th>
                    <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(summary.rollover.totalRolloverSpend)}</td>
                  </tr>
                  <tr className="govuk-table__row">
                    <th scope="row" className="govuk-table__header">
                      Rollover forecast
                    </th>
                    <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(summary.rollover.forecast)}</td>
                  </tr>
                  <tr className="govuk-table__row">
                    <th scope="row" className="govuk-table__header">
                      Rollover actual
                    </th>
                    <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(summary.rollover.actual)}</td>
                  </tr>
                  <tr className="govuk-table__row">
                    <th scope="row" className="govuk-table__header">
                      Rollover committed
                    </th>
                    <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(summary.rollover.committed)}</td>
                  </tr>
                  <tr className="govuk-table__row">
                    <th scope="row" className="govuk-table__header">
                      Rollover planned
                    </th>
                    <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(summary.rollover.planned)}</td>
                  </tr>
                  <tr className="govuk-table__row">
                    <th scope="row" className="govuk-table__header">
                      Rollover unplanned
                    </th>
                    <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(summary.rollover.unplanned)}</td>
                  </tr>
                </tbody>
              </table>
            </GovUKDetails>

            <h2 className="govuk-heading-m govuk-!-margin-top-6">Forecasted vs allocated spend per strand</h2>
            <SpendChart data={summary.forecastByStrand} />

            <GovUKDetails summary="Show data table">
              <table className="govuk-table">
                <thead className="govuk-table__head">
                  <tr className="govuk-table__row">
                    <th scope="col" className="govuk-table__header">
                      Strand
                    </th>
                    <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                      Forecasted spend
                    </th>
                    <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                      Allocated spend
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
                      <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(strand.allocation)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </GovUKDetails>
          </>
        )}
      </LoadingBox>
    </>
  );
};

export default FinancialDataTab;
