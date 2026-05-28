import { capitaliseFirst } from '../../../../../helpers/stringUtils';
import { LocationReducerAction, Location } from './components/types';

export const locationReducer = (state: Location, action: { type: LocationReducerAction; value: any }): Location => {
  switch (action.type) {
    case LocationReducerAction.INIT:
      const {
        name,
        deliverySiteName,
        postCode,
        referenceNumber,
        addressLine1,
        addressLine2,
        townOrCity,
        county,
        statusOfSite,
        typeOfSite,
        nameChange,
        dateOpened,
        bsfhBranding,
        locationType,
        clarificationComments,
        isActive,
      } = action.value;
      return {
        name: name || '',
        deliverySiteName: deliverySiteName || '',
        postCode: postCode || '',
        referenceNumber: referenceNumber || '',
        addressLine1: addressLine1 || '',
        addressLine2: addressLine2 || '',
        townOrCity: townOrCity || '',
        county: county || '',
        statusOfSite: statusOfSite || '',
        typeOfSite: typeOfSite || '',
        nameChange: nameChange || false,
        dateOpened: formatDateToString(dateOpened),
        bsfhBranding: bsfhBranding || '',
        locationType: locationType || [],
        clarificationComments: clarificationComments || '',
        isActive: isActive ?? true,
      };
    case LocationReducerAction.NAME:
      return { ...state, name: capitaliseFirst(action.value) };
    case LocationReducerAction.DELIVERY_SITE_NAME:
      return { ...state, deliverySiteName: action.value };
    case LocationReducerAction.POST_CODE:
      return { ...state, postCode: action.value };
    case LocationReducerAction.REFERENCE_NUMBER:
      return { ...state, referenceNumber: action.value };
    case LocationReducerAction.ADDRESS_LINE1:
      return { ...state, addressLine1: action.value };
    case LocationReducerAction.ADDRESS_LINE2:
      return { ...state, addressLine2: action.value };
    case LocationReducerAction.TOWN_OR_CITY:
      return { ...state, townOrCity: action.value };
    case LocationReducerAction.COUNTY:
      return { ...state, county: action.value };
    case LocationReducerAction.STATUS_OF_SITE:
      return { ...state, statusOfSite: action.value };
    case LocationReducerAction.TYPE_OF_SITE:
      return { ...state, typeOfSite: action.value };
    case LocationReducerAction.NAME_CHANGE:
      return { ...state, nameChange: action.value };
    case LocationReducerAction.DATE_OPENED:
      return { ...state, dateOpened: action.value };
    case LocationReducerAction.BSFH_BRANDING:
      return { ...state, bsfhBranding: action.value };
    case LocationReducerAction.LOCATION_TYPE:
      return { ...state, locationType: action.value };
    case LocationReducerAction.CLARIFICATION_COMMENTS:
      return { ...state, clarificationComments: action.value };
    case LocationReducerAction.ACTIVE:
      return { ...state, isActive: action.value };
    default:
      throw new Error();
  }
};

export const initialLocationState: Location = {
  name: '',
  deliverySiteName: '',
  postCode: '',
  referenceNumber: '',
  addressLine1: '',
  addressLine2: '',
  townOrCity: '',
  county: '',
  statusOfSite: '',
  typeOfSite: '',
  nameChange: false,
  dateOpened: '',
  bsfhBranding: '',
  locationType: [],
  clarificationComments: '',
  isActive: true,
};

const formatDateToString = (input?: Date | string | null): string => {
  if (!input) return '';

  // If already a string in ISO format, return as-is
  if (typeof input === 'string') {
    // Handle full ISO timestamp - extract date part only
    if (input.includes('T')) {
      return input.split('T')[0];
    }
    return input;
  }

  // Convert Date to ISO string format (YYYY-MM-DD)
  if (input instanceof Date && !isNaN(input.getTime()) && input.getFullYear() !== 1) {
    const year = input.getFullYear();
    const month = (input.getMonth() + 1).toString().padStart(2, '0');
    const day = input.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  return '';
};
