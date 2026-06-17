type DateLike = Date | string | number;

export interface Attributes {
  sub: string;
  // eslint-disable-next-line camelcase
  given_name: string;
  // eslint-disable-next-line camelcase
  family_name: string;
  email: string;
  // eslint-disable-next-line camelcase
  email_verified?: boolean;
  // eslint-disable-next-line camelcase
  phone_number_verified?: boolean;
  birthdate?: DateLike;
  address?: string;
}

export interface UserInfo {
  id?: string;
  username: string;
  attributes: Attributes;
}

export interface User {
  id: string;
  email: string;
  first_name: string;
  last_name: string;
  full_name: string;
  user_type: string;
  is_approver: boolean;
  sub_id: string;
  organisation_id: string;
  location_id: string;
  organisation_name: string;
}

export interface OrganisationSettingDto {
  enableMaculopathy: boolean;
}

export interface AdminUser extends User {}
