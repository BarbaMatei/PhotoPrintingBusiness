import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export interface PasswordStrengthErrors {
  minLength?: boolean;
  uppercase?: boolean;
  digit?: boolean;
  special?: boolean;
}

/**
 * Validates password strength: min 8 chars, 1 uppercase, 1 digit, 1 special char.
 * Returns a map of which rules failed (for per-rule UI feedback).
 */
export const passwordStrengthValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value: string = control.value ?? '';

  const errors: PasswordStrengthErrors = {};

  if (value.length < 8) errors.minLength = true;
  if (!/[A-Z]/.test(value)) errors.uppercase = true;
  if (!/[0-9]/.test(value)) errors.digit = true;
  if (!/[^A-Za-z0-9]/.test(value)) errors.special = true;

  return Object.keys(errors).length > 0 ? { passwordStrength: errors } : null;
};

/** Cross-field validator: confirmPassword must match password. */
export const passwordMatchValidator: ValidatorFn = (
  group: AbstractControl,
): ValidationErrors | null => {
  const password = group.get('password')?.value ?? '';
  const confirm = group.get('confirmPassword')?.value ?? '';
  return password !== confirm ? { passwordMismatch: true } : null;
};
