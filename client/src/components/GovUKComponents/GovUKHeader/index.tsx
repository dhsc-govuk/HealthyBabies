import React from 'react';
import { GovUKHeaderLogo } from '../../Logos';

interface GovUKHeaderProps {
  serviceName?: string;
  serviceUrl?: string;
  navigation?: Array<{
    href: string;
    text: string;
    active?: boolean;
  }>;
  children?: React.ReactNode;
}

function GovUKHeader({ 
  serviceName, 
  serviceUrl = '/', 
  navigation,
  children 
}: GovUKHeaderProps): React.ReactElement {
  return (
    <header className="govuk-header" data-module="govuk-header">
      <div className="govuk-header__container govuk-width-container">
        <div className="govuk-header__logo">
          <a href="https://www.gov.uk" className="govuk-header__link govuk-header__link--homepage">
              <GovUKHeaderLogo />
            </a>
        </div>
        {serviceName && (
          <div className="govuk-header__content">
            <a href={serviceUrl} className="govuk-header__link govuk-header__service-name">
              {serviceName}
            </a>
            {navigation && navigation.length > 0 && (
              <nav aria-label="Menu" className="govuk-header__navigation">
                <ul className="govuk-header__navigation-list">
                  {navigation.map((item, index) => (
                    <li 
                      key={index} 
                      className={`govuk-header__navigation-item${item.active ? ' govuk-header__navigation-item--active' : ''}`}
                    >
                      <a className="govuk-header__link" href={item.href}>
                        {item.text}
                      </a>
                    </li>
                  ))}
                </ul>
              </nav>
            )}
          </div>
        )}
        {children}
      </div>
    </header>
  );
}

export default GovUKHeader;
