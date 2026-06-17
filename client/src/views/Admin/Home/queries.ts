import axios from 'axios';

export const getHome = () => axios.get<GetHomeResponse>('admin/totals');

export interface GetHomeResponse {
  admins: number;
  organisations: number;
  carerTypes: number;
  contactForCalls: number;
  contactMethods: number;
  languages: number;
}
