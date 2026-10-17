import { type ValidatorFunction } from '@/shared/types';

export const CATEGORY_NAME_MAX_LENGTH = 64;

export const validateCategoryName: ValidatorFunction<string> = value => {
  const { length } = value.trim();
  return length >= 1 && length <= CATEGORY_NAME_MAX_LENGTH;
};
