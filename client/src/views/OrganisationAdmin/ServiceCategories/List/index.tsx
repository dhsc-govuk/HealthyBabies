import React, { useMemo } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useQuery } from 'react-query';
import { LoadingBox, Link } from 'govuk-react';
import { CoreDataLayout } from '../../../../layouts';
import { GovUKNotificationBanner, GovUKTag, GovUKTable, H1, H2, Paragraph } from '../../../../components/GovUKComponents';
import type { Column, FilterOption } from '../../../../components/GovUKComponents';
import {
  getServiceCategories,
  serviceCategoriesCacheKey,
  getWiderServiceCategoriesLookup,
  widerServiceCategoriesLookupCacheKey,
  ServiceCategoryListDto,
  ServiceCategoryStatus,
  WiderServiceCategoryLookup,
} from '../../../../components/Global/ServiceCategories';
import './styles.css';
import usePageTitle from '../../../../hooks/usePageTitle';

interface LocationState {
  notification?: {
    type: 'success' | 'error';
    title: string;
    message: string;
  };
}

interface CategoryWithStatus {
  code: string;
  name: string;
  hasData: boolean;
  status: ServiceCategoryStatus | null;
  serviceCategoryId: string | null;
}

const ServiceCategoriesList = (): React.ReactElement => {
  usePageTitle('Wider service categories');
  const navigate = useNavigate();
  const location = useLocation();
  const locationState = location.state as LocationState | null;

  const { data: lookupData, isLoading: isLoadingLookup } = useQuery({
    queryKey: widerServiceCategoriesLookupCacheKey(),
    queryFn: () => getWiderServiceCategoriesLookup(),
  });

  const { data: serviceCategoriesData, isLoading: isLoadingCategories } = useQuery({
    queryKey: serviceCategoriesCacheKey(),
    queryFn: () => getServiceCategories(),
  });

  const lookupCategories = lookupData?.data ?? [];
  const serviceCategories = serviceCategoriesData?.data ?? [];

  const categoriesWithStatus: CategoryWithStatus[] = useMemo(() => {
    const serviceCategoryMap = new Map<string, ServiceCategoryListDto>();
    serviceCategories.forEach((sc) => {
      serviceCategoryMap.set(sc.categoryCode, sc);
    });

    return lookupCategories.map((lookup: WiderServiceCategoryLookup) => {
      const existingCategory = serviceCategoryMap.get(lookup.value);
      return {
        code: lookup.value,
        name: lookup.description,
        hasData: !!existingCategory,
        status: existingCategory?.status ?? null,
        serviceCategoryId: existingCategory?.id ?? null,
      };
    });
  }, [lookupCategories, serviceCategories]);

  const handleAdd = (category: CategoryWithStatus) => {
    navigate('/organisation-admin/core-data/wider-service-categories/add', {
      state: {
        categoryCode: category.code,
        categoryName: category.name,
      },
    });
  };

  const handleView = (category: CategoryWithStatus) => {
    if (category.serviceCategoryId) {
      navigate(`/organisation-admin/core-data/wider-service-categories/${category.serviceCategoryId}`);
    }
  };

  const handleChange = (category: CategoryWithStatus) => {
    if (category.serviceCategoryId) {
      navigate(`/organisation-admin/core-data/wider-service-categories/${category.serviceCategoryId}/edit`);
    }
  };

  const handleDelete = (category: CategoryWithStatus) => {
    if (category.serviceCategoryId) {
      navigate(`/organisation-admin/core-data/wider-service-categories/${category.serviceCategoryId}/delete`);
    }
  };

  // const handleDownloadTemplate = async (format: TemplateFormat) => {
  //   try {
  //     const response = await downloadServiceCategoriesTemplate(format);
  //     const blob = new Blob([response.data]);
  //     const url = window.URL.createObjectURL(blob);
  //     const link = document.createElement('a');
  //     link.href = url;
  //     link.download = format === 'xlsx' ? 'wider_services_upload_template.xlsx' : 'wider_services_upload_template.csv';
  //     document.body.appendChild(link);
  //     link.click();
  //     document.body.removeChild(link);
  //     window.URL.revokeObjectURL(url);
  //   } catch (error) {
  //     console.error('Failed to download template:', error);
  //   }
  // };

  const isLoading = isLoadingLookup || isLoadingCategories;

  const columns: Column<CategoryWithStatus>[] = [
    {
      key: 'name',
      header: '',
      render: (category) => (
        <>
          <span style={{ fontWeight: 700 }}>{category.name}</span>
          {category.hasData && category.status === ServiceCategoryStatus.Draft && (
            <span style={{ marginLeft: '10px' }}>
              <GovUKTag colour="grey">Draft</GovUKTag>
            </span>
          )}
        </>
      ),
    },
    {
      key: 'actions',
      header: '',
      align: 'right',
      render: (category) =>
        category.hasData ? (
          <span style={{ display: 'flex', gap: '15px', justifyContent: 'flex-end' }}>
            <Link
              href="#"
              onClick={(e: React.MouseEvent) => {
                e.preventDefault();
                handleView(category);
              }}>
              View<span className="govuk-visually-hidden"> {category.name}</span>
            </Link>
            <Link
              href="#"
              onClick={(e: React.MouseEvent) => {
                e.preventDefault();
                handleChange(category);
              }}>
              Change<span className="govuk-visually-hidden"> {category.name}</span>
            </Link>
            <Link
              href="#"
              onClick={(e: React.MouseEvent) => {
                e.preventDefault();
                handleDelete(category);
              }}>
              Delete<span className="govuk-visually-hidden"> {category.name}</span>
            </Link>
          </span>
        ) : (
          <Link
            href="#"
            onClick={(e: React.MouseEvent) => {
              e.preventDefault();
              handleAdd(category);
            }}>
            Add<span className="govuk-visually-hidden"> {category.name}</span>
          </Link>
        ),
    },
  ];

  const sortOptions: FilterOption[] = [
    { value: 'name-asc', label: 'Alphabetically A-Z' },
    { value: 'name-desc', label: 'Alphabetically Z-A' },
  ];

  return (
    <LoadingBox loading={isLoading}>
      <CoreDataLayout breadcrumbs={[{ label: 'Home', link: '/organisation-admin/home' }]}>
        {locationState?.notification && (
          <GovUKNotificationBanner type={locationState.notification.type === 'error' ? 'important' : 'success'} title={locationState.notification.title}>
            <p className="govuk-body">{locationState.notification.message}</p>
          </GovUKNotificationBanner>
        )}

        <H1>Manage wider services categories</H1>

        <Paragraph>This page lets you add, view, change or delete wider services offered by Family Hubs in your local authority.</Paragraph>

        {/* <Paragraph>You can add or update services in one of two ways:</Paragraph>
        <UnorderedList>
          <ListItem>Complete the online form to enter information for each wider service</ListItem>
          <ListItem>Upload a Comma Separated Values (CSV) file to give us the data for all services at once</ListItem>
        </UnorderedList>
        <Paragraph>You can also remove a service at any time to keep your information accurate.</Paragraph>

        <H2>How to add or update a wider service using the guided form</H2>
        <Paragraph>
          Select &apos;Add&apos; to add one wider service from the list of pre-defined categories to your list. Select &apos;Change&apos; to update a single wider service. The
          guided form will take you through the data you need to enter, provide relevant guidance and check for errors as you go.
        </Paragraph>
        <Paragraph>You can save your progress at any time and return later to complete adding a new service.</Paragraph>

        <H2>How to add or update services using a CSV</H2>
        <Paragraph>If you want to add or update multiple services at once, you can upload a service list using a Comma Separated Values file (CSV).</Paragraph>
        <Paragraph>
          Download the service list template as a CSV or a macro-enabled Excel file (XLSM) from this page. Open the template in a spreadsheet editor and add the details for one or
          more services.
        </Paragraph>
        <Paragraph>When you&apos;re finished, save the file as a CSV and upload it by selecting &apos;Upload with CSV&apos;.</Paragraph>

        <div className="service-categories-files__item">
          <div className="service-categories-files__icon">
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
          <div className="service-categories-files__content">
            <p className="govuk-body govuk-!-margin-bottom-1">
              <strong>Wider service list</strong>
              <br />
              <span className="govuk-body-s">Template</span>
            </p>
            <div className="service-categories-files__downloads">
              <a
                href="#"
                className="govuk-link"
                onClick={(e: React.MouseEvent) => {
                  e.preventDefault();
                  handleDownloadTemplate('csv');
                }}>
                Download in CSV
              </a>
              <a
                href="#"
                className="govuk-link"
                onClick={(e: React.MouseEvent) => {
                  e.preventDefault();
                  handleDownloadTemplate('xlsx');
                }}>
                Download in Excel
              </a>
            </div>
          </div>
        </div> */}

        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <H2 style={{ marginBottom: 0 }}>Wider services category</H2>
          <div style={{ display: 'flex', gap: '10px' }}>
            <button type="button" className="govuk-button govuk-button--secondary" style={{ marginBottom: 0 }}>
              Upload with CSV
            </button>
          </div>
        </div>

        <GovUKTable<CategoryWithStatus>
          data={categoriesWithStatus}
          columns={columns}
          searchPlaceholder="Search category by name"
          searchable={true}
          sortOptions={sortOptions}
          sortLabel="Sort categories"
          keyExtractor={(category) => category.code}
          emptyMessage="No categories found"
          hideHeader={true}
        />
      </CoreDataLayout>
    </LoadingBox>
  );
};

export default ServiceCategoriesList;
