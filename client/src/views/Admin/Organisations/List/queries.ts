import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface Organisation {
  id: string;
  name: string;
  onsCode: string;
  isActive: boolean;
}

export const getOrganisations = () => axios.get<Organisation[]>(API_ROUTES.organisations.list);
