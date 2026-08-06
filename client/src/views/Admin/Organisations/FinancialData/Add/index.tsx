import React, { useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { LoadingBox } from 'govuk-react';
import { useQuery } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { defaultStaleTime } from '../../../../../helpers/queriesParams';
import { getOrganisationHome } from '../../View/queries';
import { getGlobalDataByEntity } from '../../../../../components/Global/Queries/globalData';
import usePageTitle from '../../../../../hooks/usePageTitle';
import './styles.css';
import {
  FINANCIAL_DATA_ALLOCATION_MODULE_ENTITY,
  FINANCIAL_DATA_ALLOCATION_YEAR_ENTITY,
  FINANCIAL_DATA_ROLLOVER_MODULE_ENTITY,
  FINANCIAL_DATA_ROLLOVER_YEAR_ENTITY,
  FinancialModule,
  FormValues,
  formatCurrency,
  parseAmount,
} from '../constants';

type UrlParams = { organisationId: string };

interface IncomingState {
  values?: FormValues;
  isUpdate?: boolean;
}

const AddFinancialData = (): React.ReactElement => {
  usePageTitle('Add financial data');
  const { organisationId } = useParams<UrlParams>();
  const navigate = useNavigate();
  const location = useLocation();
  const incomingState = location.state as IncomingState | null;

  const { data: orgData, isLoading: orgLoading } = useQuery({
    queryKey: [`organisation-home-${organisationId}`],
    queryFn: () => getOrganisationHome(organisationId!),
    staleTime: defaultStaleTime,
  });

  const { data: allocationModulesData, isLoading: modulesLoading } = useQuery({
    queryKey: ['global-data', FINANCIAL_DATA_ALLOCATION_MODULE_ENTITY],
    queryFn: () => getGlobalDataByEntity(FINANCIAL_DATA_ALLOCATION_MODULE_ENTITY),
    staleTime: 0,
  });

  const { data: allocationYearsData, isLoading: yearsLoading } = useQuery({
    queryKey: ['global-data', FINANCIAL_DATA_ALLOCATION_YEAR_ENTITY],
    queryFn: () => getGlobalDataByEntity(FINANCIAL_DATA_ALLOCATION_YEAR_ENTITY),
    staleTime: 0,
  });

  const { data: rolloverModulesData, isLoading: rolloverModulesLoading } = useQuery({
    queryKey: ['global-data', FINANCIAL_DATA_ROLLOVER_MODULE_ENTITY],
    queryFn: () => getGlobalDataByEntity(FINANCIAL_DATA_ROLLOVER_MODULE_ENTITY),
    staleTime: 0,
  });

  const { data: rolloverYearsData, isLoading: rolloverYearsLoading } = useQuery({
    queryKey: ['global-data', FINANCIAL_DATA_ROLLOVER_YEAR_ENTITY],
    queryFn: () => getGlobalDataByEntity(FINANCIAL_DATA_ROLLOVER_YEAR_ENTITY),
    staleTime: 0,
  });

  const isLoading = orgLoading || modulesLoading || yearsLoading || rolloverModulesLoading || rolloverYearsLoading;

  const allocationModules: FinancialModule[] = (allocationModulesData?.data ?? []).map((d) => ({
    key: d.value,
    label: d.description ?? d.value,
  }));

  const allocationYears: string[] = (allocationYearsData?.data ?? []).map((d) => d.value);

  const rolloverModuleItem = rolloverModulesData?.data?.[0];
  const rolloverModuleKey = rolloverModuleItem?.value ?? '';
  const rolloverModuleLabel = rolloverModuleItem?.description ?? rolloverModuleItem?.value ?? '';
  const rolloverYear = rolloverYearsData?.data?.[0]?.value ?? '';

  const isUpdate = incomingState?.isUpdate ?? false;
  const [values, setValues] = useState<FormValues>(incomingState?.values ?? {});

  const handleChange = (module: string, yearRange: string, value: string) => {
    setValues((prev) => ({
      ...prev,
      [module]: { ...prev[module], [yearRange]: value },
    }));
  };

  const totalFor = (yearRange: string): number =>
    allocationModules.reduce((sum, mod) => sum + parseAmount(values[mod.key]?.[yearRange] ?? ''), 0);

  const rolloverAmount = parseAmount(values[rolloverModuleKey]?.[rolloverYear] ?? '');

  const handleContinue = (e: React.FormEvent) => {
    e.preventDefault();
    navigate(`/admin/organisations/${organisationId}/financial-data/check`, {
      state: { values, allocationModules, allocationYears, rolloverModuleKey, rolloverModuleLabel, rolloverYear, isUpdate },
    });
  };

  const orgName = orgData?.data?.name ?? '';

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
        { label: 'Local authorities', link: '/admin/organisations' },
        { label: orgName, link: `/admin/organisations/${organisationId}` },
      ]}>
      <LoadingBox loading={isLoading}>
        <a href={`/admin/organisations/${organisationId}`} className="govuk-back-link" onClick={(e) => { e.preventDefault(); navigate(`/admin/organisations/${organisationId}`); }}>
          Back
        </a>

        <span className="govuk-caption-l">{orgName}</span>
        <h1 className="govuk-heading-l">Add financial data</h1>

        <form onSubmit={handleContinue}>
          {allocationModules.map((mod) => (
            <div key={mod.key} className="financial-data-section">
              <h2 className="govuk-heading-m">{mod.label}</h2>
              {allocationYears.map((year) => (
                <div key={year} className="financial-data-row">
                  <label className="govuk-label" htmlFor={`${mod.key}-${year}`}>— {year}</label>
                  <div className="govuk-input__wrapper">
                    <div className="govuk-input__prefix" aria-hidden="true">£</div>
                    <input
                      className="govuk-input govuk-input--width-10"
                      id={`${mod.key}-${year}`}
                      name={`${mod.key}-${year}`}
                      type="text"
                      inputMode="decimal"
                      value={values[mod.key]?.[year] ?? ''}
                      onChange={(e) => handleChange(mod.key, year, e.target.value)}
                    />
                  </div>
                </div>
              ))}
            </div>
          ))}

          {rolloverModuleKey && rolloverYear && (
            <div className="govuk-form-group">
              <label className="govuk-label govuk-label--m" htmlFor={`${rolloverModuleKey}-${rolloverYear}`}>
                {rolloverModuleLabel} {rolloverYear}
              </label>
              <div className="govuk-input__wrapper">
                <div className="govuk-input__prefix" aria-hidden="true">£</div>
                <input
                  className="govuk-input govuk-input--width-10"
                  id={`${rolloverModuleKey}-${rolloverYear}`}
                  name={`${rolloverModuleKey}-${rolloverYear}`}
                  type="text"
                  inputMode="decimal"
                  value={values[rolloverModuleKey]?.[rolloverYear] ?? ''}
                  onChange={(e) => handleChange(rolloverModuleKey, rolloverYear, e.target.value)}
                />
              </div>
            </div>
          )}

          <div className="govuk-inset-text">
            <h2 className="govuk-heading-m">Summary</h2>
            <div>
              {rolloverModuleKey && rolloverYear && (
                <div className="financial-summary-row">
                  <span>Total approved rollover spend {rolloverYear}</span>
                  <span>{formatCurrency(rolloverAmount)}</span>
                </div>
              )}
              {allocationYears.map((year) => (
                <div key={year} className="financial-summary-row">
                  <span>Total allocation {year}</span>
                  <span>{formatCurrency(totalFor(year))}</span>
                </div>
              ))}
            </div>
          </div>

          <button type="submit" className="govuk-button">Continue</button>
        </form>
      </LoadingBox>
    </SettingsLayout>
  );
};

export default AddFinancialData;