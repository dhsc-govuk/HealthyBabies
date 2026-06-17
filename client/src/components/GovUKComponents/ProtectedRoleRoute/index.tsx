import React from 'react';
import { Outlet } from 'react-router-dom';

interface ProtectedRoleRouteProps {
  fallbackComponent: React.ReactElement;
  allowedRoles: string[];
  userRole: string | null;
}

const ProtectedRoleRoute = ({
  fallbackComponent,
  allowedRoles,
  userRole,
}: ProtectedRoleRouteProps): React.ReactElement => {
  if (!userRole || !allowedRoles.includes(userRole)) {
    return fallbackComponent;
  }
  return <Outlet />;
};

export default ProtectedRoleRoute;
