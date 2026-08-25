import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface OrganisationContactDto {
  id: string;
  organisationId: string;
  name: string;
  email: string;
  role: string;
}

export const getContact = (organisationId: string, contactId: string) =>
  axios.get<OrganisationContactDto>(API_ROUTES.organisations.contacts.byId(organisationId, contactId));
