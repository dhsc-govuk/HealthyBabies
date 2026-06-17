import React from 'react';
import './styles.css';

interface FormAnswerItem {
  label: string;
  value: string | React.ReactNode;
  isSubAnswer?: boolean;
}

interface FormAnswersViewProps {
  sectionCaption: string;
  title: string;
  introText: React.ReactNode;
  dataCollectionTitle: string;
  dateRange: string;
  items: FormAnswerItem[];
  onChangeClick: () => void;
  onDeleteClick: () => void;
}

function FormAnswersView({
  sectionCaption,
  title,
  introText,
  dataCollectionTitle,
  dateRange,
  items,
  onChangeClick,
  onDeleteClick,
}: FormAnswersViewProps): React.ReactElement {
  return (
    <div className="form-answers-view">
      <span className="govuk-caption-l">{sectionCaption}</span>
      <h1 className="govuk-heading-l">{title}</h1>

      <div className="form-answers-view__intro">
        {introText}
      </div>

      <div className="form-answers-view__container">
        <div className="form-answers-view__header">
          <div className="form-answers-view__header-info">
            <strong>{dataCollectionTitle}</strong>, {dateRange}
          </div>
          <div className="form-answers-view__header-actions">
            <button
              type="button"
              className="govuk-link form-answers-view__action-link"
              onClick={onChangeClick}
            >
              Change
            </button>
            <span className="form-answers-view__action-divider">|</span>
            <button
              type="button"
              className="govuk-link form-answers-view__action-link"
              onClick={onDeleteClick}
            >
              Delete
            </button>
          </div>
        </div>

        <table className="govuk-table form-answers-view__table">
          <tbody className="govuk-table__body">
            {items.map((item, index, arr) => (
              <tr key={index} className="govuk-table__row">
                <th
                  scope="row"
                  className="govuk-table__header form-answers-view__label"
                  style={{ 
                    fontWeight: item.isSubAnswer ? 400 : 700,
                    borderBottom: index === arr.length - 1 ? 'none' : undefined
                  }}
                >
                  {item.isSubAnswer ? `— ${item.label}` : item.label}
                </th>
                <td 
                  className="govuk-table__cell form-answers-view__value"
                  style={{ borderBottom: index === arr.length - 1 ? 'none' : undefined }}
                >
                  {item.value}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default FormAnswersView;
export type { FormAnswerItem, FormAnswersViewProps };
