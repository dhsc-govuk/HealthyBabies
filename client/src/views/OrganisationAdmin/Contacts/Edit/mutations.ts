import axios from 'axios';

export interface UpdateOrganisationContactDto {
  organisationId: string;
  contactId: string;
  name: string;
  email: string;
  role: string;
}

export const updateContact = (contact: UpdateOrganisationContactDto) =>
  axios.put(`/organisations/${contact.organisationId}/contacts/${contact.contactId}`, {
    name: contact.name,
    email: contact.email,
    role: contact.role,
  });
