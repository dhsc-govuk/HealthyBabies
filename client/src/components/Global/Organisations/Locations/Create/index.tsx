import React, { useMemo, useReducer } from 'react';
import { useMutation, useQuery } from 'react-query';
import { LoadingSpinner, useGovUKNotification } from '../../../../../components/GovUKComponents';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { defaultStaleTime } from '../../../../../helpers/queriesParams';
import { locationReducer, initialLocationState } from '../Common';
import { getLocationTypes, getSiteTypes } from '../Common/queries';
import Steps from '../Common/components/Steps';
import { createLocation, CreateLocationDto } from './mutations';



interface Props {
  organisationId: string;
  handleFinish: (locationId: string) => void;
  apiBasePath?: string;
  onCancel?: () => void;
}

const LocationsCreateComponent = ({ organisationId, handleFinish, apiBasePath, onCancel }: Props): React.ReactElement => {
  const { setNotification } = useGovUKNotification();
  const [location, dispatchLocation] = useReducer(locationReducer, initialLocationState);

  const { data: locationTypesData } = useQuery({
    queryKey: ['location-types'],
    queryFn: getLocationTypes,
    staleTime: defaultStaleTime,
  });

  const { data: siteTypesData } = useQuery({
    queryKey: ['site-types'],
    queryFn: getSiteTypes,
    staleTime: defaultStaleTime,
  });

  const locationTypes = useMemo(() => locationTypesData?.data ?? [], [locationTypesData?.data]);
  const siteTypes = useMemo(() => siteTypesData?.data ?? [], [siteTypesData?.data]);

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['admin-locations-create'],
    mutationFn: (location: CreateLocationDto) => createLocation(organisationId!, location, apiBasePath),
    onSuccess(data, _variables, _context) {
      handleFinish(data.data.id!);
    },
    onError(error, _variables, _context) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSave = async () => {
    const loc: CreateLocationDto = {
      bsfhBranding: location.bsfhBranding,
      clarificationComments: location.clarificationComments ?? '',
      dateOpened: new Date(location.dateOpened),
      deliverySiteName: location.deliverySiteName,
      isActive: location.isActive,
      locationType: location.locationType,
      name: location.name,
      nameChange: location.nameChange,
      postCode: location.postCode,
      referenceNumber: location.referenceNumber,
      statusOfSite: location.statusOfSite,
      typeOfSite: location.typeOfSite,
    };
    await mutateAsync(loc);
  };

  return (
    <LoadingSpinner loading={saving} label="Creating site...">
      <Steps completeLabel="Create Site" location={location} dispatch={dispatchLocation} handleSave={handleSave} locationTypes={locationTypes} siteTypes={siteTypes} onCancel={onCancel} />
    </LoadingSpinner>
  );
};

export default LocationsCreateComponent;