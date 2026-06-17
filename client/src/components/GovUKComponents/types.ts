export enum ViewToggleType {
  TABLE = 'table',
  GRID = 'grid',
}

export interface EmptyObject {
  [key: string]: never;
}

export interface UnknownObject {
  [key: string]: unknown;
}
