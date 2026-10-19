import { MSW_ENABLED } from '@/shared/config';
import { dateLib } from '@/shared/lib';

export const getDefaultDate = (): string =>
  MSW_ENABLED ? '2023-10-19' : dateLib.formatToISOStringWithoutTime(new Date());
