import { FormControl, FormGroup } from '@angular/forms';
import {
  passwordStrengthValidator,
  passwordMatchValidator,
} from './password-strength.validator';

describe('passwordStrengthValidator', () => {
  function validate(value: string) {
    const ctrl = new FormControl(value);
    return passwordStrengthValidator(ctrl);
  }

  it('returns null for a fully strong password', () => {
    expect(validate('StrongP@ss1')).toBeNull();
  });

  it('returns null for minimum-valid password', () => {
    expect(validate('Abcdef1@')).toBeNull();
  });

  it('returns minLength error for password shorter than 8 characters', () => {
    const result = validate('Ab1@xyz');
    expect(result?.['passwordStrength']?.minLength).toBe(true);
  });

  it('returns uppercase error when password has no uppercase letter', () => {
    const result = validate('lowercase1@abc');
    expect(result?.['passwordStrength']?.uppercase).toBe(true);
  });

  it('does not return uppercase error when password has an uppercase letter', () => {
    const result = validate('Lowercase1@abc');
    expect(result?.['passwordStrength']?.uppercase).toBeUndefined();
  });

  it('returns digit error when password has no digit', () => {
    const result = validate('UpperCase@abc');
    expect(result?.['passwordStrength']?.digit).toBe(true);
  });

  it('does not return digit error when password has a digit', () => {
    const result = validate('UpperCase1@');
    expect(result?.['passwordStrength']?.digit).toBeUndefined();
  });

  it('returns special error when password has no special character', () => {
    const result = validate('Password1abc');
    expect(result?.['passwordStrength']?.special).toBe(true);
  });

  it('does not return special error when password has a special character', () => {
    const result = validate('Password1@');
    expect(result?.['passwordStrength']?.special).toBeUndefined();
  });

  it('returns multiple errors for a weak password', () => {
    const result = validate('abc');
    const errors = result?.['passwordStrength'];
    expect(errors?.minLength).toBe(true);
    expect(errors?.uppercase).toBe(true);
    expect(errors?.special).toBe(true);
    // has no digit
    expect(errors?.digit).toBe(true);
  });

  it('returns all errors for empty string', () => {
    const result = validate('');
    const errors = result?.['passwordStrength'];
    expect(errors?.minLength).toBe(true);
    expect(errors?.uppercase).toBe(true);
    expect(errors?.digit).toBe(true);
    expect(errors?.special).toBe(true);
  });

  it('treats null value as empty string', () => {
    const ctrl = new FormControl(null);
    const result = passwordStrengthValidator(ctrl);
    expect(result?.['passwordStrength']?.minLength).toBe(true);
  });
});

describe('passwordMatchValidator', () => {
  function buildGroup(password: string, confirmPassword: string) {
    return new FormGroup({
      password: new FormControl(password),
      confirmPassword: new FormControl(confirmPassword),
    });
  }

  it('returns null when passwords match', () => {
    expect(passwordMatchValidator(buildGroup('Pass1@abc', 'Pass1@abc'))).toBeNull();
  });

  it('returns passwordMismatch error when passwords differ', () => {
    const result = passwordMatchValidator(buildGroup('Pass1@abc', 'Different1@'));
    expect(result).toEqual({ passwordMismatch: true });
  });

  it('returns passwordMismatch when confirmPassword is empty', () => {
    const result = passwordMatchValidator(buildGroup('Pass1@abc', ''));
    expect(result).toEqual({ passwordMismatch: true });
  });

  it('returns passwordMismatch when both are empty strings of different values', () => {
    // same empty string → should match
    expect(passwordMatchValidator(buildGroup('', ''))).toBeNull();
  });
});
