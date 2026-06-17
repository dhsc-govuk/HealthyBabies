import React, { useCallback, useMemo } from 'react';
import { GovUKFieldset, ErrorSummary, GovUKDateField } from '../../../../../../../components/GovUKComponents';
import { GlobalDataDto } from '../../../../../../../components/Global/Queries/globalData';
import { LocationReducerAction, Location, ValidationErrors, validationErrors, bsfhBrandingOptions, statusOfSiteOptions } from '../types';

interface Props {
  location: Location;
  errors: ValidationErrors;
  setErrors: React.Dispatch<React.SetStateAction<ValidationErrors>>;
  dispatch: React.Dispatch<{ type: LocationReducerAction; value: any }>;
  locationTypes: GlobalDataDto[];
  siteTypes: GlobalDataDto[];
}

const StepDetails = ({ location, errors, setErrors, dispatch, locationTypes, siteTypes }: Props): React.ReactElement => {
  const locationTypeOptions = useMemo(() => locationTypes.map((item) => ({ value: item.value, text: item?.description ?? item.value })), [locationTypes]);

  const typeOfSiteOptions = useMemo(() => siteTypes.map((item) => ({ value: item.value, text: item?.description ?? item.value })), [siteTypes]);

  const validate = useCallback(
    (field: string) => {
      const errorItems: Record<string, string | boolean> = {};

      switch (field) {
        case 'deliverySiteName':
          errorItems.deliverySiteName = !location.deliverySiteName ? validationErrors.deliverySiteName : false;
          break;
        case 'postCode':
          errorItems.postCode = !location.postCode ? validationErrors.postCode : false;
          break;
        case 'referenceNumber':
          errorItems.referenceNumber = !location.referenceNumber ? validationErrors.referenceNumber : false;
          break;
        case 'statusOfSite':
          errorItems.statusOfSite = !location.statusOfSite ? validationErrors.statusOfSite : false;
          break;
        case 'typeOfSite':
          errorItems.typeOfSite = !location.typeOfSite ? validationErrors.typeOfSite : false;
          break;
        case 'dateOpened':
          errorItems.dateOpened = !location.dateOpened ? validationErrors.dateOpened : false;
          break;
        case 'bsfhBranding':
          errorItems.bsfhBranding = !location.bsfhBranding ? validationErrors.bsfhBranding : false;
          break;
        case 'locationType':
          errorItems.locationType = !location.locationType || location.locationType.length === 0 ? validationErrors.locationType : false;
          break;
      }

      setErrors({ ...errors, ...errorItems });
    },
    [location, errors, setErrors]
  );

  const errorList = useMemo(() => {
    const list: { targetName: string; text: string }[] = [];
    if (errors.deliverySiteName) list.push({ targetName: 'delivery-site-name', text: String(errors.deliverySiteName) });
    if (errors.postCode) list.push({ targetName: 'post-code', text: String(errors.postCode) });
    if (errors.referenceNumber) list.push({ targetName: 'reference-number', text: String(errors.referenceNumber) });
    if (errors.statusOfSite) list.push({ targetName: 'status-of-site-fieldset', text: String(errors.statusOfSite) });
    if (errors.typeOfSite) list.push({ targetName: 'type-of-site-fieldset', text: String(errors.typeOfSite) });
    if (errors.dateOpened) list.push({ targetName: 'date-opened', text: String(errors.dateOpened) });
    if (errors.bsfhBranding) list.push({ targetName: 'bsfh-branding-fieldset', text: String(errors.bsfhBranding) });
    if (errors.locationType) list.push({ targetName: 'location-type-fieldset', text: String(errors.locationType) });
    return list;
  }, [errors]);

  // Transform your existing option arrays to match the new component interfaces
  const nameChangeOptions = [
    { value: 'yes', text: 'Yes' },
    { value: 'no', text: 'No' },
  ];

  const handleLocationTypeChange = (value: string, checked: boolean, allValues: string[]) => {
    dispatch({ type: LocationReducerAction.LOCATION_TYPE, value: allValues });
  };

  const handleNameChange = (value: string) => {
    const booleanValue = value === 'yes';
    dispatch({ type: LocationReducerAction.NAME_CHANGE, value: booleanValue });
  };

  return (
    <>
      {errorList.length > 0 && <ErrorSummary errors={errorList} />}

      {/* Basic Information */}
      <GovUKFieldset legend="" legendSize="s">
        <GovUKFieldset.Input
          id="delivery-site-name"
          name="deliverySiteName"
          label="What is the official, public name of the site?"
          hint="Enter the full official name as it appears publicly"
          value={location.deliverySiteName}
          error={errors.deliverySiteName ? String(errors.deliverySiteName) : undefined}
          required
          onChange={(e) => dispatch({ type: LocationReducerAction.DELIVERY_SITE_NAME, value: e.target.value })}
          onKeyUp={() => validate('deliverySiteName')}
          onBlur={() => validate('deliverySiteName')}
        />

        <GovUKFieldset.Input
          id="post-code"
          name="postCode"
          label="Postcode"
          hint="Enter the postcode for this location"
          value={location.postCode}
          error={errors.postCode ? String(errors.postCode) : undefined}
          required
          onChange={(e) => dispatch({ type: LocationReducerAction.POST_CODE, value: e.target.value })}
          onKeyUp={() => validate('postCode')}
          onBlur={() => validate('postCode')}
        />

        <GovUKFieldset.Input
          id="address-line-1"
          name="addressLine1"
          label="Address line 1"
          value={location.addressLine1}
          onChange={(e) => dispatch({ type: LocationReducerAction.ADDRESS_LINE1, value: e.target.value })}
        />

        <GovUKFieldset.Input
          id="address-line-2"
          name="addressLine2"
          label="Address line 2 (optional)"
          value={location.addressLine2}
          onChange={(e) => dispatch({ type: LocationReducerAction.ADDRESS_LINE2, value: e.target.value })}
        />

        <GovUKFieldset.Input
          id="town-or-city"
          name="townOrCity"
          label="Town or city"
          value={location.townOrCity}
          onChange={(e) => dispatch({ type: LocationReducerAction.TOWN_OR_CITY, value: e.target.value })}
        />

        <GovUKFieldset.Input
          id="county"
          name="county"
          label="County (optional)"
          value={location.county}
          onChange={(e) => dispatch({ type: LocationReducerAction.COUNTY, value: e.target.value })}
        />

        <GovUKFieldset.Input
          id="reference-number"
          name="referenceNumber"
          label="What is the Unique Property Reference Number (UPRN) of the Site?"
          hint="Enter the UPRN - this is a unique identifier for the property"
          value={location.referenceNumber}
          error={errors.referenceNumber ? String(errors.referenceNumber) : undefined}
          required
          type="number"
          onChange={(e) => dispatch({ type: LocationReducerAction.REFERENCE_NUMBER, value: e.target.value })}
          onKeyUp={() => validate('referenceNumber')}
          onBlur={() => validate('referenceNumber')}
        />
      </GovUKFieldset>

      {/* Site Status and Type */}
      <GovUKFieldset legend="" legendSize="s">
        <GovUKFieldset.RadioGroup
          name="statusOfSite"
          legend="Is this site currently active?"
          labelSize="s"
          legendSize="s"
          options={statusOfSiteOptions}
          value={location.statusOfSite}
          error={errors.statusOfSite ? String(errors.statusOfSite) : undefined}
          onChange={(value) => {
            dispatch({ type: LocationReducerAction.STATUS_OF_SITE, value });
          }}
        />

        <GovUKFieldset.RadioGroup
          name="typeOfSite"
          legend="What type of site is this?"
          labelSize="s"
          options={typeOfSiteOptions}
          value={location.typeOfSite}
          error={errors.typeOfSite ? String(errors.typeOfSite) : undefined}
          onChange={(value) => {
            dispatch({ type: LocationReducerAction.TYPE_OF_SITE, value });
          }}
        />

        <GovUKFieldset.RadioGroup
          name="nameChange"
          legend="Has the site changed name?"
          labelSize="s"
          options={nameChangeOptions}
          value={location.nameChange === true ? 'yes' : location.nameChange === false ? 'no' : undefined}
          onChange={handleNameChange}
        />

        <GovUKDateField
          id="date-opened"
          legend="When was the site officially opened as a Family Hub, Best Start Family Hub or linked site?"
          hint="Enter the date the site was officially opened (DD/MM/YYYY)"
          error={errors.dateOpened ? String(errors.dateOpened) : undefined}
          value={location?.dateOpened ?? ''}
          onChange={(dateString: string) => {
            dispatch({ type: LocationReducerAction.DATE_OPENED, value: dateString });
          }}
        />
      </GovUKFieldset>

      {/* Branding and Location Type */}
      <GovUKFieldset legend="" legendSize="l">
        <GovUKFieldset.RadioGroup
          name="bsfhBranding"
          legend="Has the site introduced branded banners, stickers and other such means to clearly associate your sites with the Best Start in Life campaign, noting it is funded by the UK government?"
          options={bsfhBrandingOptions}
          value={location.bsfhBranding}
          error={errors.bsfhBranding ? String(errors.bsfhBranding) : undefined}
          onChange={(value) => {
            dispatch({ type: LocationReducerAction.BSFH_BRANDING, value });
          }}
        />

        <GovUKFieldset.CheckboxGroup
          name="locationType"
          legend="Is the site co-located with other community functions? Please select all that apply"
          options={locationTypeOptions}
          value={location.locationType || []}
          error={errors.locationType ? String(errors.locationType) : undefined}
          onChange={handleLocationTypeChange}
        />
      </GovUKFieldset>

      {/* Additional Information */}
      <GovUKFieldset legend="" legendSize="s">
        <GovUKFieldset.Textarea
          id="clarification-comments"
          name="clarificationComments"
          label="Please provide any relevant additional information"
          labelSize="s"
          hint="For example if one of the questions doesn't properly capture the information about a Family Hub site. Please only add comments in this section if necessary."
          value={location.clarificationComments}
          rows={4}
          onChange={(e) => dispatch({ type: LocationReducerAction.CLARIFICATION_COMMENTS, value: e.target.value })}
        />

        <GovUKFieldset.Checkbox
          id="is-active"
          name="isActive"
          label="Active in system"
          hint="Select if this location should be active in the system"
          checked={location.isActive}
          onChange={(e) => dispatch({ type: LocationReducerAction.ACTIVE, value: e.target.checked })}
        />
      </GovUKFieldset>
    </>
  );
};

export default StepDetails;
