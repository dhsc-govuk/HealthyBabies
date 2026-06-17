import axios from 'axios';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';

export interface CreateGlobalDataDto {
  entity: string;
  value: string;
  description: string | null;
}

export const createGlobalData = (data: CreateGlobalDataDto) =>
  axios.post<GlobalDataDto>('/global-data', data);
