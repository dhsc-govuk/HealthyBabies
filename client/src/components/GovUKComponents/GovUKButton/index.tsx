import React from 'react';
import './styles.css';

interface GovUKButtonProps {
  children: React.ReactNode;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  disabled?: boolean;
  isLoading?: boolean;
  start?: boolean;
  type?: 'button' | 'submit' | 'reset';
  className?: string;
  style?: React.CSSProperties;
}

function GovUKButton({ children, onClick, disabled = false, isLoading = false, start = false, type = 'button', className = 'govuk-button', style }: GovUKButtonProps): React.ReactElement {
  const buttonClass = [className, start && 'govuk-button--start'].filter(Boolean).join(' ');
  return (
    <button type={type} className={buttonClass} onClick={onClick} disabled={disabled || isLoading} aria-busy={isLoading} data-module="govuk-button" style={style}>
      <span style={{ visibility: isLoading ? 'hidden' : undefined }}>{children}</span>
      {isLoading && (
        <span className="govuk-button__spinner-wrapper" aria-hidden="true">
          <span className="govuk-button__spinner" />
        </span>
      )}
    </button>
  );
}

export default GovUKButton;
