import React from 'react';

/**
 * GOV.UK typography components using native govuk-frontend CSS classes.
 * These replace the govuk-react H1–H4, Paragraph, etc. which inject the
 * legacy "nta" font family via styled-components.
 */

interface HeadingProps extends React.HTMLAttributes<HTMLHeadingElement> {
  children: React.ReactNode;
}

interface TextProps extends React.HTMLAttributes<HTMLElement> {
  children: React.ReactNode;
}

export const H1 = ({ children, className, ...rest }: HeadingProps): React.ReactElement => (
  <h1 className={['govuk-heading-xl', className].filter(Boolean).join(' ')} {...rest}>
    {children}
  </h1>
);

export const H2 = ({ children, className, ...rest }: HeadingProps): React.ReactElement => (
  <h2 className={['govuk-heading-l', className].filter(Boolean).join(' ')} {...rest}>
    {children}
  </h2>
);

export const H3 = ({ children, className, ...rest }: HeadingProps): React.ReactElement => (
  <h3 className={['govuk-heading-m', className].filter(Boolean).join(' ')} {...rest}>
    {children}
  </h3>
);

export const H4 = ({ children, className, ...rest }: HeadingProps): React.ReactElement => (
  <h4 className={['govuk-heading-s', className].filter(Boolean).join(' ')} {...rest}>
    {children}
  </h4>
);

export const Paragraph = ({ children, className, ...rest }: TextProps): React.ReactElement => (
  <p className={['govuk-body', className].filter(Boolean).join(' ')} {...(rest as React.HTMLAttributes<HTMLParagraphElement>)}>
    {children}
  </p>
);

export const LeadParagraph = ({ children, className, ...rest }: TextProps): React.ReactElement => (
  <p className={['govuk-body-l', className].filter(Boolean).join(' ')} {...(rest as React.HTMLAttributes<HTMLParagraphElement>)}>
    {children}
  </p>
);

interface ListProps extends React.HTMLAttributes<HTMLElement> {
  children: React.ReactNode;
}

export const UnorderedList = ({ children, className, ...rest }: ListProps): React.ReactElement => (
  <ul className={['govuk-list', 'govuk-list--bullet', className].filter(Boolean).join(' ')} {...(rest as React.HTMLAttributes<HTMLUListElement>)}>
    {children}
  </ul>
);

export const OrderedList = ({ children, className, ...rest }: ListProps): React.ReactElement => (
  <ol className={['govuk-list', 'govuk-list--number', className].filter(Boolean).join(' ')} {...(rest as React.HTMLAttributes<HTMLOListElement>)}>
    {children}
  </ol>
);

export const ListItem = ({ children, className, ...rest }: ListProps): React.ReactElement => (
  <li className={className} {...(rest as React.HTMLAttributes<HTMLLIElement>)}>
    {children}
  </li>
);
