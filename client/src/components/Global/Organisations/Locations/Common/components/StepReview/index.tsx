import React, { useMemo } from 'react';
import { SummaryList } from '../../../../../../../components/GovUKComponents';
import { booleanToYesNo } from '../../../../../../../helpers/stringUtils';
import { GlobalDataDto } from '../../../../../../../components/Global/Queries/globalData';
import { Location, statusOfSiteOptions, bsfhBrandingOptions } from '../types';

interface Props {
  location: Location;
  setStep: React.Dispatch<React.SetStateAction<number>>;
  locationTypes: GlobalDataDto[];
  siteTypes: GlobalDataDto[];
}

const StepReview = ({ location, setStep, locationTypes, siteTypes }: Props): React.ReactElement => {
  const locationTypeOptions = useMemo(
    () => locationTypes.map((item) => ({ value: item.value, text: item.value })),
    [locationTypes]
  );

  const typeOfSiteOptions = useMemo(
    () => siteTypes.map((item) => ({ value: item.value, text: item.value })),
    [siteTypes]
  );

  // Helper function to get display text for dropdown values
  const getDisplayText = (value: string, options: Array<{ value: string; text: string }>) => {
    const option = options.find((opt) => opt.value === value);
    return option ? option.text : value;
  };

  // Helper function to format location types
  const formatLocationTypes = (types: string[]) => {
    if (!types || types.length === 0) return 'None selected';
    return types.map((type) => getDisplayText(type, locationTypeOptions)).join(', ');
  };

  // Helper function to format date for display (YYYY-MM-DD to DD/MM/YYYY)
  const formatDateForDisplay = (dateString: string) => {
    if (!dateString) return 'Not provided';
    const parts = dateString.split('-');
    if (parts.length === 3) {
      return `${parts[2]}/${parts[1]}/${parts[0]}`;
    }
    return dateString;
  };

  const summaryItems = [
    {
      label: 'Official public name',
      value: location.deliverySiteName || 'Not provided',
      onAction: () => setStep(0),
    },
    {
      label: 'Postcode',
      value: location.postCode || 'Not provided',
      onAction: () => setStep(0),
    },
    {
      label: 'Unique Property Reference Number (UPRN)',
      value: location.referenceNumber || 'Not provided',
      onAction: () => setStep(0),
    },
    {
      label: 'Delivery location status',
      value: location.statusOfSite ? getDisplayText(location.statusOfSite, statusOfSiteOptions) : 'Not selected',
      onAction: () => setStep(0),
    },
    {
      label: 'Type of delivery location',
      value: location.typeOfSite ? getDisplayText(location.typeOfSite, typeOfSiteOptions) : 'Not selected',
      onAction: () => setStep(0),
    },
    {
      label: 'Has the delivery location changed name?',
      value: booleanToYesNo(location.nameChange),
      onAction: () => setStep(0),
    },
    {
      label: 'Date officially opened',
      value: formatDateForDisplay(location.dateOpened),
      onAction: () => setStep(0),
    },
    {
      label: 'Best Start for Life branding introduced',
      value: location.bsfhBranding ? getDisplayText(location.bsfhBranding, bsfhBrandingOptions) : 'Not selected',
      onAction: () => setStep(0),
    },
    {
      label: 'Co-located community functions',
      value: formatLocationTypes(location.locationType),
      onAction: () => setStep(0),
    },
    {
      label: 'Additional comments',
      value: location.clarificationComments || 'None provided',
      onAction: () => setStep(0),
    },
    {
      label: 'Active in system',
      value: booleanToYesNo(location.isActive),
      onAction: () => setStep(0),
    },
  ];

  return (
    <>
      <p>
        Check the information you've entered before saving the site. Make sure the details are accurate and complete. You can go back to make changes if anything needs updating.
      </p>
      <SummaryList items={summaryItems} />
    </>
  );
};

export default StepReview;
