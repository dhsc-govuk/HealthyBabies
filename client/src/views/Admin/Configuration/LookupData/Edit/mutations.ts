import axios from 'axios';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface UpdateGlobalDataDto {
  id: string;
  entity: string;
  value: string;
  description: string | null;
}

export const updateGlobalData = (data: UpdateGlobalDataDto) =>
  axios.put<GlobalDataDto>(API_ROUTES.globalData.byId(data.id), data);
