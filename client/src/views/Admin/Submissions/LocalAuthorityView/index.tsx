import React from 'react';
import { useMutation, useQuery } from 'react-query';
import { useParams } from 'react-router-dom';

import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, SummaryList, useGovUKNotification } from '../../../../components/GovUKComponents';
import { getLocalAuthoritySubmission, getStatusLabel, formatDateTime, downloadSubmission, getDownloadFileName, getDownloadMimeType, DownloadFormat } from './queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import './styles.css';

function LocalAuthorityView() {
  const { submissionId, localAuthorityId } = useParams<{
    submissionId: string;
    localAuthorityId: string;
  }>();
  const { setNotification } = useGovUKNotification();

  const { data, isLoading, error } = useQuery({
    queryKey: ['local-authority-submission', submissionId, localAuthorityId],
    queryFn: () => getLocalAuthoritySubmission(submissionId!, localAuthorityId!),
    enabled: !!submissionId && !!localAuthorityId,
  });

  const { mutateAsync: downloadMutation, isLoading: downloading } = useMutation({
    mutationKey: ['download-submission', submissionId, localAuthorityId],
    mutationFn: (format: DownloadFormat) => downloadSubmission(submissionId!, localAuthorityId!, format),
    onSuccess: (response, format) => {
      const blob = new Blob([response.data], { type: getDownloadMimeType(format) });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = getDownloadFileName(submissionId!, localAuthorityId!, format);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
    onError: (err) => {
      processError(err, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleDownload = (format: DownloadFormat) => {
    downloadMutation(format);
  };

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner />
      </GeneralLayout>
    );
  }

  if (error || !data?.data) {
    return (
      <GeneralLayout>
        <p className="govuk-body">Failed to load local authority submission details.</p>
      </GeneralLayout>
    );
  }

  const submission = data.data;

  const breadcrumbItems = [
    { label: 'Home', link: '/admin/home' },
    { label: 'Submissions', link: '/admin/submissions' },
    { label: submission.dataCollectionName, link: `/admin/submissions/${submissionId}` },
  ];

  const detailsItems = [
    {
      label: 'Status',
      value: getStatusLabel(submission.status),
    },
    ...(submission.submittedBy
      ? [
          {
            label: 'Submitted by',
            value: (
              <span>
                {submission.submittedBy}
                {submission.submittedByEmail && (
                  <>
                    {'     '}
                    <a href={`mailto:${submission.submittedByEmail}`} className="govuk-link">
                      {submission.submittedByEmail}
                    </a>
                  </>
                )}
              </span>
            ),
          },
        ]
      : []),
    ...(submission.submittedAt
      ? [
          {
            label: 'Submitted on',
            value: formatDateTime(submission.submittedAt),
          },
        ]
      : []),
    ...(submission.contact
      ? [
          {
            label: 'Contact',
            value: (
              <>
                {submission.contact.organisationName}
                {submission.contact.addressLine1 && (
                  <>
                    <br />
                    {submission.contact.addressLine1}
                  </>
                )}
                {submission.contact.addressLine2 && (
                  <>
                    <br />
                    {submission.contact.addressLine2}
                  </>
                )}
                {submission.contact.city && (
                  <>
                    <br />
                    {submission.contact.city}
                  </>
                )}
                {submission.contact.postcode && (
                  <>
                    <br />
                    {submission.contact.postcode}
                  </>
                )}
                {submission.contact.country && (
                  <>
                    <br />
                    {submission.contact.country}
                  </>
                )}
                {submission.contact.phone && (
                  <>
                    <br />
                    <br />
                    {submission.contact.phone}
                  </>
                )}
                {submission.contact.email && (
                  <>
                    <br />
                    <a href={`mailto:${submission.contact.email}`} className="govuk-link">
                      {submission.contact.email}
                    </a>
                  </>
                )}
              </>
            ),
          },
        ]
      : []),
  ];

  return (
    <GeneralLayout breadcrumbs={breadcrumbItems} currentPage={submission.localAuthorityName}>
      <span className="govuk-caption-xl">{submission.dataCollectionName}</span>

      <p className="govuk-body-l">View the local authority's submission details, including its status and contact information. Download the submitted data for further analysis.</p>

      <h2 className="govuk-heading-m">Details</h2>
      <SummaryList items={detailsItems} />

      {submission.files && submission.files.length > 0 && (
        <>
          <h2 className="govuk-heading-m govuk-!-margin-top-6">Files</h2>
          {submission.files.map((file: (typeof submission.files)[number], index: number) => (
            <div key={index} className="submission-files__item govuk-!-margin-bottom-6">
              <div className="submission-files__icon">
                <svg width="109" height="150" viewBox="0 0 109 150" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <rect width="109" height="150" fill="#F3F2F1" />
                  <g filter="url(#filter0_d_109_16939)">
                    <rect width="99" height="140" transform="translate(5 5)" fill="white" />
                    <path d="M17 17H92V44H17V17ZM17 64H35.75V127H17V64ZM72 66V125H56V66H72ZM74 64H54V127H74V64Z" fill="#A8ABAD" />
                    <path d="M54 66.05V125H37.8V66.05H54ZM56 64.05H35.75V127.05H56V64V64.05ZM90 66.05V125H74.05V66.05H90ZM92 64.05H72V127.05H92V64V64.05Z" fill="#A8ABAD" />
                  </g>
                  <defs>
                    <filter id="filter0_d_109_16939" x="3" y="5" width="103" height="144" filterUnits="userSpaceOnUse" colorInterpolationFilters="sRGB">
                      <feFlood floodOpacity="0" result="BackgroundImageFix" />
                      <feColorMatrix in="SourceAlpha" type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0" result="hardAlpha" />
                      <feOffset dy="2" />
                      <feGaussianBlur stdDeviation="1" />
                      <feColorMatrix type="matrix" values="0 0 0 0 0.694118 0 0 0 0 0.705882 0 0 0 0 0.713726 0 0 0 1 0" />
                      <feBlend mode="normal" in2="BackgroundImageFix" result="effect1_dropShadow_109_16939" />
                      <feBlend mode="normal" in="SourceGraphic" in2="effect1_dropShadow_109_16939" result="shape" />
                    </filter>
                  </defs>
                </svg>
              </div>
              <div className="submission-files__content">
                <p className="govuk-body govuk-!-margin-bottom-1">
                  <strong>{file.name}</strong>
                  <br />
                  <span className="govuk-body-s">{file.description}</span>
                </p>
                <div className="submission-files__downloads">
                  {file.csvUrl && (
                    <button type="button" className="govuk-link submission-files__download-button" onClick={() => handleDownload('csv')} disabled={downloading}>
                      {downloading ? 'Downloading...' : `Download in CSV ${file.csvSize ? `(${file.csvSize})` : ''}`}
                    </button>
                  )}
                  {file.jsonUrl && (
                    <button type="button" className="govuk-link submission-files__download-button" onClick={() => handleDownload('json')} disabled={downloading}>
                      {downloading ? 'Downloading...' : `Download in JSON ${file.jsonSize ? `(${file.jsonSize})` : ''}`}
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </>
      )}
    </GeneralLayout>
  );
}

export default LocalAuthorityView;
