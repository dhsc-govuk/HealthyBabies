import React, { useState, useMemo, useEffect } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKTag, GovUKDetails, GovUKNotificationBanner } from '../../../../components/GovUKComponents';
import { SpreadsheetIcon } from '../../../../components/Icons';
import { getServiceUsersModule, ServiceFormStatusDto, downloadBulkUploadTemplate, type TemplateFormat, getBulkUploadTemplateFileName } from '../queries';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import './styles.css';
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

interface LocationState {
  savedServiceName?: string;
  bulkUploadSuccess?: {
    successfulRows: number;
    totalRows: number;
  };
}

const ServiceUsersList = (): React.ReactElement => {
  usePageTitle('Service users');
  const navigate = useNavigate();
  const location = useLocation();
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  const [searchTerm, setSearchTerm] = useState('');
  const [sortBy, setSortBy] = useState('name');
  const [savedServiceName, setSavedServiceName] = useState<string | null>(null);
  const [bulkUploadSuccess, setBulkUploadSuccess] = useState<{ successfulRows: number; totalRows: number } | null>(null);

  useEffect(() => {
    const state = location.state as LocationState | null;
    if (state?.savedServiceName) {
      setSavedServiceName(state.savedServiceName);
    }
    if (state?.bulkUploadSuccess) {
      setBulkUploadSuccess(state.bulkUploadSuccess);
    }
    // Clear the state so the banner doesn't show again on refresh
    if (state?.savedServiceName || state?.bulkUploadSuccess) {
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  const { data, isLoading } = useQuery({
    queryKey: ['service-users-module', submissionId, moduleId],
    queryFn: () => getServiceUsersModule(submissionId!, moduleId!),
    enabled: !!submissionId && !!moduleId,
  });

  const moduleData = data?.data;

  const filteredAndSortedServices = useMemo(() => {
    if (!moduleData?.services) return [];

    let services = [...moduleData.services];

    if (searchTerm) {
      services = services.filter((s) => s.serviceName.toLowerCase().includes(searchTerm.toLowerCase()));
    }

    services.sort((a, b) => {
      if (sortBy === 'status') {
        const statusOrder: Record<string, number> = { 'Not started': 0, 'In progress': 1, Completed: 2 };
        return (statusOrder[a.status] ?? 0) - (statusOrder[b.status] ?? 0);
      }
      return a.serviceName.localeCompare(b.serviceName);
    });

    return services;
  }, [moduleData?.services, searchTerm, sortBy]);

  const handleServiceClick = (serviceId: string, status: string) => {
    if (status.toLowerCase() === 'completed') {
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services/${serviceId}/view`);
    } else {
      navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services/${serviceId}`);
    }
  };

  const handleBack = () => {
    navigate(`/organisation-admin/submissions/${submissionId}`);
  };

  const handleDownloadTemplate = async (format: TemplateFormat = 'csv') => {
    try {
      const blob = await downloadBulkUploadTemplate(submissionId!, moduleId!, format);
      const filename = getBulkUploadTemplateFileName(format, 'service_users');
      downloadFile(blob, filename);
    } catch (error) {
      console.error('Failed to download template:', error);
    }
  };

  const handleBulkUpload = () => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/services/bulk-upload`);
  };

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading services" />
      </GeneralLayout>
    );
  }

  if (!moduleData) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Module not found</h1>
        <p className="govuk-body">The module you are looking for does not exist.</p>
        <GovUKButton onClick={() => navigate(`/organisation-admin/submissions/${submissionId}`)}>Back to submission</GovUKButton>
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Submissions', link: '/organisation-admin/submissions' },
        { label: moduleData.submissionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
        { label: 'Section 2: Service users', link: '#' },
      ]}
      currentPage=""
      backLink={{ href: '#', onClick: handleBack }}>
      {savedServiceName && (
        <GovUKNotificationBanner type="success" title="Service user data saved">
          <p className="govuk-body">Service user data for '{savedServiceName}' has been successfully saved.</p>
        </GovUKNotificationBanner>
      )}

      {bulkUploadSuccess && (
        <GovUKNotificationBanner type="success" title="Bulk upload complete">
          <p className="govuk-body">
            {bulkUploadSuccess.successfulRows} of {bulkUploadSuccess.totalRows} row(s) were successfully uploaded.
          </p>
        </GovUKNotificationBanner>
      )}

      <span className="govuk-caption-l">Section {moduleData.sectionNumber}</span>
      <h1 className="govuk-heading-l">Service users</h1>

      <h2 className="govuk-heading-m">Tell us about your service users</h2>
      <p className="govuk-body">
        Provide information about the users of any services your local authority has delivered, or arranged to be delivered on its behalf, through your Best Start Family Hub and
        Healthy Babies Network over the past 3 months.
      </p>
      <p className="govuk-body">If you do not have any user data for this period, you can tell us that instead.</p>

      <p className="govuk-body">You can submit your data in one of two ways:</p>
      <ul className="govuk-list govuk-list--bullet">
        <li>Complete the online form to enter information for each service</li>
        <li>Upload a Comma Separated Values (CSV) file to give us the data for all services at once</li>
      </ul>

      <h3 className="govuk-heading-s">How to provide service user numbers using the guided form</h3>
      <p className="govuk-body">
        Select the service you want to report on from the list below. The guided form will take you through the data you need to enter, provide relevant guidance and check for
        errors as you go.
      </p>
      <p className="govuk-body">You can save your progress at any time and return later to complete your submission.</p>

      <h3 className="govuk-heading-s">How to provide service numbers using a CSV</h3>
      <p className="govuk-body">If you want to provide service user data for multiple services at once, you can upload the data using a Comma Separated Values file (CSV).</p>
      <p className="govuk-body">Download the CSV template which contains your incomplete services. Open the template in a spreadsheet editor and fill in the service user data.</p>
      <p className="govuk-body">If you need help understanding the format, download the example guide first to see sample data.</p>

      <div className="govuk-grid-row govuk-!-margin-top-6 govuk-!-margin-bottom-6">
        <div className="govuk-grid-column-full">
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '15px' }}>
            <SpreadsheetIcon />
            <div>
              <p className="govuk-body govuk-!-font-weight-bold govuk-!-margin-bottom-0">Service users</p>
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

      <h3 className="govuk-heading-s">About your list of services</h3>
      <p className="govuk-body">The services shown below have been pre-populated from your Core Services List.</p>
      <p className="govuk-body">
        If a service is missing, or if a service is no longer delivered in your local authority, you can add, remove or edit services using the 'Manage services' button.
      </p>

      <GovUKDetails summary="Help with Best Start Family Hub and Healthy Babies services">
        <p className="govuk-body">
          Best Start Family Hub services are those delivered through your family hub network. Healthy Babies services focus on supporting families with children aged 0-2.
        </p>
      </GovUKDetails>

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="service-users__header">
        <h2 className="govuk-heading-m govuk-!-margin-bottom-0">Services</h2>
        <div className="service-users__actions">
          <GovUKButton className="govuk-button govuk-!-margin-bottom-0" onClick={handleBulkUpload}>
            Upload with CSV
          </GovUKButton>
          <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={() => handleDownloadTemplate('csv')}>
            Download copy in CSV
          </GovUKButton>
          <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={() => navigate('/organisation-admin/core-data/services')}>
            Manage services
          </GovUKButton>
        </div>
      </div>

      <div className="govuk-grid-row govuk-!-margin-bottom-4">
        <div className="govuk-grid-column-two-thirds">
          <div className="govuk-form-group">
            <label className="govuk-label" htmlFor="search-services">
              Search service by name
            </label>
            <input className="govuk-input" id="search-services" name="search-services" type="text" value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} />
          </div>
        </div>
        <div className="govuk-grid-column-one-third">
          <div className="govuk-form-group">
            <label className="govuk-label" htmlFor="sort-services">
              Sort services
            </label>
            <select className="govuk-select govuk-!-width-full" id="sort-services" name="sort-services" value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
              <option value="name">By name</option>
              <option value="status">By status</option>
            </select>
          </div>
        </div>
      </div>

      {filteredAndSortedServices.length > 0 ? (
        <ul className="govuk-list" style={{ borderTop: '1px solid #b1b4b6' }}>
          {filteredAndSortedServices.map((service: ServiceFormStatusDto) => (
            <li key={service.serviceId} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '15px 0', borderBottom: '1px solid #b1b4b6' }}>
              <a
                href="#"
                className="govuk-link"
                onClick={(e) => {
                  e.preventDefault();
                  handleServiceClick(service.serviceId, service.status);
                }}>
                {service.serviceName}
              </a>
              <GovUKTag colour={getSubmissionStatusTagColour(service.status)}>{service.status}</GovUKTag>
            </li>
          ))}
        </ul>
      ) : (
        <div className="govuk-inset-text">
          <p className="govuk-body">{searchTerm ? 'No services match your search.' : 'No services have been added yet. Please add services before completing this section.'}</p>
        </div>
      )}
    </GeneralLayout>
  );
};

export default ServiceUsersList;
