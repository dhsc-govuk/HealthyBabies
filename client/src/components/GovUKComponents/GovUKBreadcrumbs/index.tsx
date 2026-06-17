import React from 'react';
import { Link } from 'react-router-dom';

interface BreadcrumbItem {
  label: string;
  href?: string;
}

interface GovUKBreadcrumbsProps {
  items: BreadcrumbItem[];
}

function GovUKBreadcrumbs({ items }: GovUKBreadcrumbsProps): React.ReactElement {
  return (
    <nav className="govuk-breadcrumbs" aria-label="Breadcrumb">
      <ol className="govuk-breadcrumbs__list">
        {items.map((item, index) => (
          <li className="govuk-breadcrumbs__list-item" key={index}>
            {item.href ? (
              <Link className="govuk-breadcrumbs__link" to={item.href}>
                {item.label}
              </Link>
            ) : (
              <span aria-current="page">{item.label}</span>
            )}
          </li>
        ))}
      </ol>
    </nav>
  );
}

export default GovUKBreadcrumbs;
