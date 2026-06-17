import React from 'react';
import GovUKInputWithSuffix from '../GovUKInputWithSuffix';
import './styles.css';

interface GroupedInputItem {
  code: string;
  label: string;
  value: string;
  suffix?: string;
}

interface GovUKGroupedInputsProps {
  legend: string;
  hint?: string;
  questionCode?: string;
  items: GroupedInputItem[];
  onChange: (code: string, value: string) => void;
}

const GovUKGroupedInputs: React.FC<GovUKGroupedInputsProps> = ({
  legend,
  hint,
  questionCode,
  items,
  onChange,
}) => {
  return (
    <div className="govuk-grouped-inputs">
      <fieldset className="govuk-fieldset">
        <legend className="govuk-fieldset__legend govuk-fieldset__legend--s">
          <div className="govuk-grouped-inputs__header">
            <span className="govuk-grouped-inputs__legend">{legend}</span>
            {questionCode && (
              <span className="govuk-grouped-inputs__code">{questionCode}</span>
            )}
          </div>
        </legend>
        {hint && (
          <div className="govuk-hint">{hint}</div>
        )}
        <hr className="govuk-section-break govuk-section-break--visible govuk-!-margin-top-2 govuk-!-margin-bottom-3" />
        <div className="govuk-grouped-inputs__items">
          {items.map((item) => (
            <div key={item.code} className="govuk-grouped-inputs__item">
              <span className="govuk-grouped-inputs__item-label">— {item.label}</span>
              <div className="govuk-grouped-inputs__item-input">
                <GovUKInputWithSuffix
                  id={item.code}
                  name={item.code}
                  label=""
                  suffix={item.suffix}
                  value={item.value}
                  width="5"
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => onChange(item.code, e.target.value)}
                />
              </div>
            </div>
          ))}
        </div>
      </fieldset>
    </div>
  );
};

export default GovUKGroupedInputs;
