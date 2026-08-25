import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface CreateOrganisationContactDto {
  organisationId: string;
  name: string;
  email: string;
  role: string;
}

export const createContact = (contact: CreateOrganisationContactDto) =>
  axios.post(API_ROUTES.organisations.contacts.create(contact.organisationId), contact);
