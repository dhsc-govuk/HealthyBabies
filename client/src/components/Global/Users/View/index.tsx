import React, { useMemo } from 'react';
import { Grid } from '@mui/material';
import { SummaryList, GovUKActionMenu } from '../../../../components/GovUKComponents';
import type { ActionMenuItem } from '../../../../components/GovUKComponents';
import { booleanToYesNo, capitaliseFirst, stringFromArray } from '../../../../helpers/stringUtils';
import { User } from '..';

type MfaState = 'None' | 'PendingSetup' | 'Enabled' | 'Disabled';

interface Props {
  user: User;
  mfaState?: MfaState;
  handleEdit: () => void;
  handleDelete?: () => void;
  handleEnableMfa?: () => void;
  handleDisableMfa?: () => void;
  handleResetMfa?: () => void;
  enablingMfa?: boolean;
  disablingMfa?: boolean;
  resettingMfa?: boolean;
}

const getMfaStatusLabel = (state: MfaState): string => {
  switch (state) {
    case 'Enabled':
      return 'Enabled';
    case 'Disabled':
    case 'PendingSetup':
      return 'Disabled';
    case 'None':
    default:
      return 'Pending Setup';
  }
};

const ViewComponent = ({
  user,
  mfaState = 'None',
  handleEdit,
  handleDelete,
  handleEnableMfa,
  handleDisableMfa,
  handleResetMfa,
  enablingMfa = false,
  disablingMfa = false,
  resettingMfa = false,
}: Props): React.ReactElement => {
  const items = [
    { label: 'Full Name', value: stringFromArray([user.firstName, user.lastName]) },
    { label: 'Email', value: user.email },
    { label: 'Active', value: booleanToYesNo(user.isActive) },
    { label: 'Multi-factor authentication (MFA)', value: getMfaStatusLabel(mfaState) },
  ];

  if (user.role) {
    const role = user.role === 'organisation admin' ? 'LA Admin' : user.role;
    items.push({ label: 'Role', value: capitaliseFirst(role) });
  }

  const actionMenuItems: ActionMenuItem[] = useMemo(() => {
    const menuItems: ActionMenuItem[] = [
      { label: 'Edit user', onClick: handleEdit },
    ];

    if (handleEnableMfa && (mfaState === 'Disabled' || mfaState === 'PendingSetup')) {
      menuItems.push({
        label: enablingMfa ? 'Enabling...' : 'Enable MFA',
        onClick: handleEnableMfa,
        dividerBefore: true,
      });
    }

    if (handleDisableMfa && mfaState === 'Enabled') {
      menuItems.push({
        label: disablingMfa ? 'Disabling...' : 'Disable MFA',
        onClick: handleDisableMfa,
        dividerBefore: true,
      });
    }

    if (handleResetMfa && mfaState === 'Enabled') {
      menuItems.push({
        label: resettingMfa ? 'Resetting...' : 'Reset MFA',
        onClick: handleResetMfa,
      });
    }

    if (handleDelete) {
      menuItems.push({
        label: 'Delete user',
        onClick: handleDelete,
        dividerBefore: true,
      });
    }

    return menuItems;
  }, [handleEdit, handleEnableMfa, handleDisableMfa, handleResetMfa, handleDelete, mfaState, enablingMfa, disablingMfa, resettingMfa]);

  return (
    <Grid container spacing={4}>
      <Grid item xs={12}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '20px' }}>
          <div style={{ flex: 1 }} />
          <GovUKActionMenu items={actionMenuItems} />
        </div>
        <SummaryList items={items} />
      </Grid>
    </Grid>
  );
};

export default ViewComponent;
