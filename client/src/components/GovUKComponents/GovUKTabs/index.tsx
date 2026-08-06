import React, { useState } from 'react';

export interface GovUKTab {
  id: string;
  label: string;
  panel: React.ReactNode;
}

export interface GovUKTabsProps {
  tabs: GovUKTab[];
  title?: string;
  /** id of the tab selected on first render; defaults to the first tab */
  defaultTabId?: string;
}

/**
 * GOV.UK Design System tabs, controlled by React state so panels switch without
 * navigation. All panels stay mounted (hidden via the GOV.UK class) so chart and
 * filter state is preserved when switching tabs.
 */
const GovUKTabs = ({ tabs, title = 'Contents', defaultTabId }: GovUKTabsProps): React.ReactElement => {
  const [selectedId, setSelectedId] = useState<string>(defaultTabId ?? tabs[0]?.id);

  return (
    <div className="govuk-tabs">
      <h2 className="govuk-tabs__title">{title}</h2>
      <ul className="govuk-tabs__list" role="tablist">
        {tabs.map((tab) => {
          const selected = tab.id === selectedId;
          return (
            <li key={tab.id} className={`govuk-tabs__list-item${selected ? ' govuk-tabs__list-item--selected' : ''}`} role="presentation">
              <a
                className="govuk-tabs__tab"
                href={`#${tab.id}`}
                role="tab"
                aria-selected={selected}
                aria-controls={tab.id}
                onClick={(e) => {
                  e.preventDefault();
                  setSelectedId(tab.id);
                }}>
                {tab.label}
              </a>
            </li>
          );
        })}
      </ul>
      {tabs.map((tab) => (
        <div
          key={tab.id}
          className={`govuk-tabs__panel${tab.id === selectedId ? '' : ' govuk-tabs__panel--hidden'}`}
          id={tab.id}
          role="tabpanel">
          {tab.panel}
        </div>
      ))}
    </div>
  );
};

export default GovUKTabs;
