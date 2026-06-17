import React, { useState } from 'react';
import { useQuery } from 'react-query';
import { GovUKButton } from '../../../../components/GovUKComponents';
import { OsPlacesAddress, ParsedAddress, lookupAddressByPostcode, addressLookupCacheKey, parseOsPlacesAddress } from './addressLookupQueries';

interface AddressLookupProps {
  postcode: string;
  onAddressSelected: (address: ParsedAddress) => void;
}

const AddressLookup = ({ postcode, onAddressSelected }: AddressLookupProps): React.ReactElement => {
  const [searchPostcode, setSearchPostcode] = useState('');
  const [selectedUprn, setSelectedUprn] = useState('');
  const [showManualEntry, setShowManualEntry] = useState(false);

  const {
    data: addressesResponse,
    isLoading,
    isError,
    refetch,
    isFetched,
  } = useQuery(addressLookupCacheKey(searchPostcode), () => lookupAddressByPostcode(searchPostcode), {
    enabled: false,
    staleTime: 5 * 60 * 1000,
  });

  const addresses = addressesResponse?.data || [];

  const handleFindAddress = () => {
    if (!postcode.trim()) return;
    setSearchPostcode(postcode.trim());
    setSelectedUprn('');
    setShowManualEntry(false);
    setTimeout(() => refetch(), 0);
  };

  const handleSelectAddress = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const uprn = e.target.value;
    setSelectedUprn(uprn);

    if (!uprn) return;

    const selected = addresses.find((a: OsPlacesAddress) => a.uprn === uprn);
    if (selected) {
      const parsed = parseOsPlacesAddress(selected);
      onAddressSelected(parsed);
    }
  };

  const handleManualEntry = (e: React.MouseEvent) => {
    e.preventDefault();
    setShowManualEntry(true);
    onAddressSelected({
      addressLine1: '',
      addressLine2: '',
      townOrCity: '',
      county: '',
      uprn: '',
    });
  };

  return (
    <div className="govuk-!-margin-top-2">
      <GovUKButton
        onClick={handleFindAddress}
        disabled={!postcode.trim()}
        isLoading={isLoading}
        style={{ backgroundColor: '#1d70b8', boxShadow: '0 2px 0 #003078' }}
      >
        Find address
      </GovUKButton>

      {isError && (
        <p className="govuk-error-message govuk-!-margin-top-2">
          <span className="govuk-visually-hidden">Error:</span> Could not look up addresses. Please try again or enter your address manually.
        </p>
      )}

      {isFetched && addresses.length > 0 && (
        <div className="govuk-form-group govuk-!-margin-top-3">
          <label className="govuk-label" htmlFor="address-lookup-select">
            Select an address
          </label>
          <select className="govuk-select" id="address-lookup-select" name="address-lookup-select" value={selectedUprn} onChange={handleSelectAddress}>
            <option value="">{addresses.length} addresses found</option>
            {addresses.map((addr: OsPlacesAddress) => (
              <option key={addr.uprn} value={addr.uprn}>
                {addr.address}
              </option>
            ))}
          </select>
        </div>
      )}

      {isFetched && addresses.length === 0 && !isError && <p className="govuk-body govuk-!-margin-top-2">No addresses found for this postcode.</p>}

      {isFetched && !showManualEntry && (
        <p className="govuk-body govuk-!-margin-top-2">
          <button type="button" className="govuk-link" style={{ background: 'none', border: 'none', padding: 0, cursor: 'pointer' }} onClick={handleManualEntry}>
            I can&apos;t find my address in the list
          </button>
        </p>
      )}
    </div>
  );
};

export default AddressLookup;
