import axios from 'axios';

export interface OrganisationContactDto {
  id: string;
  organisationId: string;
  name: string;
  email: string;
  role: string;
}

export const getContact = (organisationId: string, contactId: string) =>
  axios.get<OrganisationContactDto>(`/organisations/${organisationId}/contacts/${contactId}`);
