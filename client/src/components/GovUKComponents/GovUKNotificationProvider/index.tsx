import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import GovUKNotificationBanner from '../GovUKNotificationBanner';

export interface GovUKNotification {
  type: 'success' | 'important';
  title: string;
  message?: string;
}

interface GovUKNotificationContextValue {
  notification: GovUKNotification | null;
  notificationKey: number;
  setNotification: (notification: GovUKNotification | null) => void;
}

const GovUKNotificationContext = createContext<GovUKNotificationContextValue | null>(null);

export function GovUKNotificationProvider({ children }: { children: React.ReactNode }): React.ReactElement {
  const [notification, setNotificationState] = useState<GovUKNotification | null>(null);
  const [notificationKey, setNotificationKey] = useState(0);
  const navAllowanceRef = useRef(0);
  const location = useLocation();
  const prevPathRef = useRef(location.pathname);

  useEffect(() => {
    const currentPath = location.pathname;
    const prevPath = prevPathRef.current;
    prevPathRef.current = currentPath;

    if (prevPath !== currentPath && notification) {
      if (navAllowanceRef.current > 0) {
        navAllowanceRef.current--;
      } else {
        setNotificationState(null);
        navAllowanceRef.current = 0;
      }
    }
  }, [location.pathname, notification]);

  const setNotification = useCallback((n: GovUKNotification | null) => {
    setNotificationState(n);
    if (n !== null) {
      setNotificationKey((prev) => prev + 1);
      navAllowanceRef.current = n.type === 'success' ? 1 : 0;
    } else {
      navAllowanceRef.current = 0;
    }
  }, []);

  return (
    <GovUKNotificationContext.Provider value={{ notification, notificationKey, setNotification }}>
      {children}
    </GovUKNotificationContext.Provider>
  );
}

export function useGovUKNotification(): { setNotification: (n: GovUKNotification | null) => void } {
  const context = useContext(GovUKNotificationContext);
  if (!context) {
    throw new Error('useGovUKNotification must be used within GovUKNotificationProvider');
  }
  return { setNotification: context.setNotification };
}

export function GovUKNotificationDisplay(): React.ReactElement | null {
  const context = useContext(GovUKNotificationContext);

  if (!context?.notification) return null;

  const { notification, notificationKey } = context;

  return (
    <GovUKNotificationBanner key={notificationKey} type={notification.type} title={notification.title}>
      {notification.message ? <p className="govuk-body">{notification.message}</p> : null}
    </GovUKNotificationBanner>
  );
}
