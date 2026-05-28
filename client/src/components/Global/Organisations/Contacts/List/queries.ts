import axios from 'axios';

export interface OrganisationContactDto {
  id: string;
  organisationId: string;
  name: string;
  email: string;
  role: string;
}

export const getOrganisationContacts = (organisationId: string) =>
  axios.get<OrganisationContactDto[]>(`/organisations/${organisationId}/contacts`);
