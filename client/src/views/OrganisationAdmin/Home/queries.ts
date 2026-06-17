import axios from 'axios';



export const getHome = () => axios.get<GetHomeResponse>('organisation-admin/totals');



export interface GetHomeResponse {

  admins: number;

  locations: number;

  contacts: number;

  serviceCategories: number;

}

