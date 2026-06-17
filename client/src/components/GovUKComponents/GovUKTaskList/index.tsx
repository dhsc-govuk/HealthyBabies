import React from 'react';
import type { TagColour } from '../GovUKTag';
import GovUKTag from '../GovUKTag';
import './styles.css';

export interface TaskListItemStatus {
  text: string;
  tag?: {
    text: string;
    colour?: TagColour;
  };
}

export interface TaskListItem {
  id: string;
  title: string;
  hint?: string;
  href?: string;
  status: TaskListItemStatus;
  onClick?: () => void;
}

interface GovUKTaskListProps {
  items: TaskListItem[];
  idPrefix?: string;
}

function GovUKTaskList({ items, idPrefix = 'task-list' }: GovUKTaskListProps): React.ReactElement {
  return (
    <ul className="govuk-task-list">
      {items.map((item, index) => {
        const statusId = `${idPrefix}-${index}-status`;
        const hintId = item.hint ? `${idPrefix}-${index}-hint` : undefined;
        const hasLink = !!item.href || !!item.onClick;
        const ariaDescribedBy = [hintId, statusId].filter(Boolean).join(' ');

        return (
          <li
            key={item.id}
            className={`govuk-task-list__item${hasLink ? ' govuk-task-list__item--with-link' : ''}`}
          >
            <div className="govuk-task-list__name-and-hint">
              {hasLink ? (
                <a
                  className="govuk-link govuk-task-list__link"
                  href={item.href || '#'}
                  aria-describedby={ariaDescribedBy}
                  onClick={(e) => {
                    if (item.onClick) {
                      e.preventDefault();
                      item.onClick();
                    }
                  }}
                >
                  {item.title}
                </a>
              ) : (
                <span aria-describedby={ariaDescribedBy}>{item.title}</span>
              )}
              {item.hint && (
                <div id={hintId} className="govuk-task-list__hint">
                  {item.hint}
                </div>
              )}
            </div>
            <div className="govuk-task-list__status" id={statusId}>
              {item.status.tag ? (
                <GovUKTag colour={item.status.tag.colour}>
                  {item.status.tag.text}
                </GovUKTag>
              ) : (
                item.status.text
              )}
            </div>
          </li>
        );
      })}
    </ul>
  );
}

export default GovUKTaskList;
