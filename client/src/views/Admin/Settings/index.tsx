import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

const Settings = (): React.ReactElement | null => {
  const navigate = useNavigate();

  useEffect(() => {
    navigate('/admin/organisations', { replace: true });
  }, [navigate]);

  return null;
};

export default Settings;
