import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

interface UpdateOrganisationDto {
  id: string;
  name: string;
  onsCode: string;
  isActive: boolean;
}

export const updateOrganisation = (organisation: UpdateOrganisationDto) => axios.put<UpdateOrganisationDto>(API_ROUTES.organisations.edit, organisation);
