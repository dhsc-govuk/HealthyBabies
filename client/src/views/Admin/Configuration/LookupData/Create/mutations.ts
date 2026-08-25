import axios from 'axios';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface CreateGlobalDataDto {
  entity: string;
  value: string;
  description: string | null;
}

export const createGlobalData = (data: CreateGlobalDataDto) =>
  axios.post<GlobalDataDto>(API_ROUTES.globalData.create, data);
