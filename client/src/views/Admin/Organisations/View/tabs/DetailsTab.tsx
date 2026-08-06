import React from 'react';
import { Link } from 'govuk-react';
import { OrganisationContactDetailDto, OrganisationHomeDto } from '../queries';

interface DetailsTabProps {
  org: OrganisationHomeDto;
  onEdit: () => void;
}

const formatDate = (dateString?: string): string => {
  if (!dateString) return '-';
  return new Date(dateString).toLocaleDateString('en-GB', {
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
    assistant: 'Assistant',
    admin: 'Administrator',
    manager: 'Manager',
    'data-entry': 'Data Entry',
    other: contact.roleTitle || 'Other',
  };
  return roleMap[contact.role] || contact.role;
};

const DetailsTab = ({ org, onEdit }: DetailsTabProps): React.ReactElement => (
  <>
    <p className="govuk-body govuk-!-text-align-right">
      <Link
        href="#"
        onClick={(e: React.MouseEvent) => {
          e.preventDefault();
          onEdit();
        }}>
        Change<span className="govuk-visually-hidden"> local authority details</span>
      </Link>
    </p>

    <dl className="govuk-summary-list">
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
            <div>
              <a href={`mailto:${contact.email}`} className="govuk-link">
                {contact.email}
              </a>
            </div>
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
  </>
);

export default DetailsTab;
