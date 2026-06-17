import { capitaliseFirst } from '../../../../../helpers/stringUtils';

export type OrganisationUserRole = (typeof organisationRoles)[keyof typeof organisationRoles];

export const organisationRoles = {
  ORGANISATION_ADMIN: 'organisation admin',
} as const;

export const organisationUserRoleOptions = [
  { value: organisationRoles.ORGANISATION_ADMIN, label: 'LA Admin' },
];

export interface User {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
}

export interface ValidationErrors {
  firstName: string | boolean;
  lastName: string | boolean;
  email: string | boolean;
}

export const validationErrors: ValidationErrors = {
  firstName: 'Please provide a first name',
  lastName: 'Please provide a last name',
  email: 'Please provide a valid email address',
};

export enum UserReducerAction {
  FIRST_NAME,
  LAST_NAME,
  EMAIL,
  ACTIVE,
  ROLE,
  INIT,
}

export const userReducer = (state: User, action: { type: UserReducerAction; value: any }): User => {
  switch (action.type) {
    case UserReducerAction.FIRST_NAME:
      return { ...state, firstName: capitaliseFirst(action.value) };
    case UserReducerAction.LAST_NAME:
      return { ...state, lastName: capitaliseFirst(action.value) };
    case UserReducerAction.EMAIL:
      return { ...state, email: action.value?.toLowerCase() };
    case UserReducerAction.ACTIVE:
      return { ...state, isActive: action.value };
    case UserReducerAction.ROLE:
      return {
        ...state,
        role: action.value,
      };
    case UserReducerAction.INIT:
      const { firstName, lastName, email, isActive, role } = action.value;
      const obj = {
        firstName,
        lastName,
        email,
        isActive,
        role,
      };
      return { ...(obj as User) };
    default:
      throw new Error();
  }
};
