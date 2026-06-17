import { capitaliseFirst } from '../../../../helpers/stringUtils';

export interface OrganisationContact {
  name: string;
  email: string;
  role: string;
}

export interface ValidationErrors {
  name: string | boolean;
  email: string | boolean;
  role: string | boolean;
}

export const validationErrors: ValidationErrors = {
  name: 'Please provide contact name',
  email: 'Please provide a valid email address',
  role: 'Please provide contact role',
};

export enum ContactReducerAction {
  NAME,
  EMAIL,
  ROLE,
  INIT,
}

export const contactReducer = (state: OrganisationContact, action: { type: ContactReducerAction; value: any }): OrganisationContact => {
  switch (action.type) {
    case ContactReducerAction.NAME:
      return { ...state, name: capitaliseFirst(action.value) };
    case ContactReducerAction.EMAIL:
      return { ...state, email: action.value };
    case ContactReducerAction.ROLE:
      return { ...state, role: action.value };
    case ContactReducerAction.INIT:
      const { name, email, role } = action.value;
      return { name, email, role };
    default:
      throw new Error();
  }
};

export const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};
