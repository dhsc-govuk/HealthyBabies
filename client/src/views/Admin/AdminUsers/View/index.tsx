import React, { useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { stringFromArray } from '../../../../helpers/stringUtils';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { processError } from '../../../../helpers/axiosErrorFallback';
import { SettingsLayout } from '../../../../layouts';
import { Button, LoadingBox } from 'govuk-react';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../styles/govuk-global';
import { GovUKActionMenu, useGovUKNotification } from '../../../../components/GovUKComponents';
import type { ActionMenuItem } from '../../../../components/GovUKComponents';
import axios from 'axios';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

interface AdminUserResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  createdBy?: string;
  createdAt?: string;
  lastChangedBy?: string;
  lastChangedAt?: string;
}

interface MfaStatusResponse {
  isEnabled: boolean;
  state: number;
}

type UrlParams = {
  userId: string;
};

const getAdminUser = (userId: string) => axios.get<AdminUserResponse>(`/admin/users/${userId}`);
const getMfaStatus = (userId: string) => axios.get<MfaStatusResponse>(`/admin/users/${userId}/mfa-status`);
const enableUserMfa = (userId: string) => axios.post(`/admin/users/${userId}/mfa/enable`);
const disableUserMfa = (userId: string) => axios.post(`/admin/users/${userId}/mfa/disable`);
const resetUserMfa = (userId: string) => axios.post(`/admin/users/${userId}/mfa/reset`);

const ViewAdminUser = (): React.ReactElement => {
  usePageTitle('View departmental user');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const { userId } = useParams<UrlParams>();

  const { data, isLoading } = useQuery({
    queryKey: ['departmental-user-view', userId],
    queryFn: () => getAdminUser(userId!),
  });

  const { data: mfaStatus } = useQuery({
    queryKey: ['departmental-user-mfa-status', userId],
    queryFn: () => getMfaStatus(userId!),
  });

  const { mutateAsync: enableMfa } = useMutation({
    mutationKey: ['departmental-user-enable-mfa'],
    mutationFn: () => enableUserMfa(userId!),
    onSuccess() {
      setNotification({ type: 'success', title: 'MFA enabled successfully' });
      queryClient.invalidateQueries(['departmental-user-mfa-status', userId]);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const { mutateAsync: disableMfa } = useMutation({
    mutationKey: ['departmental-user-disable-mfa'],
    mutationFn: () => disableUserMfa(userId!),
    onSuccess() {
      setNotification({ type: 'success', title: 'MFA disabled successfully' });
      queryClient.invalidateQueries(['departmental-user-mfa-status', userId]);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const { mutateAsync: resetMfa } = useMutation({
    mutationKey: ['departmental-user-reset-mfa'],
    mutationFn: () => resetUserMfa(userId!),
    onSuccess() {
      setNotification({ type: 'success', title: 'MFA reset successfully' });
      queryClient.invalidateQueries(['departmental-user-mfa-status', userId]);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleEdit = () => {
    navigate(`/admin/departmental-users/${userId}/edit`);
  };

  const actionMenuItems: ActionMenuItem[] = useMemo(() => {
    const items: ActionMenuItem[] = [];

    // MFA State: 0=None, 1=PendingSetup, 2=Enabled, 3=Disabled
    const state = mfaStatus?.data?.state ?? 0;

    if (state === 2) {
      items.push(
        { label: 'Disable MFA', onClick: async () => { await disableMfa(); } },
        { label: 'Reset MFA', onClick: async () => { await resetMfa(); }, dividerBefore: true }
      );
    } else {
      items.push({ label: 'Activate MFA', onClick: async () => { await enableMfa(); } });
    }

    items.push({ label: 'Delete account', href: `/admin/departmental-users/${userId}/delete`, dividerBefore: true });

    return items;
  }, [mfaStatus, userId, disableMfa, resetMfa, enableMfa]);

  // MFA State: 0=None, 1=PendingSetup, 2=Enabled, 3=Disabled
  const mfaState = mfaStatus?.data?.state ?? 0;
  const mfaStatusText = mfaState === 2 ? 'Enabled' : mfaState === 3 ? 'Disabled' : 'Not set up';

  const user = data?.data;
  const fullName = user ? stringFromArray([user.firstName, user.lastName]) : '';

  usePageTitle(fullName || 'View departmental user');

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
        { label: 'Departmental users', link: '/admin/departmental-users' },
      ]}>
      <LoadingBox loading={isLoading}>
        {user && (
          <>
            <PageHeaderContainer>
              <PageTitle>{fullName}</PageTitle>
              <PageHeaderActions>
                <Button onClick={handleEdit}>Change</Button>
                <GovUKActionMenu items={actionMenuItems} />
              </PageHeaderActions>
            </PageHeaderContainer>

            <dl className="govuk-summary-list">
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">First name</dt>
                <dd className="govuk-summary-list__value">{user.firstName}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Last name</dt>
                <dd className="govuk-summary-list__value">{user.lastName}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Role</dt>
                <dd className="govuk-summary-list__value">Admin</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Email address</dt>
                <dd className="govuk-summary-list__value">
                  <a href={`mailto:${user.email}`} className="govuk-link">{user.email}</a>
                </dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Account status</dt>
                <dd className="govuk-summary-list__value">{user.isActive ? 'Active' : 'Inactive'}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Multi-factor authentication (MFA) status</dt>
                <dd className="govuk-summary-list__value">{mfaStatusText}</dd>
              </div>
              {user.createdBy && user.createdAt && (
                <div className="govuk-summary-list__row">
                  <dt className="govuk-summary-list__key">Created</dt>
                  <dd className="govuk-summary-list__value">
                    {user.createdBy}<br />
                    {new Date(user.createdAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}, {new Date(user.createdAt).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}
                  </dd>
                </div>
              )}
              {user.lastChangedBy && user.lastChangedAt && (
                <div className="govuk-summary-list__row">
                  <dt className="govuk-summary-list__key">Last changed</dt>
                  <dd className="govuk-summary-list__value">
                    {user.lastChangedBy}<br />
                    {new Date(user.lastChangedAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}, {new Date(user.lastChangedAt).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}
                  </dd>
                </div>
              )}
            </dl>
          </>
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default ViewAdminUser;
