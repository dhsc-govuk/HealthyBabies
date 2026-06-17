import React, { useState, useEffect } from 'react';

interface GovUKNotificationBannerProps {
  type?: 'success' | 'important';
  title: string;
  children: React.ReactNode;
  autoDismiss?: boolean;
  autoDismissTimeout?: number;
}

function GovUKNotificationBanner({
  type = 'important',
  title,
  children,
  autoDismiss = false,
  autoDismissTimeout = 5000,
}: GovUKNotificationBannerProps): React.ReactElement | null {
  const [isVisible, setIsVisible] = useState(true);
  const isSuccess = type === 'success';

  useEffect(() => {
    if (autoDismiss) {
      const timer = setTimeout(() => {
        setIsVisible(false);
      }, autoDismissTimeout);

      return () => clearTimeout(timer);
    }
  }, [autoDismiss, autoDismissTimeout]);

  if (!isVisible) {
    return null;
  }

  return (
    <div
      className={`govuk-notification-banner ${isSuccess ? 'govuk-notification-banner--success' : ''}`}
      role={isSuccess ? 'alert' : 'region'}
      aria-labelledby="govuk-notification-banner-title"
      data-module="govuk-notification-banner"
    >
      <div className="govuk-notification-banner__header">
        <h2 className="govuk-notification-banner__title" id="govuk-notification-banner-title">
          {isSuccess ? 'Success' : title}
        </h2>
      </div>
      <div className="govuk-notification-banner__content">
        {isSuccess && <h3 className="govuk-notification-banner__heading">{title}</h3>}
        {children}
      </div>
    </div>
  );
}

export default GovUKNotificationBanner;
