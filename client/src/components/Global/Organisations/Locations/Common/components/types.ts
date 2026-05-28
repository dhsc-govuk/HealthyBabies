export interface Location {
  name: string;
  deliverySiteName: string;
  postCode: string;
  referenceNumber: string;
  addressLine1: string;
  addressLine2: string;
  townOrCity: string;
  county: string;
  statusOfSite: string;
  typeOfSite: string;
  nameChange: boolean;
  dateOpened: string;
  bsfhBranding: string;
  locationType: string[];
  clarificationComments: string;
  isActive: boolean;
}

export interface ValidationErrors {
  name: string | boolean;
  deliverySiteName: string | boolean;
  postCode: string | boolean;
  referenceNumber: string | boolean;
  statusOfSite: string | boolean;
  typeOfSite: string | boolean;
  dateOpened: string | boolean;
  bsfhBranding: string | boolean;
  locationType: string | boolean;
}
export enum LocationReducerAction {
  INIT,
  NAME,
  DELIVERY_SITE_NAME,
  POST_CODE,
  REFERENCE_NUMBER,
  ADDRESS_LINE1,
  ADDRESS_LINE2,
  TOWN_OR_CITY,
  COUNTY,
  STATUS_OF_SITE,
  TYPE_OF_SITE,
  NAME_CHANGE,
  DATE_OPENED,
  BSFH_BRANDING,
  LOCATION_TYPE,
  CLARIFICATION_COMMENTS,
  ACTIVE,
}

export const validationErrors: ValidationErrors = {
  name: 'Please provide location name',
  deliverySiteName: 'Please provide the official public name of the site',
  postCode: 'Please provide the postcode',
  referenceNumber: 'Please provide the Unique Property Reference Number',
  statusOfSite: 'Please select the site status',
  typeOfSite: 'Please select the type of site',
  dateOpened: 'Please provide the date the site was opened',
  bsfhBranding: 'Please select the BSFH branding status',
  locationType: 'Please select at least one location type',
};

export const statusOfSiteOptions = [
  { value: 'active', text: 'Active' },
  { value: 'inactive', text: 'Inactive' },
];

export const bsfhBrandingOptions = [
  { value: 'yes', text: 'Yes' },
  { value: 'no', text: 'No' },
];

export const locationTypeOptions = [
  { value: 'all-family-hub-sites', text: 'All Family Hubs sites' },
  { value: 'home-visits', text: 'Home visits' },
  { value: 'early-years-setting', text: 'Early years setting' },
  { value: 'hospital', text: 'Hospital' },
  { value: 'virtual', text: 'Virtual' },
  { value: 'family-hub-network-site', text: 'Family Hub Network site' },
  { value: 'other-locations-local-authority', text: 'Other locations in Local Authority' },
];

export const typeOfSiteOptions = [
  { value: 'family-hub', text: 'Family Hub' },
  { value: 'best-start-family-hub', text: 'Best Start Family Hub' },
  { value: 'linked-site', text: 'Linked Site' },
];
