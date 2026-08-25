import React from 'react';
import { Link } from 'govuk-react';
import { GovUKTableScroll } from '../../../../../components/GovUKComponents';
import { FinancialDataEntryDto } from '../queries';
import { ALLOCATION_STRANDS, ALLOCATION_YEARS, ROLLOVER_STRAND, ROLLOVER_YEAR, Strand, formatMoney } from '../../../../../components/Global/DeliveryPlanAnalytics/constants';

interface AllocationsTabProps {
  entries: FinancialDataEntryDto[];
  onChange: () => void;
}

const AllocationsTab = ({ entries, onChange }: AllocationsTabProps): React.ReactElement => {
  const getAmount = (module: string, yearRange: string): number =>
    entries.find((e) => e.module === module && e.yearRange === yearRange)?.amount ?? 0;

  const totalFor = (yearRange: string): number =>
    ALLOCATION_STRANDS.reduce((sum, strand) => sum + getAmount(strand.key, yearRange), 0);

  if (entries.length === 0) {
    return (
      <>
        <div className="govuk-button-group govuk-!-text-align-right">
          <button type="button" className="govuk-button govuk-button--secondary" onClick={onChange}>
            Add allocations
          </button>
        </div>
        <div className="govuk-inset-text">
          You have not added any grant allocations for this local authority. Once you have set them up, they will appear here.
        </div>
      </>
    );
  }

  const renderStrandTable = (strand: Strand, years: string[], amountHeader: string) => (
    <React.Fragment key={strand.key}>
      <h2 className="govuk-heading-m">{strand.label}</h2>
      <GovUKTableScroll label={`${strand.label} allocations table`}>
      <table className="govuk-table">
        <caption className="govuk-visually-hidden">{strand.label} allocations</caption>
        <thead className="govuk-table__head">
          <tr className="govuk-table__row">
            <th scope="col" className="govuk-table__header">
              Period
            </th>
            <th scope="col" className="govuk-table__header govuk-table__header--numeric">
              {amountHeader}
            </th>
          </tr>
        </thead>
        <tbody className="govuk-table__body">
          {years.map((year) => (
            <tr key={year} className="govuk-table__row">
              <th scope="row" className="govuk-table__header">
                {year}
              </th>
              <td className="govuk-table__cell govuk-table__cell--numeric">{formatMoney(getAmount(strand.key, year))}</td>
            </tr>
          ))}
        </tbody>
      </table>
      </GovUKTableScroll>
    </React.Fragment>
  );

  return (
    <>
      <p className="govuk-body govuk-!-text-align-right">
        <Link
          href="#"
          onClick={(e: React.MouseEvent) => {
            e.preventDefault();
            onChange();
          }}>
          Change<span className="govuk-visually-hidden"> financial allocations</span>
        </Link>
      </p>

      {ALLOCATION_STRANDS.map((strand) => renderStrandTable(strand, ALLOCATION_YEARS, 'Allocation'))}
      {renderStrandTable(ROLLOVER_STRAND, [ROLLOVER_YEAR], 'Rollover')}

      <div className="govuk-inset-text">
        <h3 className="govuk-heading-s">Summary</h3>
        <dl className="govuk-summary-list">
          <div className="govuk-summary-list__row">
            <dt className="govuk-summary-list__key">Total approved rollover spend {ROLLOVER_YEAR}</dt>
            <dd className="govuk-summary-list__value govuk-!-text-align-right">{formatMoney(getAmount(ROLLOVER_STRAND.key, ROLLOVER_YEAR))}</dd>
          </div>
          {ALLOCATION_YEARS.map((year) => (
            <div key={year} className="govuk-summary-list__row">
              <dt className="govuk-summary-list__key">Total allocation {year}</dt>
              <dd className="govuk-summary-list__value govuk-!-text-align-right">{formatMoney(totalFor(year))}</dd>
            </div>
          ))}
        </dl>
      </div>
    </>
  );
};

export default AllocationsTab;
