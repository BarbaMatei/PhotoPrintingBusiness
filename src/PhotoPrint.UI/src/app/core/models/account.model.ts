export interface AccountDto {
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  hasPassword: boolean;
  linkedProviders: string[];
  deletionRequested: boolean;
}

export interface UpdateAccountRequest {
  firstName: string;
  lastName: string;
  phone: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface SavedAddressDto {
  id: string;
  label: string;
  fullName: string;
  phone: string;
  addressLine: string;
  city: string;
  county: string;
  postalCode: string;
  isDefault: boolean;
}

export interface SavedAddressRequest {
  label: string;
  fullName: string;
  phone: string;
  addressLine: string;
  city: string;
  county: string;
  postalCode: string;
  isDefault: boolean;
}
