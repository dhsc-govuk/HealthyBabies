import React, { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import {
  SummaryList,
  GovUKButton,
  GovUKTag,
  GovUKWarningText,
  useGovUKNotification,
} from '../../../../components/GovUKComponents';
import {
  getSubmissionById,
  getSubmissionStatus,
  getLocalAuthorityStatusLabel,
  getLocalAuthorityStatusTagColour,
  LocalAuthoritySubmission,
  LocalAuthorityFilterStatus,
  downloadConsolidatedSubmission,
  getConsolidatedDownloadFileName,
  getConsolidatedDownloadMimeType,
  DownloadFormat,
} from './queries';
import { processError } from '../../../../helpers/axiosErrorFallback';
import './styles.css';

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

function SubmissionView(): React.ReactElement {
  const { submissionId } = useParams<{ submissionId: string }>();
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();
  const [searchQuery, setSearchQuery] = useState('');
  const [sortOrder, setSortOrder] = useState<'alphabetically' | 'status'>('alphabetically');
  const [statusFilter, setStatusFilter] = useState<LocalAuthorityFilterStatus>('all');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['submission-view', submissionId],
    queryFn: () => getSubmissionById(submissionId!),
    enabled: !!submissionId,
  });

  const { mutateAsync: downloadMutation, isLoading: downloading } = useMutation({
    mutationKey: ['download-consolidated', submissionId],
    mutationFn: (format: DownloadFormat) => downloadConsolidatedSubmission(submissionId!, format),
    onSuccess: (response, format) => {
      const blob = new Blob([response.data], { type: getConsolidatedDownloadMimeType(format) });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = getConsolidatedDownloadFileName(submissionId!, format);
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

  const submission = data?.data;
  const status = submission ? getSubmissionStatus(submission) : null;
  const isOpen = status === 'Open';

  const filteredAndSortedLAs = useMemo(() => {
    if (!submission?.localAuthoritySubmissions) return [];

    let filtered = submission.localAuthoritySubmissions;

    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter((la) =>
        la.localAuthorityName.toLowerCase().includes(query)
      );
    }

    if (statusFilter !== 'all') {
      const statusMap: Record<LocalAuthorityFilterStatus, LocalAuthoritySubmission['status'] | null> = {
        all: null,
        submitted: 'Submitted',
        approved: 'Approved',
        'in-progress': 'InProgress',
        'not-started': 'NotStarted',
        rejected: 'Rejected',
        'requires-changes': 'RequiresChanges',
      };
      const targetStatus = statusMap[statusFilter];
      if (targetStatus) {
        filtered = filtered.filter((la) => la.status === targetStatus);
      }
    }

    const sorted = [...filtered];
    if (sortOrder === 'alphabetically') {
      sorted.sort((a, b) => a.localAuthorityName.localeCompare(b.localAuthorityName));
    } else if (sortOrder === 'status') {
      const statusOrder = ['Rejected', 'RequiresChanges', 'InProgress', 'NotStarted', 'Submitted', 'Approved'];
      sorted.sort((a, b) => statusOrder.indexOf(a.status) - statusOrder.indexOf(b.status));
    }

    return sorted;
  }, [submission?.localAuthoritySubmissions, searchQuery, sortOrder, statusFilter]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
  };

  if (isError) {
    return (
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Submissions', link: '/admin/submissions' },
        ]}
        currentPage="Error"
      >
        <p className="govuk-body">Unable to load submission.</p>
        <GovUKButton onClick={() => navigate('/admin/submissions')}>
          Back to submissions
        </GovUKButton>
      </GeneralLayout>
    );
  }

  const detailsItems = submission
    ? [
        {
          label: 'Status',
          value: status || '',
          onAction: () => navigate(`/admin/submissions/${submissionId}/change-status`),
          actionLabel: 'Change',
        },
        {
          label: 'Start date',
          value: formatDate(submission.startDate),
        },
        {
          label: 'Due date',
          value: formatDate(submission.endDate),
          onAction: () => navigate(`/admin/submissions/${submissionId}/change-due-date`),
          actionLabel: 'Change',
        },
      ]
    : [];

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Submissions', link: '/admin/submissions' },
        { label: submission?.name || 'Submission', link: '' },
      ]}
    >
      <LoadingBox loading={isLoading}>
        {submission && (
          <>
            <div className="govuk-grid-row">
              <div className="govuk-grid-column-two-thirds">
                <p className="govuk-caption-l">Submission</p>
                <h1 className="govuk-heading-l">{submission.name}</h1>
                <p className="govuk-body">
                  Manage the submission timeline and status, and track reporting progress
                  across all local authorities. Download automatically consolidated data, and
                  access each local authority's data individually.
                </p>

                <h2 className="govuk-heading-m">Details</h2>
                <SummaryList items={detailsItems} noOuterBorder />

                <h2 className="govuk-heading-m govuk-!-margin-top-8">Files</h2>
                <div className="submission-files__item govuk-!-margin-bottom-6">
                    <div className="submission-files__icon">
                      <svg width="109" height="150" viewBox="0 0 109 150" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <rect width="109" height="150" fill="#F3F2F1"/>
                        <g filter="url(#filter0_d_109_16939)">
                          <rect width="99" height="140" transform="translate(5 5)" fill="white"/>
                          <path d="M17 17H92V44H17V17ZM17 64H35.75V127H17V64ZM72 66V125H56V66H72ZM74 64H54V127H74V64Z" fill="#A8ABAD"/>
                          <path d="M54 66.05V125H37.8V66.05H54ZM56 64.05H35.75V127.05H56V64V64.05ZM90 66.05V125H74.05V66.05H90ZM92 64.05H72V127.05H92V64V64.05Z" fill="#A8ABAD"/>
                        </g>
                        <defs>
                          <filter id="filter0_d_109_16939" x="3" y="5" width="103" height="144" filterUnits="userSpaceOnUse" colorInterpolationFilters="sRGB">
                            <feFlood floodOpacity="0" result="BackgroundImageFix"/>
                            <feColorMatrix in="SourceAlpha" type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0" result="hardAlpha"/>
                            <feOffset dy="2"/>
                            <feGaussianBlur stdDeviation="1"/>
                            <feColorMatrix type="matrix" values="0 0 0 0 0.694118 0 0 0 0 0.705882 0 0 0 0 0.713726 0 0 0 1 0"/>
                            <feBlend mode="normal" in2="BackgroundImageFix" result="effect1_dropShadow_109_16939"/>
                            <feBlend mode="normal" in="SourceGraphic" in2="effect1_dropShadow_109_16939" result="shape"/>
                          </filter>
                        </defs>
                      </svg>
                    </div>
                    <div className="submission-files__content">
                      <p className="govuk-body govuk-!-margin-bottom-1">
                        <strong>Consolidated Management Information data</strong>
                        <br />
                        <span className="govuk-body-s">for {submission.name.split(' ').slice(0, 2).join(' ')}</span>
                      </p>
                      {isOpen && (
                        <GovUKWarningText>
                          This file may contain incomplete data because the submission is still open.
                        </GovUKWarningText>
                      )}
                      <div className="submission-files__downloads">
                        <button
                          type="button"
                          className="govuk-link submission-files__download-button"
                          onClick={() => handleDownload('csv')}
                          disabled={downloading}
                        >
                          {downloading ? 'Downloading...' : 'Download in CSV'}
                        </button>
                        <button
                          type="button"
                          className="govuk-link submission-files__download-button"
                          onClick={() => handleDownload('json')}
                          disabled={downloading}
                        >
                          {downloading ? 'Downloading...' : 'Download in JSON'}
                        </button>
                      </div>
                    </div>
                </div>
              </div>
            </div>

            <h2 className="govuk-heading-m govuk-!-margin-top-8">Local authorities</h2>
            
            <form onSubmit={handleSearch} className="submission-la-filters govuk-!-margin-bottom-6">
              <div className="submission-la-filters__row">
                <div className="submission-la-filters__search">
                  <label className="govuk-label" htmlFor="la-search">
                    Search Local Authority by name
                  </label>
                  <div className="submission-la-filters__search-row">
                    <input
                      type="text"
                      id="la-search"
                      className="govuk-input"
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                    />
                    <GovUKButton type="submit">Search</GovUKButton>
                  </div>
                </div>

                <div className="submission-la-filters__sort">
                  <label className="govuk-label" htmlFor="la-sort">
                    Sort list
                  </label>
                  <select
                    id="la-sort"
                    className="govuk-select"
                    value={sortOrder}
                    onChange={(e) => setSortOrder(e.target.value as 'alphabetically' | 'status')}
                  >
                    <option value="alphabetically">Alphabetically</option>
                    <option value="status">By status</option>
                  </select>
                </div>

                <div className="submission-la-filters__filter">
                  <label className="govuk-label" htmlFor="la-status-filter">
                    Filter by status
                  </label>
                  <select
                    id="la-status-filter"
                    className="govuk-select"
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value as LocalAuthorityFilterStatus)}
                  >
                    <option value="all">Select status...</option>
                    <option value="submitted">Submitted</option>
                    <option value="approved">Approved</option>
                    <option value="in-progress">In progress</option>
                    <option value="not-started">Not started</option>
                    <option value="rejected">Rejected</option>
                    <option value="requires-changes">Requires changes</option>
                  </select>
                </div>
              </div>
            </form>

            <ul className="submission-la-list">
              {filteredAndSortedLAs.map((la) => {
                const tagColour = getLocalAuthorityStatusTagColour(la.status);
                const statusLabel = getLocalAuthorityStatusLabel(la.status);

                return (
                  <li key={la.localAuthorityId} className="submission-la-list__item">
                    <div className="submission-la-list__name">
                      <a
                        href={`/admin/submissions/${submissionId}/local-authority/${la.localAuthorityId}`}
                        className="govuk-link"
                        onClick={(e) => {
                          e.preventDefault();
                          navigate(`/admin/submissions/${submissionId}/local-authority/${la.localAuthorityId}`);
                        }}
                      >
                        {la.localAuthorityName}
                      </a>
                      {la.submittedBy && la.submittedAt && (
                        <p className="govuk-body-s govuk-hint govuk-!-margin-top-1 govuk-!-margin-bottom-0">
                          Submitted by {la.submittedBy} on {formatDate(la.submittedAt)}
                        </p>
                      )}
                    </div>
                    <div className="submission-la-list__status">
                      {tagColour ? (
                        <GovUKTag colour={tagColour}>{statusLabel}</GovUKTag>
                      ) : (
                        <span className="govuk-body">{statusLabel}</span>
                      )}
                    </div>
                  </li>
                );
              })}
              {filteredAndSortedLAs.length === 0 && (
                <li className="submission-la-list__item submission-la-list__item--empty">
                  <p className="govuk-body">No local authorities found.</p>
                </li>
              )}
            </ul>
          </>
        )}
      </LoadingBox>
    </GeneralLayout>
  );
}

export default SubmissionView;
