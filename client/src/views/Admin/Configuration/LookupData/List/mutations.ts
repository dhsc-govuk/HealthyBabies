import axios from 'axios';

export const deleteGlobalData = (id: string) => axios.delete(`/global-data/${id}`);
