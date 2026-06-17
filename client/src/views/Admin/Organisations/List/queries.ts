import axios from 'axios';

export interface Organisation {
  id: string;
  name: string;
  onsCode: string;
  isActive: boolean;
}

export const getOrganisations = () => axios.get<Organisation[]>('/organisations');
