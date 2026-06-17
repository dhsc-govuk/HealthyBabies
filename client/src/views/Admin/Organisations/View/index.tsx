import React from 'react';
import { useQuery } from 'react-query';
import { getOrganisationHome, getOrganisationUsers, OrganisationContactDetailDto } from './queries';
import { useNavigate, useParams } from 'react-router-dom';
import { defaultStaleTime, viewOrganisationHomeCacheKey } from '../../../../helpers/queriesParams';
import { SettingsLayout } from '../../../../layouts';
import { Button, LoadingBox, Link } from 'govuk-react';
import { PageHeaderContainer, PageHeaderActions } from '../../../../styles/govuk-global';
import { GovUKTable, GovUKActionMenu } from '../../../../components/GovUKComponents';
import type { Column } from '../../../../components/GovUKComponents';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

type UrlParams = {
  organisationId: string;
};

interface UserRow {
  id: string;
  name: string;
  role: string;
  isActive: boolean;
}

const formatDate = (dateString?: string): string => {
  if (!dateString) return '-';
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const getRoleDisplayName = (contact: OrganisationContactDetailDto): string => {
  if (contact.role === 'other' && contact.roleTitle) return contact.roleTitle;
  const roleMap: Record<string, string> = {
    'programme-lead': 'Programme Lead',
    'assistant': 'Assistant',
    'admin': 'Administrator',
    'manager': 'Manager',
    'data-entry': 'Data Entry',
    'other': contact.roleTitle || 'Other'
  };
  return roleMap[contact.role] || contact.role;
};

const ViewOrganisation = (): React.ReactElement => {
  usePageTitle('View local authority');
  const { organisationId } = useParams<UrlParams>();
  const navigate = useNavigate();

  const { data, isLoading } = useQuery({
    queryKey: [viewOrganisationHomeCacheKey(organisationId!)],
    queryFn: () => getOrganisationHome(organisationId!),
    staleTime: defaultStaleTime,
  });

  const { data: usersData, isLoading: usersLoading } = useQuery({
    queryKey: ['organisation-users', organisationId],
    queryFn: () => getOrganisationUsers(organisationId!),
    staleTime: defaultStaleTime,
  });

  const handleEdit = () => {
    navigate(`/admin/organisations/${organisationId}/edit`);
  };

  const org = data?.data;

  usePageTitle(org?.name ?? 'View local authority');
  const users = usersData?.data || [];

  const userColumns: Column<UserRow>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (user) => (
        <Link href={`/admin/la-users/${user.id}`} onClick={(e: React.MouseEvent) => { e.preventDefault(); navigate(`/admin/la-users/${user.id}`); }}>
          {user.name}
        </Link>
      ),
    },
    {
      key: 'role',
      header: 'Role',
    },
    {
      key: 'status',
      header: '',
      align: 'right',
      render: (user) => (
        user.isActive ? (
          <span>Active</span>
        ) : (
          <span className="govuk-tag" style={{ textTransform: 'none', backgroundColor: '#fff7bf', color: '#594d00' }}>Inactive</span>
        )
      ),
    },
  ];

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
        { label: 'Local authorities', link: '/admin/organisations' },
      ]}>
      <LoadingBox loading={isLoading}>
        {org && (
          <>
            <PageHeaderContainer>
              <h1 className="govuk-heading-l">{org.name}</h1>
              <PageHeaderActions>
                <Button onClick={handleEdit}>Change</Button>
                <GovUKActionMenu
                  items={[
                    {
                      label: 'Add contact',
                      href: `/admin/organisations/${organisationId}/contacts/create`,
                    },
                  ]}
                />
              </PageHeaderActions>
            </PageHeaderContainer>

            <dl className="govuk-summary-list org-view-summary">
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Name</dt>
                <dd className="govuk-summary-list__value">{org.name}</dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">ONS code</dt>
                <dd className="govuk-summary-list__value">{org.onsCode || '-'}</dd>
              </div>
              {org.contactDetails?.map((contact, index) => (
                <div key={index} className="govuk-summary-list__row">
                  <dt className="govuk-summary-list__key">Contact person</dt>
                  <dd className="govuk-summary-list__value">
                    <div>{contact.fullName}</div>
                    <div>{getRoleDisplayName(contact)}</div>
                    <div><a href={`mailto:${contact.email}`} className="govuk-link">{contact.email}</a></div>
                  </dd>
                </div>
              ))}
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Created</dt>
                <dd className="govuk-summary-list__value">
                  {org.createdBy && <div>{org.createdBy}</div>}
                  <div>{formatDate(org.createdAt)}</div>
                </dd>
              </div>
              <div className="govuk-summary-list__row">
                <dt className="govuk-summary-list__key">Last changed</dt>
                <dd className="govuk-summary-list__value">
                  {org.lastChangedBy && <div>{org.lastChangedBy}</div>}
                  <div>{formatDate(org.lastChangedAt)}</div>
                </dd>
              </div>
            </dl>

            <div className="org-view-users-section">
              <PageHeaderContainer>
                <h2 className="govuk-heading-m">LA users</h2>
                <PageHeaderActions>
                  <button type="button" className="govuk-button govuk-button--secondary" onClick={() => navigate(`/admin/la-users/create?organisationId=${organisationId}`)}>Add LA user</button>
                </PageHeaderActions>
              </PageHeaderContainer>

              <LoadingBox loading={usersLoading}>
                <GovUKTable<UserRow>
                  data={users}
                  columns={userColumns}
                  searchPlaceholder="Search LA user by name"
                  sortLabel="Sort users"
                  sortOptions={[
                    { value: 'status', label: 'By status' },
                    { value: 'name', label: 'By name' },
                  ]}
                  keyExtractor={(user) => user.id}
                  getRowHref={(user) => `/admin/la-users/${user.id}`}
                />
              </LoadingBox>
            </div>
          </>
        )}
      </LoadingBox>
    </SettingsLayout>
  );
};

export default ViewOrganisation;
