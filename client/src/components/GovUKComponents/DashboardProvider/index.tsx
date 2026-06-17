import React, { createContext, useContext } from 'react';

interface DashboardContextType {
  dashboardItems: unknown[];
}

const DashboardContext = createContext<DashboardContextType>({ dashboardItems: [] });

interface DashboardProviderProps {
  children: React.ReactNode;
  AuthContext?: unknown;
  withRouter?: unknown;
  storageProvider?: unknown;
  dashboardItems: unknown[];
}

const DashboardProvider = ({ children, dashboardItems }: DashboardProviderProps): React.ReactElement => {
  return (
    <DashboardContext.Provider value={{ dashboardItems }}>
      {children}
    </DashboardContext.Provider>
  );
};

export const useDashboard = () => useContext(DashboardContext);

export default DashboardProvider;
