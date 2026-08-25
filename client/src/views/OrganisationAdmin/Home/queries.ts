import axios from 'axios';
import { API_ROUTES } from '../../../constants/apiRoutes';



export const getHome = () => axios.get<GetHomeResponse>(API_ROUTES.organisationAdmin.totals);



export interface GetHomeResponse {

  admins: number;

  locations: number;

  contacts: number;

  serviceCategories: number;

}

