import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { SettingsLayout } from '../../../../layouts';
import {
  getDataCollectionFull,
  duplicateDataCollection,
} from '../List/queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { GovUKButton, GovUKActionMenu, useGovUKNotification } from '../../../../components/GovUKComponents';
import type { ActionMenuItem } from '../../../../components/GovUKComponents';
import { PageHeaderContainer, PageHeaderActions } from '../../../../styles/govuk-global';
import LocalAuthorityManager from './LocalAuthorityManager';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};


const formatDateTime = (dateString: string): string => {
  const date = new Date(dateString);
  const dateFormatted = date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
  const timeFormatted = date.toLocaleTimeString('en-GB', {
    hour: '2-digit',
    minute: '2-digit',
  });
  return `${dateFormatted}, ${timeFormatted}`;
};

const ViewDataCollection = (): React.ReactElement => {
  usePageTitle('View data collection');
  const { dataCollectionId } = useParams<{ dataCollectionId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['data-collection-full', dataCollectionId],
    queryFn: () => getDataCollectionFull(dataCollectionId!),
    enabled: !!dataCollectionId,
  });

  const { mutateAsync: duplicateMutation } = useMutation({
    mutationFn: () => duplicateDataCollection(dataCollectionId!),
    onSuccess: (response) => {
      setNotification({ type: 'success', title: 'Success', message: 'Data collection duplicated successfully' });
      queryClient.invalidateQueries(['data-collections-list']);
      navigate(`/admin/data-collections/${response.data.id}`);
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const dataCollection = data?.data;
  const status = dataCollection?.status ?? 'Draft';

  usePageTitle(dataCollection?.name ?? 'View data collection');

  const actionMenuItems: ActionMenuItem[] = [
    {
      label: 'View submissions',
      href: `/admin/submissions/${dataCollectionId}`,
    },
    {
      label: 'Revert to a draft',
      href: `/admin/data-collections/${dataCollectionId}/revert-to-draft`,
      dividerBefore: true,
    },
    {
      label: 'Duplicate to a new draft',
      onClick: async () => { await duplicateMutation(); },
    },
    {
      label: 'Close data collection',
      href: `/admin/data-collections/${dataCollectionId}/close`,
    },
    {
      label: 'Delete data collection',
      href: `/admin/data-collections/${dataCollectionId}/delete`,
      dividerBefore: true,
    },
  ];

  if (isError) {
    return (
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Settings', link: '/admin/settings' },
          { label: 'Data collections', link: '/admin/data-collections' },
        ]}
      >
        <h1 className="govuk-heading-l">Error</h1>
        <p className="govuk-body">Unable to load data collection.</p>
        <GovUKButton onClick={() => navigate('/admin/data-collections')}>
          Back to list
        </GovUKButton>
      </SettingsLayout>
    );
  }

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
        { label: 'Data collections', link: '/admin/data-collections' },
      ]}
    >
      <LoadingBox loading={isLoading}>
        {dataCollection && (
          <>
            <PageHeaderContainer>
              <h1 className="govuk-heading-l">{dataCollection.name}</h1>
              <PageHeaderActions>
                <Button onClick={() => navigate(`/admin/data-collections/${dataCollectionId}/edit`)}>
                  Change
                </Button>
                <GovUKActionMenu items={actionMenuItems} />
              </PageHeaderActions>
            </PageHeaderContainer>

            <dl className="govuk-summary-list">
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Name</dt>
                <dd className="govuk-summary-list__value">{dataCollection.name}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Start date</dt>
                <dd className="govuk-summary-list__value">{formatDate(dataCollection.startDate)}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Due date</dt>
                <dd className="govuk-summary-list__value">{formatDate(dataCollection.endDate)}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Status</dt>
                <dd className="govuk-summary-list__value">{status}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Forms</dt>
                <dd className="govuk-summary-list__value">
                  {dataCollection.formModules && dataCollection.formModules.length > 0 ? (
                    dataCollection.formModules.map((fm, index) => (
                      <React.Fragment key={fm.id}>
                        Section {fm.sectionNumber}: {fm.name}
                        {index < dataCollection.formModules!.length - 1 && <br />}
                      </React.Fragment>
                    ))
                  ) : (
                    <span className="govuk-hint">No forms assigned</span>
                  )}
                </dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Is this data collection submitted by all local authorities?</dt>
                <dd className="govuk-summary-list__value">
                  {dataCollection.isSubmittedByAllLocalAuthorities ? 'Yes' : 'No'}
                </dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Created</dt>
                <dd className="govuk-summary-list__value">
                  {dataCollection.createdByName || 'Unknown'}
                  <br />
                  {formatDateTime(dataCollection.createdAt)}
                </dd>
              </div>
              {dataCollection.updatedAt && (
                <div className="govuk-summary-list__row">
                  <dt className="govuk-summary-list__key">Last changed</dt>
                  <dd className="govuk-summary-list__value">
                    {dataCollection.lastModifiedByName || 'Unknown'}
                    <br />
                    {formatDateTime(dataCollection.updatedAt)}
                  </dd>
                </div>
              )}
            </dl>

            <LocalAuthorityManager
              dataCollectionId={dataCollectionId!}
              assignedLocalAuthorities={dataCollection.localAuthorities || []}
              collectionStartDate={dataCollection.startDate}
              collectionEndDate={dataCollection.endDate}
              onUpdate={() => refetch()}
            />
          </>
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default ViewDataCollection;
