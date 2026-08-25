import axios from 'axios';
import { API_ROUTES } from '../../../constants/apiRoutes';

export const getHome = () => axios.get<GetHomeResponse>(API_ROUTES.admin.totals);

export interface GetHomeResponse {
  admins: number;
  organisations: number;
  carerTypes: number;
  contactForCalls: number;
  contactMethods: number;
  languages: number;
}
