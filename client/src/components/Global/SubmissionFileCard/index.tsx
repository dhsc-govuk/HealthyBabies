import React from 'react';
import './styles.css';

export interface SubmissionFileCardDownload {
  label: string;
  onClick: () => void;
  disabled?: boolean;
}

interface SubmissionFileCardProps {
  name: string;
  subtitle?: React.ReactNode;
  downloads: SubmissionFileCardDownload[];
}

/**
 * The "Files" card shown on a submission's summary page - a document icon next to the file name,
 * an optional subtitle (typically "Submitted by X on Y"), and one download link per format.
 * Shared by the admin's local authority view and the local authority's own submission/version
 * summary pages, so the icon and layout stay identical everywhere a submission can be downloaded.
 */
export function SubmissionFileCard({ name, subtitle, downloads }: SubmissionFileCardProps): React.ReactElement {
  return (
    <div className="submission-files__item govuk-!-margin-bottom-6">
      <div className="submission-files__icon">
        <svg width="109" height="150" viewBox="0 0 109 150" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect width="109" height="150" fill="#F3F2F1" />
          <g filter="url(#filter0_d_submission_file_card)">
            <rect width="99" height="140" transform="translate(5 5)" fill="white" />
            <path d="M17 17H92V44H17V17ZM17 64H35.75V127H17V64ZM72 66V125H56V66H72ZM74 64H54V127H74V64Z" fill="#A8ABAD" />
            <path
              d="M54 66.05V125H37.8V66.05H54ZM56 64.05H35.75V127.05H56V64V64.05ZM90 66.05V125H74.05V66.05H90ZM92 64.05H72V127.05H92V64V64.05Z"
              fill="#A8ABAD"
            />
          </g>
          <defs>
            <filter id="filter0_d_submission_file_card" x="3" y="5" width="103" height="144" filterUnits="userSpaceOnUse" colorInterpolationFilters="sRGB">
              <feFlood floodOpacity="0" result="BackgroundImageFix" />
              <feColorMatrix in="SourceAlpha" type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0" result="hardAlpha" />
              <feOffset dy="2" />
              <feGaussianBlur stdDeviation="1" />
              <feColorMatrix type="matrix" values="0 0 0 0 0.694118 0 0 0 0 0.705882 0 0 0 0 0.713726 0 0 0 1 0" />
              <feBlend mode="normal" in2="BackgroundImageFix" result="effect1_dropShadow_submission_file_card" />
              <feBlend mode="normal" in="SourceGraphic" in2="effect1_dropShadow_submission_file_card" result="shape" />
            </filter>
          </defs>
        </svg>
      </div>
      <div className="submission-files__content">
        <p className="govuk-body-l govuk-!-margin-bottom-0">{name}</p>
        {subtitle && <p className="govuk-hint govuk-!-margin-bottom-0">{subtitle}</p>}
        <div className="submission-files__downloads govuk-!-margin-top-2">
          {downloads.map((download) => (
            <button
              key={download.label}
              type="button"
              className="govuk-link submission-files__download-button"
              onClick={download.onClick}
              disabled={download.disabled}>
              {download.label}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
