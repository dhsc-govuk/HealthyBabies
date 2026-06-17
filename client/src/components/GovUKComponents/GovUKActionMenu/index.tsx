import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import './styles.css';

export interface ActionMenuItem {
  label: string;
  href?: string;
  onClick?: () => void | Promise<void>;
  dividerBefore?: boolean;
}

export interface GovUKActionMenuProps {
  items: ActionMenuItem[];
  buttonLabel?: string;
}

const GovUKActionMenu = ({
  items,
  buttonLabel = 'Actions',
}: GovUKActionMenuProps): React.ReactElement => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleItemClick = async (item: ActionMenuItem) => {
    setIsOpen(false);
    if (item.onClick) {
      await item.onClick();
    } else if (item.href) {
      navigate(item.href);
    }
  };

  return (
    <div className="govuk-action-menu" ref={containerRef}>
      <button
        type="button"
        className="govuk-button govuk-button--secondary govuk-action-menu__button"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        {buttonLabel}
        <span className={`govuk-action-menu__chevron ${isOpen ? 'govuk-action-menu__chevron--open' : ''}`} aria-hidden="true">
          <svg width="12" height="7" viewBox="0 0 12 7" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
            <path d="M6 7L12 0H0L6 7Z" />
          </svg>
        </span>
      </button>
      {isOpen && (
        <ul className="govuk-action-menu__list" role="menu">
          {items.map((item, index) => (
            <React.Fragment key={index}>
              {item.dividerBefore && (
                <li className="govuk-action-menu__divider" role="separator" />
              )}
              <li role="none">
                <button
                  type="button"
                  className="govuk-action-menu__item"
                  onClick={() => handleItemClick(item)}
                  role="menuitem"
                >
                  {item.label}
                </button>
              </li>
            </React.Fragment>
          ))}
        </ul>
      )}
    </div>
  );
};

export default GovUKActionMenu;
