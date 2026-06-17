import React, { useEffect, useReducer } from 'react';
import { LoadingSpinner, useGovUKNotification } from '../../../../../components/GovUKComponents';
import { getOrganisationUser } from './queries';
import { EditOrganisationUserDto, updateOrganisationUser } from './mutations';
import { useMutation, useQuery } from 'react-query';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { organisationRoles, userReducer, UserReducerAction } from '../Common';
import UserSteps from '../Common/Steps';
import { organisationUserStaleTime, viewOrganisationUsersCacheKey } from '../../../../../helpers/queriesParams';

interface Props {
  organisationId: string;
  userId: string;
  handleFinish: (organisationId: string, userId: string) => void;
  onCancel?: () => void;
}

const EditOrganisationUsersComponent = ({ organisationId, userId, handleFinish, onCancel }: Props): React.ReactElement => {
  const { setNotification } = useGovUKNotification();
  const [organisationAdmin, dispatchOrganisationAdmin] = useReducer(userReducer, {
    firstName: '',
    lastName: '',
    email: '',
    isActive: true,
    role: organisationRoles.ORGANISATION_ADMIN,
  });

  const { data: userData, isLoading: userLoading } = useQuery({
    queryKey: viewOrganisationUsersCacheKey(userId!),
    queryFn: () => getOrganisationUser(organisationId!, userId!),
    staleTime: organisationUserStaleTime,
  });

  useEffect(() => {
    if (userData?.data) {
      dispatchOrganisationAdmin({ type: UserReducerAction.INIT, value: userData.data });
    }
  }, [userData]);

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: 'admin-organisations-admins-update',
    mutationFn: (user: EditOrganisationUserDto) => updateOrganisationUser(organisationId!, user),
    onSuccess(data, _variables, _context) {
      handleFinish(organisationId, data.data.id!);
    },
    onError(error, _variables, _context) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });
  const handleSave = async () => {
    await mutateAsync({ ...organisationAdmin, id: userId });
  };

  return (
    <LoadingSpinner loading={userLoading || saving} label={userLoading ? 'Loading' : 'Saving Changes'}>
      <UserSteps completeLabel="Save" user={organisationAdmin} dispatch={dispatchOrganisationAdmin} handleSave={handleSave} showRoleSelect={true} isEdit={true} onCancel={onCancel} />
    </LoadingSpinner>
  );
};

export default EditOrganisationUsersComponent;
