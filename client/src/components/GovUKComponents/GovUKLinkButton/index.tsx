import React from 'react';
import './styles.css';

export type GovUKLinkButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement>;

/**
 * A button for JavaScript actions that should use the GOV.UK link appearance.
 * Using a real button preserves keyboard and assistive-technology semantics
 * without relying on an invalid placeholder href.
 */
const GovUKLinkButton = ({ className = '', style, type = 'button', ...props }: GovUKLinkButtonProps): React.ReactElement => (
  <button
    {...props}
    type={type}
    className={`govuk-link govuk-link-button${className ? ` ${className}` : ''}`}
    style={style}
  />
);

export default GovUKLinkButton;
