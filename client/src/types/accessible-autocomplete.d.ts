declare module 'accessible-autocomplete' {
  interface AutocompleteOptions {
    element: HTMLElement;
    id: string;
    name?: string;
    source: (query: string, populateResults: (results: string[]) => void) => void;
    defaultValue?: string;
    placeholder?: string;
    showNoOptionsFound?: boolean;
    autoselect?: boolean;
    confirmOnBlur?: boolean;
    minLength?: number;
    displayMenu?: 'inline' | 'overlay';
    onConfirm?: (confirmed: string | undefined) => void;
    templates?: {
      inputValue?: (result: string | undefined) => string;
      suggestion?: (result: string | undefined) => string;
    };
    cssNamespace?: string;
    showAllValues?: boolean;
    dropdownArrow?: (config: { className: string }) => string;
  }

  function accessibleAutocomplete(options: AutocompleteOptions): void;

  export = accessibleAutocomplete;
}

declare module 'accessible-autocomplete/react' {
  import { ComponentType } from 'react';

  interface AutocompleteProps {
    id: string;
    source: string[] | ((query: string, populateResults: (results: string[]) => void) => void);
    defaultValue?: string;
    placeholder?: string;
    showNoOptionsFound?: boolean;
    autoselect?: boolean;
    confirmOnBlur?: boolean;
    minLength?: number;
    displayMenu?: 'inline' | 'overlay';
    onConfirm?: (confirmed: string | undefined) => void;
    templates?: {
      inputValue?: (result: string | undefined) => string;
      suggestion?: (result: string | undefined) => string;
    };
    cssNamespace?: string;
    showAllValues?: boolean;
  }

  const Autocomplete: ComponentType<AutocompleteProps>;
  export default Autocomplete;
}
