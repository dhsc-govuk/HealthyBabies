import axios from 'axios';

export interface Organisation {
  id: string;
  name: string;
  isActive: boolean;
}

export const getOrganisations = () => axios.get<Organisation[]>('/organisations');
