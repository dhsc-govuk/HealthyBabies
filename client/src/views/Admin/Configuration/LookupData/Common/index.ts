export interface LookupData {
  entity: string;
  value: string;
  description: string;
}

export interface ValidationErrors {
  entity: string | boolean;
  value: string | boolean;
}

export const validationErrors = {
  entity: 'Entity is required',
  value: 'Value is required',
};

export enum LookupDataReducerAction {
  ENTITY = 'ENTITY',
  VALUE = 'VALUE',
  DESCRIPTION = 'DESCRIPTION',
  INIT = 'INIT',
}

export const lookupDataReducer = (
  state: LookupData,
  action: { type: LookupDataReducerAction; value: any }
): LookupData => {
  switch (action.type) {
    case LookupDataReducerAction.ENTITY:
      return { ...state, entity: action.value };
    case LookupDataReducerAction.VALUE:
      return { ...state, value: action.value };
    case LookupDataReducerAction.DESCRIPTION:
      return { ...state, description: action.value };
    case LookupDataReducerAction.INIT:
      return action.value;
    default:
      return state;
  }
};
