import axios from 'axios';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';

export interface UpdateGlobalDataDto {
  id: string;
  entity: string;
  value: string;
  description: string | null;
}

export const updateGlobalData = (data: UpdateGlobalDataDto) =>
  axios.put<GlobalDataDto>(`/global-data/${data.id}`, data);
