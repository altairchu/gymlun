import { addDays, differenceInDays, subMonths } from 'date-fns';
import { format, formatWithOptions, endOfMonth } from 'date-fns/fp';
import { zhHK } from 'date-fns/locale/zh-HK';

export const enum DateFormat {
  Iso = 'yyyy-MM-dd',
  UserFriendly = "yyyy'年'M'月'd'日'",
  MonthAndYear = "yyyy'年'M'月'",
}

export const locale = zhHK;

export const formatToISOStringWithoutTime = format(DateFormat.Iso);
export const formatToUserFriendlyString = formatWithOptions({ locale }, DateFormat.UserFriendly);
export const getEndOfMonth = endOfMonth();

export const getCurrentDate = (): Date => {
  const today = new Date();
  return new Date(today.getFullYear(), today.getMonth(), today.getDate());
};

export { addDays, differenceInDays, subMonths };
