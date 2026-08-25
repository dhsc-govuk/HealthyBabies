import React, { useMemo, useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKLinkButton, GovUKTag, GovUKNotificationBanner } from '../../../../../components/GovUKComponents';
import { SpreadsheetIcon } from '../../../../../components/Icons';
import { getSubmissionStatusTagColour } from '../../../../../helpers/submissionStatusColour';
import { getActivitiesModule, downloadBulkUploadTemplate, ActivityRecordDto } from '../../queries';
import { getBulkUploadTemplateFileName, type TemplateFormat } from '../../../../../components/Global/BulkUpload/dtos';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { getModuleContent } from '../moduleContent';
import { decodeFromWaf } from '../../../../../helpers/stringUtils';
import { ROUTES } from '../../../../../constants/routes';

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

const ActivitiesList = (): React.ReactElement => {
  const navigate = useNavigate();
  const routerLocation = useLocation();
  const { submissionId, moduleId } = useParams<{ submissionId: string; moduleId: string }>();

  const [searchTerm, setSearchTerm] = useState('');
  const [sortOrder, setSortOrder] = useState<'a-z' | 'z-a' | 'newest' | 'oldest'>('a-z');

  const notification = routerLocation.state?.notification as { type: 'success' | 'error'; title: string; message: string } | undefined;

  const apiBase = routerLocation.pathname.startsWith(ROUTES.organisationAdmin.root) ? 'organisation-admin' : 'delivery-plan';

  const { data, isLoading } = useQuery({
    queryKey: ['activities-module', submissionId, moduleId],
    queryFn: () => getActivitiesModule(submissionId!, moduleId!, apiBase),
    enabled: !!submissionId && !!moduleId,
  });

  const moduleData = data?.data;
  const content = getModuleContent(moduleData?.code);
  // Once the round is submitted (or its window has closed) the plan is a record, not a form.
  const isReadOnly = moduleData?.isReadOnly === true;

  usePageTitle(moduleData?.name ?? content.listHeading);

  const handleAddActivity = () => {
    navigate(ROUTES.areaSubmissions.activities.create(apiBase, submissionId, moduleId));
  };

  const handleBulkUpload = () => {
    navigate(ROUTES.areaSubmissions.activities.bulkUpload(apiBase, submissionId, moduleId));
  };

  const handleDownloadTemplate = async (format: TemplateFormat = 'csv') => {
    const blob = await downloadBulkUploadTemplate(submissionId!, moduleId!, format, apiBase);
    const filename = getBulkUploadTemplateFileName(format, moduleData?.code ?? 'activities');
    downloadFile(blob, filename);
  };

  const filteredAndSorted = useMemo(() => {
    if (!moduleData?.activities) return [];

    let list = [...moduleData.activities];

    if (searchTerm) {
      const lower = searchTerm.toLowerCase();
      list = list.filter((a) => (decodeFromWaf(a.activityName) ?? '').toLowerCase().includes(lower));
    }

    list.sort((a, b) => {
      const nameA = decodeFromWaf(a.activityName) ?? '';
      const nameB = decodeFromWaf(b.activityName) ?? '';
      if (sortOrder === 'a-z') return nameA.localeCompare(nameB);
      if (sortOrder === 'z-a') return nameB.localeCompare(nameA);
      const dateA = a.lastModified ? new Date(a.lastModified).getTime() : 0;
      const dateB = b.lastModified ? new Date(b.lastModified).getTime() : 0;
      return sortOrder === 'newest' ? dateB - dateA : dateA - dateB;
    });

    return list;
  }, [moduleData?.activities, searchTerm, sortOrder]);

  const handleView = (activityId: string) => {
    navigate(ROUTES.areaSubmissions.activities.view(apiBase, submissionId, moduleId, activityId));
  };

  const handleChange = (activityId: string) => {
    navigate(ROUTES.areaSubmissions.activities.form(apiBase, submissionId, moduleId, activityId));
  };

  const handleDelete = (activityId: string, activityDisplayName: string, moduleCode: string | undefined) => {
    navigate(ROUTES.areaSubmissions.activities.delete(apiBase, submissionId, moduleId, activityId), {
      state: { activityName: decodeFromWaf(activityDisplayName) || content.addButtonText, moduleCode },
    });
  };

  const handleBack = () => {
    navigate(ROUTES.areaSubmissions.view(apiBase, submissionId));
  };

  return (
    <LoadingSpinner loading={isLoading} label={`Loading ${content.activityNounPlural}`}>
      <GeneralLayout backLink={{ href: '#', onClick: handleBack }}>
        {notification && (
          <GovUKNotificationBanner type={notification.type === 'success' ? 'success' : 'important'} title={notification.title} autoDismiss autoDismissTimeout={8000}>
            <p className="govuk-body">{notification.message}</p>
          </GovUKNotificationBanner>
        )}

        {moduleData && (
          <>
            <h1 className="govuk-heading-l">{moduleData.name}</h1>

            {moduleData.description && <p className="govuk-body">{moduleData.description}</p>}

            <p className="govuk-body">{content.instructionText}</p>
            <ul className="govuk-list govuk-list--bullet">
              <li>Complete the online form to enter information for each {content.activityNoun}</li>
              <li>Upload a Comma Separated Values (CSV) file to give us information for all {content.activityNounPlural} at once</li>
            </ul>

            <p className="govuk-body">
              <GovUKLinkButton>
                ► {content.helpLinkText}
              </GovUKLinkButton>
            </p>

            <div className="govuk-grid-row govuk-!-margin-top-4 govuk-!-margin-bottom-4">
              <div className="govuk-grid-column-full">
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: '15px' }}>
                  <SpreadsheetIcon />
                  <div>
                    <p className="govuk-body govuk-!-font-weight-bold govuk-!-margin-bottom-0">{moduleData.name}</p>
                    <p className="govuk-body-s govuk-!-margin-bottom-2">Template</p>
                    <ul className="govuk-list govuk-!-margin-bottom-0">
                      <li>
                        <GovUKLinkButton onClick={() => handleDownloadTemplate('csv')}>
                          Download CSV template
                        </GovUKLinkButton>
                      </li>
                      <li>
                        <GovUKLinkButton onClick={() => handleDownloadTemplate('xlsx')}>
                          Download Excel template with guidance
                        </GovUKLinkButton>
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>

            {isReadOnly ? (
              <div className="govuk-inset-text govuk-!-margin-top-0">
                This delivery plan has been submitted, so it can no longer be changed. You can still view everything recorded in it.
              </div>
            ) : (
              <div style={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'flex-start', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '10px' }}>
                <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                  <GovUKButton className="govuk-button govuk-!-margin-bottom-0" onClick={handleAddActivity}>
                    {content.addButtonText}
                  </GovUKButton>
                  <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={handleBulkUpload}>
                    {content.uploadLabel}
                  </GovUKButton>
                </div>
              </div>
            )}

            <div className="govuk-grid-row govuk-!-margin-bottom-4">
              <div className="govuk-grid-column-three-quarters">
                <div className="govuk-form-group" role="search" aria-label={`Search ${content.activityNounPlural}`}>
                  <label className="govuk-label" htmlFor="search-activities">
                    Search by {content.activityNoun} name
                  </label>
                  <div style={{ display: 'flex' }}>
                    <input
                      className="govuk-input"
                      id="search-activities"
                      name="search-activities"
                      type="search"
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      aria-label={`Search ${content.activityNounPlural} by name`}
                      style={{ borderRight: 'none' }}
                    />
                    <button
                      type="button"
                      aria-label={`Search ${content.activityNounPlural}`}
                      style={{
                        marginBottom: 0,
                        padding: '7px 12px',
                        backgroundColor: '#1d70b8',
                        border: '2px solid #1d70b8',
                        cursor: 'pointer',
                        display: 'flex',
                        alignItems: 'center',
                      }}>
                      <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">
                        <circle cx="11" cy="11" r="8" />
                        <line x1="21" y1="21" x2="16.65" y2="16.65" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
              <div className="govuk-grid-column-one-quarter">
                <div className="govuk-form-group">
                  <label className="govuk-label" htmlFor="sort-activities">
                    Sort {content.activityNounPlural}
                  </label>
                  <select
                    className="govuk-select govuk-!-width-full"
                    id="sort-activities"
                    name="sort-activities"
                    value={sortOrder}
                    onChange={(e) => setSortOrder(e.target.value as typeof sortOrder)}
                    aria-label={`Sort ${content.activityNounPlural}`}>
                    <option value="a-z">Alphabetically, A-Z</option>
                    <option value="z-a">Alphabetically, Z-A</option>
                    <option value="newest">Newest first</option>
                    <option value="oldest">Oldest first</option>
                  </select>
                </div>
              </div>
            </div>

            <p className="govuk-body govuk-!-font-size-16" aria-live="polite" aria-atomic="true">
              {filteredAndSorted.length === 0
                ? `No ${content.activityNounPlural} found`
                : `Showing ${filteredAndSorted.length} ${filteredAndSorted.length === 1 ? content.activityNoun : content.activityNounPlural}`}
            </p>

            {filteredAndSorted.length > 0 ? (
              <ul className="govuk-list" aria-label={content.listHeading} style={{ borderTop: '1px solid #b1b4b6', marginTop: 0 }}>
                {filteredAndSorted.map((activity: ActivityRecordDto) => {
                  const displayName = decodeFromWaf(activity.activityName) || content.addButtonText;
                  return (
                    <li
                      key={activity.activityId}
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        padding: '15px 0',
                        borderBottom: '1px solid #b1b4b6',
                      }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                        <GovUKLinkButton
                          className="govuk-!-font-weight-bold govuk-body govuk-!-margin-bottom-0"
                          onClick={() => (isReadOnly ? handleView(activity.activityId) : handleChange(activity.activityId))}>
                          {displayName}
                        </GovUKLinkButton>
                        {activity.status === 'Draft' && (
                          <GovUKTag colour={getSubmissionStatusTagColour(activity.status)}>
                            {activity.status}
                            <span className="govuk-visually-hidden"> – {displayName} is not yet complete</span>
                          </GovUKTag>
                        )}
                      </div>
                      <div style={{ display: 'flex', gap: '15px' }}>
                        <GovUKLinkButton onClick={() => handleView(activity.activityId)}>
                          View<span className="govuk-visually-hidden"> {displayName}</span>
                        </GovUKLinkButton>
                        {!isReadOnly && (
                          <>
                            <GovUKLinkButton onClick={() => handleChange(activity.activityId)}>
                              Change<span className="govuk-visually-hidden"> {displayName}</span>
                            </GovUKLinkButton>
                            <GovUKLinkButton onClick={() => handleDelete(activity.activityId, activity.activityName, moduleData?.code)}>
                              Delete<span className="govuk-visually-hidden"> {displayName}</span>
                            </GovUKLinkButton>
                          </>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ul>
            ) : (
              <div className="govuk-inset-text" role="status">
                <p className="govuk-body">{content.emptyText}</p>
              </div>
            )}
          </>
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ActivitiesList;
