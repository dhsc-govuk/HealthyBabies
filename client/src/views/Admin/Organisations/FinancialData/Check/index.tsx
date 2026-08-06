import React from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { defaultStaleTime, viewOrganisationHomeCacheKey } from '../../../../../helpers/queriesParams';
import { getOrganisationHome, upsertOrganisationFinancialData, FinancialDataEntryDto } from '../../View/queries';
import { FinancialModule, FormValues, formatCurrency, parseAmount } from '../constants';
import { GovUKSummaryTable } from '../../../../../components/GovUKComponents';

type UrlParams = { organisationId: string };

interface LocationState {
  values: FormValues;
  allocationModules: FinancialModule[];
  allocationYears: string[];
  rolloverModuleKey: string;
  rolloverModuleLabel: string;
  rolloverYear: string;
  isUpdate?: boolean;
}

const CheckFinancialData = (): React.ReactElement => {
  usePageTitle('Check your answers');
  const { organisationId } = useParams<UrlParams>();
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const state = location.state as LocationState | null;

  const values = state?.values ?? {};
  const allocationModules = state?.allocationModules ?? [];
  const allocationYears = state?.allocationYears ?? [];
  const rolloverModuleKey = state?.rolloverModuleKey ?? '';
  const rolloverModuleLabel = state?.rolloverModuleLabel ?? '';
  const rolloverYear = state?.rolloverYear ?? '';
  const isUpdate = state?.isUpdate ?? false;

  const { data: orgData } = useQuery({
    queryKey: [`organisation-home-${organisationId}`],
    queryFn: () => getOrganisationHome(organisationId!),
    staleTime: defaultStaleTime,
  });

  const orgName = orgData?.data?.name ?? '';

  const totalFor = (yearRange: string): number =>
    allocationModules.reduce((sum, mod) => sum + parseAmount(values[mod.key]?.[yearRange] ?? ''), 0);

  const rolloverAmount = parseAmount(values[rolloverModuleKey]?.[rolloverYear] ?? '');

  const { mutate, isLoading } = useMutation({
    mutationFn: (entries: FinancialDataEntryDto[]) =>
      upsertOrganisationFinancialData(organisationId!, entries),
    async onSuccess() {
      // Drop the cached (now stale) reads so the profile's Allocations tab shows the freshly saved data.
      await queryClient.invalidateQueries(['organisation-financial-data', organisationId]);
      await queryClient.invalidateQueries([viewOrganisationHomeCacheKey(organisationId!)]);
      navigate(`/admin/organisations/${organisationId}`, { state: { financialDataSaved: true, isUpdate } });
    },
  });

  const handleConfirm = () => {
    const entries: FinancialDataEntryDto[] = [];

    for (const mod of allocationModules) {
      for (const year of allocationYears) {
        entries.push({ module: mod.key, yearRange: year, amount: parseAmount(values[mod.key]?.[year] ?? '') });
      }
    }

    if (rolloverModuleKey && rolloverYear) {
      entries.push({ module: rolloverModuleKey, yearRange: rolloverYear, amount: rolloverAmount });
    }

    mutate(entries);
  };

  const handleChange = () => {
    navigate(`/admin/organisations/${organisationId}/financial-data/add`, {
      state: { values, allocationModules, allocationYears, rolloverModuleKey, rolloverModuleLabel, rolloverYear, isUpdate },
    });
  };

  return (
    <SettingsLayout breadcrumbs={[]}>
      <a
        href={`/admin/organisations/${organisationId}/financial-data/add`}
        className="govuk-back-link"
        onClick={(e) => { e.preventDefault(); handleChange(); }}>
        Back
      </a>

      <span className="govuk-caption-l">{orgName}</span>
      <h1 className="govuk-heading-l">Check your answers</h1>

      <div className="govuk-!-margin-top-6">
        {allocationModules.map((mod) => (
          <GovUKSummaryTable
            key={mod.key}
            caption={mod.label}
            captionSize="m"
            className="govuk-!-margin-bottom-6"
            rows={allocationYears.map((year) => ({
              label: year,
              value: formatCurrency(parseAmount(values[mod.key]?.[year] ?? '')),
              changeHiddenText: `${mod.label} ${year}`,
            }))}
            onChangeAll={handleChange}
          />
        ))}

        {rolloverModuleKey && rolloverYear && (
          <GovUKSummaryTable
            caption={rolloverModuleLabel}
            captionSize="m"
            className="govuk-!-margin-bottom-6"
            rows={[{
              label: rolloverYear,
              value: formatCurrency(rolloverAmount),
              changeHiddenText: `rollover spend ${rolloverYear}`,
            }]}
            onChangeAll={handleChange}
          />
        )}
      </div>

      <div className="govuk-inset-text">
        <h2 className="govuk-heading-m">Summary</h2>
        <GovUKSummaryTable
          rows={[
            ...(rolloverModuleKey && rolloverYear ? [{
              label: `Total approved rollover spend ${rolloverYear}`,
              value: formatCurrency(rolloverAmount),
            }] : []),
            ...allocationYears.map((year) => ({
              label: `Total allocation ${year}`,
              value: formatCurrency(totalFor(year)),
            })),
          ]}
        />
      </div>

      <h2 className="govuk-heading-m">Now save financial data</h2>
      <p className="govuk-body">
        By saving the financial data, you are confirming that, to the best of your knowledge, the details you are providing are correct. You may return and update these details.
      </p>

      <button
        type="button"
        className="govuk-button"
        onClick={handleConfirm}
        disabled={isLoading}>
        Confirm and save
      </button>
    </SettingsLayout>
  );
};

export default CheckFinancialData;
