import { capitaliseFirst } from '../../../../helpers/stringUtils';

export interface Organisation {
  name: string;
  onsCode: string;
  isActive: boolean;
}

export interface ValidationErrors {
  name: string | boolean;
  onsCode: string | boolean;
}

export const validationErrors: ValidationErrors = {
  name: 'Please provide local authority name',
  onsCode: 'Please provide ONS code',
};

export enum OrganisationReducerAction {
  NAME,
  ONS_CODE,
  ACTIVE,
  INIT,
}

export const organisationReducer = (state: Organisation, action: { type: OrganisationReducerAction; value: any }): Organisation => {
  switch (action.type) {
    case OrganisationReducerAction.NAME:
      return { ...state, name: capitaliseFirst(action.value) };
    case OrganisationReducerAction.ONS_CODE:
      return { ...state, onsCode: action.value };
    case OrganisationReducerAction.ACTIVE:
      return { ...state, isActive: action.value };
    case OrganisationReducerAction.INIT:
      const { name, onsCode, isActive } = action.value;
      const obj = {
        name,
        onsCode: onsCode ?? '',
        isActive,
      };
      return { ...(obj as Organisation) };
    default:
      throw new Error();
  }
};
