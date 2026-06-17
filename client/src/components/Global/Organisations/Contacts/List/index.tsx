import React from 'react';
import { Link } from 'govuk-react';
import { LoadingSpinner, GovUKTable } from '../../../../GovUKComponents';
import type { Column } from '../../../../GovUKComponents';
import { getOrganisationContacts, OrganisationContactDto } from './queries';
import { useQuery } from 'react-query';

interface Props {
  organisationId: string;
  handleView: (contactId: string) => void;
  handleEdit?: (contactId: string) => void;
}

const ListOrganisationContactsComponent = ({ organisationId, handleView, handleEdit }: Props): React.ReactElement => {
  const { data: contactsData, isLoading: contactsLoading } = useQuery({
    queryKey: ['organisation-contacts', organisationId],
    queryFn: () => getOrganisationContacts(organisationId),
  });

  const columns: Column<OrganisationContactDto>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (contact) => (
        <Link
          href="#"
          onClick={(e: React.MouseEvent) => {
            e.preventDefault();
            handleView(contact.id);
          }}>
          {contact.name}
        </Link>
      ),
    },
    {
      key: 'email',
      header: 'Email',
      render: (contact) => contact.email,
    },
    {
      key: 'role',
      header: 'Role',
      render: (contact) => contact.role ?? '-',
    },
    {
      key: 'actions',
      header: '',
      align: 'right',
      render: (contact) => (
        <div className="govuk-table__actions">
          <Link
            href="#"
            onClick={(e: React.MouseEvent) => {
              e.preventDefault();
              handleView(contact.id);
            }}>
            View<span className="govuk-visually-hidden"> {contact.name}</span>
          </Link>
          {handleEdit && (
            <>
              {' '}
              <Link
                href="#"
                onClick={(e: React.MouseEvent) => {
                  e.preventDefault();
                  handleEdit(contact.id);
                }}>
                Edit<span className="govuk-visually-hidden"> {contact.name}</span>
              </Link>
            </>
          )}
        </div>
      ),
    },
  ];

  return (
    <LoadingSpinner loading={contactsLoading} label="Loading">
      <GovUKTable<OrganisationContactDto>
        data={contactsData?.data ?? []}
        columns={columns}
        keyExtractor={(contact) => contact.id}
        searchable={false}
        emptyMessage="No contacts found"
      />
    </LoadingSpinner>
  );
};

export default ListOrganisationContactsComponent;
