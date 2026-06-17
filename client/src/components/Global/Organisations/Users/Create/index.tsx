import React, { useReducer } from 'react';
import { LoadingSpinner, useGovUKNotification } from '../../../../../components/GovUKComponents';
import { CreateOrganisationUserDto, createOrganisationUser } from './mutations';
import { useMutation } from 'react-query';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { organisationRoles, userReducer } from '../Common';
import UserSteps from '../Common/Steps';
import { encodeForWaf } from '../../../../../helpers/stringUtils';

interface Props {
  organisationId: string;
  handleFinish: (organisationId: string, userId: string) => void;
  onCancel?: () => void;
}

const CreateOrganisationUsersComponent = ({ organisationId, handleFinish, onCancel }: Props): React.ReactElement => {
  const { setNotification } = useGovUKNotification();
  const [organisationAdmin, dispatchOrganisationAdmin] = useReducer(userReducer, {
    firstName: '',
    lastName: '',
    email: '',
    isActive: true,
    role: organisationRoles.ORGANISATION_ADMIN,
  });

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: 'organisations-admins-create',
    mutationFn: (user: CreateOrganisationUserDto) => createOrganisationUser(organisationId!, user),
    onSuccess(data, _variables, _context) {
      setNotification({ type: 'success', title: 'Success', message: 'LA user created successfully' });
      handleFinish(organisationId!, data?.data.id!);
    },
    onError(error, _variables, _context) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleSave = async () => {
    await mutateAsync({
      ...organisationAdmin,
      firstName: encodeForWaf(organisationAdmin.firstName),
      lastName: encodeForWaf(organisationAdmin.lastName),
    });
  };

  return (
    <LoadingSpinner loading={saving} label="Saving Changes">
      <UserSteps completeLabel="Create" user={organisationAdmin} dispatch={dispatchOrganisationAdmin} handleSave={handleSave} showRoleSelect={true} onCancel={onCancel} />
    </LoadingSpinner>
  );
};

export default CreateOrganisationUsersComponent;
