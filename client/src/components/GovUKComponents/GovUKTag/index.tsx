import React from 'react';
import './styles.css';

export type TagColour = 
  | 'grey' 
  | 'green' 
  | 'turquoise' 
  | 'blue' 
  | 'light-blue' 
  | 'purple' 
  | 'pink' 
  | 'red' 
  | 'orange' 
  | 'yellow'
  | 'white';

interface GovUKTagProps {
  children: React.ReactNode;
  colour?: TagColour;
  className?: string;
}

function GovUKTag({ children, colour, className = '' }: GovUKTagProps): React.ReactElement {
  const colourClass = colour ? `govuk-tag--${colour}` : '';
  
  return (
    <strong className={`govuk-tag ${colourClass} ${className}`.trim()}>
      {children}
    </strong>
  );
}

export default GovUKTag;
