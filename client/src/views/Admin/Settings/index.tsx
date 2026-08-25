import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ROUTES } from '../../../constants/routes';

const Settings = (): React.ReactElement | null => {
  const navigate = useNavigate();

  useEffect(() => {
    navigate(ROUTES.admin.organisations.list, { replace: true });
  }, [navigate]);

  return null;
};

export default Settings;
