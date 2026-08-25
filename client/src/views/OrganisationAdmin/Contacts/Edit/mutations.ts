import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface UpdateOrganisationContactDto {
  organisationId: string;
  contactId: string;
  name: string;
  email: string;
  role: string;
}

export const updateContact = (contact: UpdateOrganisationContactDto) =>
  axios.put(API_ROUTES.organisations.contacts.byId(contact.organisationId, contact.contactId), {
    name: contact.name,
    email: contact.email,
    role: contact.role,
  });
