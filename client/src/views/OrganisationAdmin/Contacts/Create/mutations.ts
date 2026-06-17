import axios from 'axios';

export interface CreateOrganisationContactDto {
  organisationId: string;
  name: string;
  email: string;
  role: string;
}

export const createContact = (contact: CreateOrganisationContactDto) =>
  axios.post(`/organisations/${contact.organisationId}/contacts`, contact);
