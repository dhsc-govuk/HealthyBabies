import React from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useQuery, useMutation } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { GovUKNotificationBanner, GovUKButton, H1, H2, H4, Paragraph, UnorderedList, ListItem, useGovUKNotification } from '../../../../components/GovUKComponents';
import GovUKTable, { Column } from '../../../../components/GovUKComponents/GovUKTable';
import { SpreadsheetIcon } from '../../../../components/Icons';
import {
  getServices,
  servicesCacheKey,
  ServiceStatus,
  ServiceListDto,
  downloadServicesTemplate,
  getServicesTemplateFileName,
  getServicesTemplateMimeType,
  TemplateFormat,
} from '../../../../components/Global/Services';
import { processError } from '../../../../helpers/axiosErrorFallback';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

interface LocationState {
  notification?: {
    type: 'success' | 'important';
    title: string;
    message: string;
  };
}

const ListServices = (): React.ReactElement => {
  usePageTitle('Services');
  const navigate = useNavigate();
  const location = useLocation();
  const locationState = location.state as LocationState | null;
  const notification = locationState?.notification;

  React.useEffect(() => {
    if (notification) {
      window.scrollTo(0, 0);
      window.history.replaceState({}, document.title);
    }
  }, [notification]);

  const { data, isLoading } = useQuery({
    queryKey: servicesCacheKey(),
    queryFn: () => getServices(),
  });

  const services = data?.data ?? [];

  const handleCreate = () => {
    navigate('/organisation-admin/core-data/services/create');
  };

  const handleBulkUpload = () => {
    navigate('/organisation-admin/core-data/services/bulk-upload');
  };

  const { setNotification } = useGovUKNotification();

  const { mutate: downloadTemplate, isLoading: downloadingTemplate } = useMutation({
    mutationKey: ['download-services-template'],
    mutationFn: (format: TemplateFormat) => downloadServicesTemplate(format),
    onSuccess: (data, format) => {
      const blob = new Blob([data.data], { type: getServicesTemplateMimeType(format) });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = getServicesTemplateFileName(format);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
    onError: (error) => {
      processError(error, (message) => setNotification({ type: 'important', title: 'Error', message }));
    },
  });

  const columns: Column<ServiceListDto>[] = [
    {
      key: 'name',
      header: 'Service name',
      render: (service) => <span style={{ fontWeight: 700, fontSize: '19px', color: service.status === ServiceStatus.Draft ? '#505a5f' : '#0b0c0c' }}>{service.name}</span>,
    },
    {
      key: 'status',
      header: '',
      render: (service) => (service.status === ServiceStatus.Draft ? <span style={{ fontSize: '19px', color: '#505a5f' }}>Draft</span> : null),
    },
    {
      key: 'actions',
      header: '',
      align: 'right',
      render: (service) => (
        <span className="services-actions">
          <Link to={`/organisation-admin/core-data/services/${service.id}`} className="govuk-link govuk-link--no-visited-state services-actions__link">
            View
          </Link>
          <span className="services-actions__divider" aria-hidden="true">
            |
          </span>
          <Link to={`/organisation-admin/core-data/services/${service.id}/edit`} className="govuk-link govuk-link--no-visited-state services-actions__link">
            Change
          </Link>
          <span className="services-actions__divider" aria-hidden="true">
            |
          </span>
          <Link to={`/organisation-admin/core-data/services/${service.id}/delete`} className="govuk-link govuk-link--no-visited-state services-actions__link">
            Delete
          </Link>
        </span>
      ),
    },
  ];

  const sortOptions = [
    { value: 'name-asc', label: 'Alphabetically (A-Z)' },
    { value: 'name-desc', label: 'Alphabetically (Z-A)' },
    { value: 'status-asc', label: 'By status' },
  ];

  return (
    <LoadingBox loading={isLoading}>
      <GeneralLayout breadcrumbs={[{ label: 'Home', link: '/organisation-admin/home' }]}>
        {notification && (
          <GovUKNotificationBanner type={notification.type} title={notification.title}>
            <p className="govuk-body">{notification.message}</p>
          </GovUKNotificationBanner>
        )}

        <H1>Manage services</H1>
        <Paragraph>This page lets you add, view, change or delete services offered by Family Hubs in your local authority.</Paragraph>

        <Paragraph>You can add or update services in one of two ways:</Paragraph>
        <UnorderedList>
          <ListItem>Complete the online form to enter information for each service</ListItem>
          <ListItem>Upload a Comma Separated Values (CSV) file to give us the data for all services at once</ListItem>
        </UnorderedList>
        <Paragraph>You can also remove a service at any time to keep your information accurate.</Paragraph>

        <H4>How to add or update a service using the guided form</H4>
        <Paragraph>
          Select &apos;Add service&apos; to add or update a single service to your list. The guided form will take you through the data you need to enter, provide relevant guidance
          and check for errors as you go.
        </Paragraph>
        <Paragraph>You can save your progress at any time and return later to complete adding a new service.</Paragraph>

        <H4>How to add or update services using a CSV</H4>
        <Paragraph>If you want to add or update multiple services at once, you can upload a service list using a Comma Separated Values file (CSV).</Paragraph>
        <Paragraph>
          Download the service list template as a CSV or a macro-enabled Excel file (XLSM) from this page. Open the template in a spreadsheet editor and add the details for one or
          more services.
        </Paragraph>
        <Paragraph>When you&apos;re finished, save the file as a CSV and upload it by selecting &apos;Upload with CSV&apos;.</Paragraph>

        <div className="services-template">
          <SpreadsheetIcon />
          <div>
            <p className="govuk-body govuk-!-font-weight-bold govuk-!-margin-bottom-0">Service list</p>
            <p className="govuk-body-s govuk-!-margin-bottom-2">Template</p>
            <div className="services-template__downloads">
              <a
                href="#"
                className="govuk-link"
                onClick={(e) => {
                  e.preventDefault();
                  downloadTemplate('csv');
                }}>
                Download in CSV
              </a>
              <a
                href="#"
                className="govuk-link"
                onClick={(e) => {
                  e.preventDefault();
                  downloadTemplate('xlsx');
                }}>
                Download in XLSM
              </a>
            </div>
          </div>
        </div>

        <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

        <div className="services-header">
          <H2 style={{ marginBottom: 0 }}>Services</H2>
          <div className="services-header__actions">
            <GovUKButton className="govuk-button govuk-!-margin-bottom-0" onClick={handleCreate}>
              Add service
            </GovUKButton>
            <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={handleBulkUpload}>
              Upload with CSV
            </GovUKButton>
            <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={() => downloadTemplate('csv')} disabled={downloadingTemplate}>
              Download copy in CSV
            </GovUKButton>
          </div>
        </div>

        <GovUKTable
          data={services}
          columns={columns}
          keyExtractor={(service) => service.id}
          searchPlaceholder="Search service by name"
          sortOptions={sortOptions}
          sortLabel="Sort services"
          emptyMessage="No services found."
          hideHeader
        />
      </GeneralLayout>
    </LoadingBox>
  );
};

export default ListServices;
