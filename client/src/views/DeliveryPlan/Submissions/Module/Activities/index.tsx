import React, { useMemo, useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery } from 'react-query';

import { GeneralLayout } from '../../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKTag, GovUKNotificationBanner } from '../../../../../components/GovUKComponents';
import { getSubmissionStatusTagColour } from '../../../../../helpers/submissionStatusColour';
import { getActivitiesModule, ActivityRecordDto } from '../../queries';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { getModuleContent } from '../moduleContent';
import { decodeFromWaf } from '../../../../../helpers/stringUtils';

const ActivitiesList = (): React.ReactElement => {
  const navigate = useNavigate();
  const routerLocation = useLocation();
  const { submissionId, moduleId } = useParams<{ submissionId: string; moduleId: string }>();

  const [searchTerm, setSearchTerm] = useState('');
  const [sortOrder, setSortOrder] = useState<'a-z' | 'z-a' | 'newest' | 'oldest'>('a-z');

  const notification = routerLocation.state?.notification as { type: 'success' | 'error'; title: string; message: string } | undefined;

  const apiBase = routerLocation.pathname.startsWith('/organisation-admin') ? 'organisation-admin' : 'delivery-plan';
  const basePath = `/${apiBase}`;

  const { data, isLoading } = useQuery({
    queryKey: ['activities-module', submissionId, moduleId],
    queryFn: () => getActivitiesModule(submissionId!, moduleId!, apiBase),
    enabled: !!submissionId && !!moduleId,
  });

  const moduleData = data?.data;
  const content = getModuleContent(moduleData?.code);

  usePageTitle(moduleData?.name ?? content.listHeading);

  const handleAddActivity = () => {
    navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities/new`);
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
    navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities/${activityId}/view`);
  };

  const handleChange = (activityId: string) => {
    navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities/${activityId}`);
  };

  const handleDelete = (activityId: string, activityDisplayName: string, moduleCode: string | undefined) => {
    navigate(`${basePath}/submissions/${submissionId}/modules/${moduleId}/activities/${activityId}/delete`, {
      state: { activityName: decodeFromWaf(activityDisplayName) || content.addButtonText, moduleCode },
    });
  };

  const handleBack = () => {
    navigate(`${basePath}/submissions/${submissionId}`);
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
              <a href="#" className="govuk-link">
                ► {content.helpLinkText}
              </a>
            </p>

            <div style={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'flex-start', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '10px' }}>
              <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                <GovUKButton className="govuk-button govuk-!-margin-bottom-0" onClick={handleAddActivity}>
                  {content.addButtonText}
                </GovUKButton>
                <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={() => {}}>
                  {content.uploadLabel}
                </GovUKButton>
                <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={() => {}}>
                  {content.downloadLabel}
                </GovUKButton>
              </div>
            </div>

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
                        <a
                          href="#"
                          className="govuk-link govuk-!-font-weight-bold govuk-body govuk-!-margin-bottom-0"
                          onClick={(e) => {
                            e.preventDefault();
                            handleChange(activity.activityId);
                          }}>
                          {displayName}
                        </a>
                        {activity.status === 'Draft' && (
                          <GovUKTag colour={getSubmissionStatusTagColour(activity.status)}>
                            {activity.status}
                            <span className="govuk-visually-hidden"> – {displayName} is not yet complete</span>
                          </GovUKTag>
                        )}
                      </div>
                      <div style={{ display: 'flex', gap: '15px' }}>
                        <a
                          href="#"
                          className="govuk-link"
                          onClick={(e) => {
                            e.preventDefault();
                            handleView(activity.activityId);
                          }}>
                          View<span className="govuk-visually-hidden"> {displayName}</span>
                        </a>
                        <a
                          href="#"
                          className="govuk-link"
                          onClick={(e) => {
                            e.preventDefault();
                            handleChange(activity.activityId);
                          }}>
                          Change<span className="govuk-visually-hidden"> {displayName}</span>
                        </a>
                        <a
                          href="#"
                          className="govuk-link"
                          onClick={(e) => {
                            e.preventDefault();
                            handleDelete(activity.activityId, activity.activityName, moduleData?.code);
                          }}>
                          Delete<span className="govuk-visually-hidden"> {displayName}</span>
                        </a>
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
