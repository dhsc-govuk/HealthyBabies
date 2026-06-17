import { capitaliseFirst } from '../../../helpers/stringUtils';

export interface ListItem {
  id: string;
  fullName: string;
  email: string;
  active: boolean;
}

export interface User {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role?: string;
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
    case UserReducerAction.INIT:
      const { firstName, lastName, email, isActive } = action.value;
      const obj = {
        firstName,
        lastName,
        email,
        isActive,
      };
      return { ...(obj as User) };
    default:
      throw new Error();
  }
};
