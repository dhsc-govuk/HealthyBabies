import { useQuery } from 'react-query';
import axios from 'axios';
import { defaultStaleTime, viewServiceDeliverySiteCacheKey } from '../../../helpers/queriesParams';
import { API_ROUTES } from '../../../constants/apiRoutes';

interface GetLocationDto {
  id: string;
  name: string;
  isActive: boolean;
}

export const useLocationQuery = ({ organisationId, locationId, apiBasePath }: { organisationId: string; locationId: string; apiBasePath?: string }) =>
  useQuery({
    queryKey: [viewServiceDeliverySiteCacheKey(organisationId, locationId), apiBasePath],
    queryFn: () => {
      const url = apiBasePath ? `${apiBasePath}/${locationId}` : API_ROUTES.organisations.locations.byId(organisationId, locationId);
      return axios.get<GetLocationDto>(url);
    },
    staleTime: defaultStaleTime,
  });
