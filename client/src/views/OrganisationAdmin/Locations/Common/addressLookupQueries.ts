import axios from 'axios';

export interface OsPlacesAddress {
  uprn: string;
  address: string;
  buildingName?: string;
  buildingNumber?: string;
  thoroughfareName?: string;
  dependentLocality?: string;
  postTown: string;
  postcode: string;
  organisationName?: string;
}

export interface ParsedAddress {
  addressLine1: string;
  addressLine2: string;
  townOrCity: string;
  county: string;
  uprn: string;
}

export const lookupAddressByPostcode = (postcode: string) => axios.get<OsPlacesAddress[]>('/address-lookup', { params: { postcode } });

export const addressLookupCacheKey = (postcode: string) => ['address-lookup', postcode];

export const parseOsPlacesAddress = (address: OsPlacesAddress): ParsedAddress => {
  let addressLine1 = '';
  let addressLine2 = '';

  if (address.buildingName) {
    addressLine1 = address.buildingName;
    const streetParts = [address.buildingNumber, address.thoroughfareName].filter(Boolean).join(' ');
    addressLine2 = streetParts || address.dependentLocality || '';
  } else {
    const streetParts = [address.buildingNumber, address.thoroughfareName].filter(Boolean).join(' ');
    addressLine1 = streetParts;
    addressLine2 = address.dependentLocality || '';
  }

  const townOrCity = address.postTown || '';
  const county = addressLine2 === address.dependentLocality ? '' : address.dependentLocality || '';

  return {
    addressLine1,
    addressLine2,
    townOrCity,
    county,
    uprn: address.uprn,
  };
};
