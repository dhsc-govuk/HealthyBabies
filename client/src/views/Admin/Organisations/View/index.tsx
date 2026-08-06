import React, { useEffect, useState } from 'react';
import { useQuery } from 'react-query';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { LoadingBox } from 'govuk-react';
import { getOrganisationHome, getOrganisationUsers, getOrganisationFinancialData, FinancialDataEntryDto } from './queries';
import { defaultStaleTime, viewOrganisationHomeCacheKey } from '../../../../helpers/queriesParams';
import { SettingsLayout } from '../../../../layouts';
import { PageHeaderContainer, PageHeaderActions } from '../../../../styles/govuk-global';
import { GovUKActionMenu, GovUKTabs } from '../../../../components/GovUKComponents';
import type { GovUKTab } from '../../../../components/GovUKComponents';
import usePageTitle from '../../../../hooks/usePageTitle';
import { FormValues } from '../FinancialData/constants';
import DetailsTab from './tabs/DetailsTab';
import AllocationsTab from './tabs/AllocationsTab';
import FinancialDataTab from './tabs/FinancialDataTab';
import DeliveryDataTab from './tabs/DeliveryDataTab';
import UsersTab, { UserRow } from './tabs/UsersTab';
import './styles.css';

type UrlParams = {
  organisationId: string;
};

interface LocationState {
  financialDataSaved?: boolean;
  isUpdate?: boolean;
}

const ViewOrganisation = (): React.ReactElement => {
  usePageTitle('View local authority');
  const { organisationId } = useParams<UrlParams>();
  const navigate = useNavigate();
  const location = useLocation();
  const [showFinancialDataBanner, setShowFinancialDataBanner] = useState(false);
  const [financialDataWasUpdated, setFinancialDataWasUpdated] = useState(false);

  useEffect(() => {
    const state = location.state as LocationState;
    if (state?.financialDataSaved) {
      setShowFinancialDataBanner(true);
      setFinancialDataWasUpdated(state.isUpdate ?? false);
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  const { data, isLoading } = useQuery({
    queryKey: [viewOrganisationHomeCacheKey(organisationId!)],
    queryFn: () => getOrganisationHome(organisationId!),
    staleTime: defaultStaleTime,
  });

  const { data: financialData } = useQuery({
    queryKey: ['organisation-financial-data', organisationId],
    queryFn: () => getOrganisationFinancialData(organisationId!),
    staleTime: defaultStaleTime,
  });

  const { data: usersData, isLoading: usersLoading } = useQuery({
    queryKey: ['organisation-users', organisationId],
    queryFn: () => getOrganisationUsers(organisationId!),
    staleTime: defaultStaleTime,
  });

  const org = data?.data;
  const entries: FinancialDataEntryDto[] = financialData?.data ?? [];
  const hasFinancialData = entries.length > 0;
  const users: UserRow[] = usersData?.data ?? [];

  usePageTitle(org?.name ?? 'View local authority');

  const handleEdit = () => navigate(`/admin/organisations/${organisationId}/edit`);
  const handleEditFinancialData = () => {
    const existingValues: FormValues = entries.reduce<FormValues>(
      (acc, entry) => ({
        ...acc,
        [entry.module]: { ...(acc[entry.module] ?? {}), [entry.yearRange]: entry.amount.toString() },
      }),
      {},
    );
    navigate(`/admin/organisations/${organisationId}/financial-data/add`, {
      state: { isUpdate: hasFinancialData, values: hasFinancialData ? existingValues : undefined },
    });
  };

  const tabs: GovUKTab[] = org
    ? [
        { id: 'details', label: 'Details', panel: <DetailsTab org={org} onEdit={handleEdit} /> },
        { id: 'allocations', label: 'Allocations', panel: <AllocationsTab entries={entries} onChange={handleEditFinancialData} /> },
        { id: 'financial-data', label: 'Financial data', panel: <FinancialDataTab organisationId={organisationId!} /> },
        { id: 'delivery-data', label: 'Delivery data', panel: <DeliveryDataTab organisationId={organisationId!} /> },
        { id: 'users', label: 'Users', panel: <UsersTab users={users} isLoading={usersLoading} organisationId={organisationId!} /> },
      ]
    : [];

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
        { label: 'Local authorities', link: '/admin/organisations' },
      ]}>
      <LoadingBox loading={isLoading}>
        {showFinancialDataBanner && (
          <div
            className="govuk-notification-banner govuk-notification-banner--success"
            role="alert"
            aria-labelledby="govuk-notification-banner-title"
            data-module="govuk-notification-banner">
            <div className="govuk-notification-banner__header">
              <h2 className="govuk-notification-banner__title" id="govuk-notification-banner-title">
                Success
              </h2>
            </div>
            <div className="govuk-notification-banner__content">
              <h3 className="govuk-notification-banner__heading">
                {financialDataWasUpdated ? 'Financial data updated' : 'Financial data added'}
              </h3>
              <p className="govuk-body">
                {financialDataWasUpdated
                  ? 'Financial data has been updated for the local authority.'
                  : 'Financial data has been saved for the local authority. You can update it at any time.'}
              </p>
            </div>
          </div>
        )}
        {org && (
          <>
            <PageHeaderContainer>
              <h1 className="govuk-heading-l">{org.name}</h1>
              <PageHeaderActions>
                <GovUKActionMenu
                  items={[
                    { label: 'Edit details', onClick: handleEdit },
                    { label: hasFinancialData ? 'Update financial data' : 'Add financial data', onClick: handleEditFinancialData },
                    { label: 'Add contact', href: `/admin/organisations/${organisationId}/contacts/create` },
                    { label: 'Add LA user', href: `/admin/la-users/create?organisationId=${organisationId}` },
                  ]}
                />
              </PageHeaderActions>
            </PageHeaderContainer>

            <GovUKTabs tabs={tabs} />
          </>
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default ViewOrganisation;
