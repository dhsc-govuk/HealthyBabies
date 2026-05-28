import axios from 'axios';
import { GlobalDataDto } from '../../../../../components/Global/Queries/globalData';

export const getGlobalDataById = (id: string) => axios.get<GlobalDataDto>(`/global-data/${id}`);
