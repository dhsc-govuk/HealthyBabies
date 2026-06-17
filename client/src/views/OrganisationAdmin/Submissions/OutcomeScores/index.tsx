import React, { useMemo, useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useMutation, useQuery } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKTag, GovUKNotificationBanner } from '../../../../components/GovUKComponents';
import { SpreadsheetIcon } from '../../../../components/Icons';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import {
  getOutcomeScoresModule,
  createOutcomeScoreRecord,
  OutcomeScoreRecordDto,
  downloadBulkUploadTemplate,
  type TemplateFormat,
  getBulkUploadTemplateFileName,
  ServiceRecordRequirementDto,
} from '../queries';
import '../ServiceUsers/styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

const downloadFile = (blob: Blob, filename: string) => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};

const OutcomeScoresList = (): React.ReactElement => {
  usePageTitle('Outcome scores');
  const navigate = useNavigate();
  const location = useLocation();
  const { submissionId, moduleId } = useParams<{ submissionId: string; moduleId: string }>();
  const [searchTerm, setSearchTerm] = useState('');

  const notification = location.state?.notification as
    | {
        type: 'success' | 'error';
        title: string;
        message: string;
      }
    | undefined;
  const bulkUploadSuccess = location.state?.bulkUploadSuccess as
    | {
        successfulRows: number;
        totalRows: number;
      }
    | undefined;
  const [filterService, setFilterService] = useState('');
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest');

  const { data, isLoading } = useQuery({
    queryKey: ['outcome-scores-module', submissionId, moduleId],
    queryFn: () => getOutcomeScoresModule(submissionId!, moduleId!),
    enabled: !!submissionId && !!moduleId,
  });

  const createRecordMutation = useMutation({
    mutationFn: () => createOutcomeScoreRecord(submissionId!, moduleId!),
    onSuccess: (response) => {
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${response.data.recordId}`);
    },
    onError: (error) => {
      console.error('Failed to create record:', error);
    },
  });

  const moduleData = data?.data;

  const filteredAndSortedRecords = useMemo(() => {
    if (!moduleData?.records) return [];

    let filtered = moduleData.records;

    if (searchTerm) {
      const lowerSearch = searchTerm.toLowerCase();
      filtered = filtered.filter((record) => record.anonymisedId.toLowerCase().includes(lowerSearch) || record.serviceName.toLowerCase().includes(lowerSearch));
    }

    if (filterService) {
      filtered = filtered.filter((record) => record.serviceId === filterService);
    }

    return [...filtered].sort((a, b) => {
      if (!a.lastModified || !b.lastModified) return 0;
      const dateA = new Date(a.lastModified).getTime();
      const dateB = new Date(b.lastModified).getTime();
      return sortOrder === 'newest' ? dateB - dateA : dateA - dateB;
    });
  }, [moduleData?.records, searchTerm, filterService, sortOrder]);

  const handleAddRecord = () => {
    createRecordMutation.mutate();
  };

  const handleViewRecord = (recordId: string) => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${recordId}/view`);
  };

  const handleChangeRecord = (recordId: string) => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${recordId}/edit`);
  };

  const handleDeleteRecord = (recordId: string) => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/${recordId}/delete`);
  };

  const handleBack = () => {
    navigate(`/organisation-admin/submissions/${submissionId}`);
  };

  const handleDownloadTemplate = async (format: TemplateFormat = 'csv') => {
    try {
      const blob = await downloadBulkUploadTemplate(submissionId!, moduleId!, format);
      const filename = getBulkUploadTemplateFileName(format, 'outcome_scores');
      downloadFile(blob, filename);
    } catch (error) {
      console.error('Failed to download template:', error);
    }
  };

  const handleBulkUpload = () => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/outcome-scores/bulk-upload`);
  };

  const uniqueServices = useMemo(() => {
    if (!moduleData?.records) return [];
    const serviceMap = new Map<string, string>();
    moduleData.records.forEach((record) => {
      if (record.serviceId && !serviceMap.has(record.serviceId)) {
        serviceMap.set(record.serviceId, record.serviceName);
      }
    });
    return Array.from(serviceMap.entries()).map(([id, name]) => ({ id, name }));
  }, [moduleData?.records]);

  return (
    <LoadingSpinner loading={isLoading} label="Loading outcome scores">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Submissions', link: '/organisation-admin/submissions' },
          { label: moduleData?.submissionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
          { label: 'Outcome scores', link: '#' },
        ]}
        currentPage=""
        backLink={!notification ? { href: '#', onClick: handleBack } : undefined}>
        {notification && (
          <GovUKNotificationBanner type={notification.type === 'success' ? 'success' : 'important'} title={notification.title} autoDismiss autoDismissTimeout={8000}>
            <p className="govuk-body">{notification.message}</p>
          </GovUKNotificationBanner>
        )}

        {bulkUploadSuccess && (
          <GovUKNotificationBanner type="success" title="Bulk upload complete">
            <p className="govuk-body">
              {bulkUploadSuccess.successfulRows} of {bulkUploadSuccess.totalRows} row(s) were successfully uploaded.
            </p>
          </GovUKNotificationBanner>
        )}

        {moduleData && (
          <>
            <span className="govuk-caption-l">Section {moduleData.sectionNumber}</span>
            <h1 className="govuk-heading-l">Outcome scores</h1>

            <h2 className="govuk-heading-m">Tell us about outcome scores for your service users</h2>
            <p className="govuk-body">
              If services delivered by your local authority, or arranged on its behalf through the Best Start Family Hubs and Start for Life programme, collect scores before and
              after an intervention, provide these outcome scores for service users in the past 3 months.
            </p>
            <p className="govuk-body">You can submit your data in one of two ways:</p>
            <ul className="govuk-list govuk-list--bullet">
              <li>Complete the online form to enter information for each service</li>
              <li>Upload a Comma Separated Values (CSV) file to give us the data for all services at once</li>
            </ul>

            <h3 className="govuk-heading-s">How to provide outcome scores using the guided form</h3>
            <p className="govuk-body">
              Select 'Add record' to add a outcome score record for a single service user. The guided form will take you through the data you need to enter, provide relevant
              guidance and check for errors as you go.
            </p>
            <p className="govuk-body">You can save your progress at any time and return later to complete your submission.</p>

            <h3 className="govuk-heading-s">How to provide outcome scores using a CSV</h3>
            <p className="govuk-body">
              If you want to provide outcome scores for multiple users or services at once, you can upload the data using a Comma Separated Values file (CSV).
            </p>
            <p className="govuk-body">
              Download the CSV template to add outcome scores. If you need help understanding the format, download the example guide first to see sample data.
            </p>

            <div className="govuk-grid-row govuk-!-margin-top-6 govuk-!-margin-bottom-6">
              <div className="govuk-grid-column-full">
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: '15px' }}>
                  <SpreadsheetIcon />
                  <div>
                    <p className="govuk-body govuk-!-font-weight-bold govuk-!-margin-bottom-0">Outcome scores</p>
                    <p className="govuk-body-s govuk-!-margin-bottom-2">Template</p>
                    <ul className="govuk-list govuk-!-margin-bottom-0">
                      <li>
                        <a
                          href="#"
                          className="govuk-link"
                          onClick={(e) => {
                            e.preventDefault();
                            handleDownloadTemplate('csv');
                          }}>
                          Download CSV template
                        </a>
                      </li>
                      <li>
                        <a
                          href="#"
                          className="govuk-link"
                          onClick={(e) => {
                            e.preventDefault();
                            handleDownloadTemplate('xlsx');
                          }}>
                          Download Excel template
                        </a>
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>

            <p className="govuk-body">
              <a href="#" className="govuk-link">
                ► Help with outcome scores
              </a>
            </p>

            <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

            {/* Service Requirements Summary */}
            {moduleData.serviceRequirements && moduleData.serviceRequirements.length > 0 && (
              <div className="govuk-!-margin-bottom-6">
                <h2 className="govuk-heading-m">Records required per service</h2>
                {!moduleData.isSection2Complete && (
                  <div className="govuk-warning-text">
                    <span className="govuk-warning-text__icon" aria-hidden="true">
                      !
                    </span>
                    <strong className="govuk-warning-text__text">
                      <span className="govuk-visually-hidden">Warning</span>
                      Section 2 (Service users) must be completed before this section can be marked as complete.
                    </strong>
                  </div>
                )}
                <table className="govuk-table">
                  <thead className="govuk-table__head">
                    <tr className="govuk-table__row">
                      <th scope="col" className="govuk-table__header">
                        Service
                      </th>
                      <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                        Expected records
                      </th>
                      <th scope="col" className="govuk-table__header govuk-table__header--numeric">
                        Actual records
                      </th>
                      <th scope="col" className="govuk-table__header">
                        Status
                      </th>
                    </tr>
                  </thead>
                  <tbody className="govuk-table__body">
                    {moduleData.serviceRequirements.map((req: ServiceRecordRequirementDto) => (
                      <tr key={req.serviceId} className="govuk-table__row">
                        <td className="govuk-table__cell">{req.serviceName}</td>
                        <td className="govuk-table__cell govuk-table__cell--numeric">{req.expectedRecords}</td>
                        <td className="govuk-table__cell govuk-table__cell--numeric">{req.actualRecords}</td>
                        <td className="govuk-table__cell">
                          {req.isComplete ? (
                            <GovUKTag colour="green">Complete</GovUKTag>
                          ) : req.actualRecords > req.expectedRecords ? (
                            <GovUKTag colour="red">Too many</GovUKTag>
                          ) : req.actualRecords > 0 ? (
                            <GovUKTag colour="blue">In progress</GovUKTag>
                          ) : (
                            <GovUKTag colour="grey">Not started</GovUKTag>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="govuk-table__row">
                      <th scope="row" className="govuk-table__header">
                        Total
                      </th>
                      <td className="govuk-table__cell govuk-table__cell--numeric govuk-!-font-weight-bold">{moduleData.totalExpectedRecords}</td>
                      <td className="govuk-table__cell govuk-table__cell--numeric govuk-!-font-weight-bold">{moduleData.totalRecords}</td>
                      <td className="govuk-table__cell">
                        {moduleData.totalRecords === moduleData.totalExpectedRecords && moduleData.isSection2Complete ? (
                          <GovUKTag colour="green">Complete</GovUKTag>
                        ) : (
                          <GovUKTag colour="blue">In progress</GovUKTag>
                        )}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            )}

            <div className="service-users__header">
              <h2 className="govuk-heading-m govuk-!-margin-bottom-0">Outcome score records</h2>
              <div className="service-users__actions">
                <GovUKButton className="govuk-button govuk-!-margin-bottom-0" onClick={handleAddRecord} isLoading={createRecordMutation.isLoading}>
                  Add record
                </GovUKButton>
                <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={handleBulkUpload}>
                  Upload with CSV
                </GovUKButton>
                <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={() => handleDownloadTemplate('csv')}>
                  Download copy in CSV
                </GovUKButton>
              </div>
            </div>

            <div className="govuk-grid-row govuk-!-margin-bottom-4">
              <div className="govuk-grid-column-one-third">
                <div className="govuk-form-group">
                  <label className="govuk-label" htmlFor="search-records">
                    Search record by anonymised ID
                  </label>
                  <div style={{ display: 'flex' }}>
                    <input
                      className="govuk-input"
                      id="search-records"
                      name="search-records"
                      type="text"
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      style={{ borderRight: 'none' }}
                    />
                    <button
                      type="button"
                      style={{
                        marginBottom: 0,
                        padding: '7px 12px',
                        backgroundColor: '#1d70b8',
                        border: '2px solid #1d70b8',
                        cursor: 'pointer',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}>
                      <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="20"
                        height="20"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="white"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round">
                        <circle cx="11" cy="11" r="8" />
                        <line x1="21" y1="21" x2="16.65" y2="16.65" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
              <div className="govuk-grid-column-one-third">
                <div className="govuk-form-group">
                  <label className="govuk-label" htmlFor="filter-service">
                    Filter records by service
                  </label>
                  <select
                    className="govuk-select govuk-!-width-full"
                    id="filter-service"
                    name="filter-service"
                    value={filterService}
                    onChange={(e) => setFilterService(e.target.value)}>
                    <option value="">Select service...</option>
                    {uniqueServices.map((service) => (
                      <option key={service.id} value={service.id}>
                        {service.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="govuk-grid-column-one-third">
                <div className="govuk-form-group">
                  <label className="govuk-label" htmlFor="sort-records">
                    Sort records
                  </label>
                  <select
                    className="govuk-select govuk-!-width-full"
                    id="sort-records"
                    name="sort-records"
                    value={sortOrder}
                    onChange={(e) => setSortOrder(e.target.value as 'newest' | 'oldest')}>
                    <option value="newest">Newest first</option>
                    <option value="oldest">Oldest first</option>
                  </select>
                </div>
              </div>
            </div>

            {filteredAndSortedRecords.length > 0 ? (
              <ul className="govuk-list" style={{ borderTop: '1px solid #b1b4b6', marginTop: 0 }}>
                {filteredAndSortedRecords.map((record: OutcomeScoreRecordDto) => (
                  <li
                    key={record.recordId}
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      padding: '15px 0',
                      borderBottom: '1px solid #b1b4b6',
                    }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                      <span className="govuk-body govuk-!-margin-bottom-0" style={{ fontWeight: 'bold' }}>
                        {record.anonymisedId}
                      </span>
                      <span className="govuk-body govuk-!-margin-bottom-0">{record.serviceName || '—'}</span>
                      {record.status === 'Draft' && <GovUKTag colour={getSubmissionStatusTagColour(record.status)}>{record.status}</GovUKTag>}
                    </div>
                    <div style={{ display: 'flex', gap: '15px' }}>
                      <a
                        href="#"
                        className="govuk-link"
                        onClick={(e) => {
                          e.preventDefault();
                          handleViewRecord(record.recordId);
                        }}>
                        View<span className="govuk-visually-hidden"> record {record.anonymisedId}</span>
                      </a>
                      <a
                        href="#"
                        className="govuk-link"
                        onClick={(e) => {
                          e.preventDefault();
                          handleChangeRecord(record.recordId);
                        }}>
                        Change<span className="govuk-visually-hidden"> record {record.anonymisedId}</span>
                      </a>
                      <a
                        href="#"
                        className="govuk-link"
                        onClick={(e) => {
                          e.preventDefault();
                          handleDeleteRecord(record.recordId);
                        }}>
                        Delete<span className="govuk-visually-hidden"> record {record.anonymisedId}</span>
                      </a>
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <div className="govuk-inset-text">
                <p className="govuk-body">No records found. Click 'Add record' to create your first outcome score record.</p>
              </div>
            )}
          </>
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default OutcomeScoresList;
