import React, { useState } from 'react';
import { GeneralLayout } from '../../../../layouts';
import { GovUKTabs } from '../../../../components/GovUKComponents';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';
import { ALLOCATION_STRANDS, ALLOCATION_YEARS } from '../../../../components/Global/DeliveryPlanAnalytics/constants';
import '../../../../components/Global/DeliveryPlanAnalytics/styles.css';
import SummaryTab from './tabs/SummaryTab';
import DeliveryStatusTab from './tabs/DeliveryStatusTab';
import ActivityStatusTab from './tabs/ActivityStatusTab';

const STRAND_OPTIONS = [{ key: 'all', label: 'All strands' }, ...ALLOCATION_STRANDS];

const DeliveryOverview = (): React.ReactElement => {
  usePageTitle('Delivery overview');
  const [strand, setStrand] = useState<string>('all');
  const [yearRange, setYearRange] = useState<string>(ALLOCATION_YEARS[0]);

  return (
    <GeneralLayout breadcrumbs={[{ label: 'Home', link: ROUTES.admin.home }, { label: 'Trends', link: ROUTES.admin.trends }]}>
      <h1 className="govuk-heading-l">Delivery overview</h1>
      <p className="govuk-body">View key delivery information for Best Start Family Hub and Healthy Baby programme across local authorities</p>

      <hr className="govuk-section-break govuk-section-break--visible govuk-!-margin-bottom-6" />

      <div className="govuk-grid-row">
        <div className="govuk-grid-column-one-half">
          <div className="govuk-form-group">
            <label className="govuk-label" htmlFor="delivery-overview-strand">
              Filter by strand
            </label>
            <select className="govuk-select" id="delivery-overview-strand" value={strand} onChange={(e) => setStrand(e.target.value)}>
              {STRAND_OPTIONS.map((option) => (
                <option key={option.key} value={option.key}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div className="govuk-grid-column-one-half">
          <div className="govuk-form-group">
            <label className="govuk-label" htmlFor="delivery-overview-year">
              Financial year
            </label>
            <select className="govuk-select" id="delivery-overview-year" value={yearRange} onChange={(e) => setYearRange(e.target.value)}>
              {ALLOCATION_YEARS.map((year) => (
                <option key={year} value={year}>
                  {year}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      <GovUKTabs
        title="Delivery overview"
        tabs={[
          { id: 'summary', label: 'Summary', panel: <SummaryTab yearRange={yearRange} strand={strand} /> },
          { id: 'delivery-status', label: 'Delivery status', panel: <DeliveryStatusTab yearRange={yearRange} strand={strand} /> },
          { id: 'activity-status', label: 'Activity status', panel: <ActivityStatusTab yearRange={yearRange} strand={strand} /> },
        ]}
      />
    </GeneralLayout>
  );
};

export default DeliveryOverview;
