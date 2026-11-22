export const readText = (form: FormData, key: string): string | null => {
  const value = form.get(key);
  return typeof value === 'string' && value.trim() !== '' ? value.trim() : null;
};

export const readNumber = (form: FormData, key: string): number | null => {
  const value = readText(form, key);
  if (value === null) {
    return null;
  }

  const number = Number(value);
  return Number.isFinite(number) ? number : null;
};

const valueFormatter = new Intl.NumberFormat('zh-HK', { maximumFractionDigits: 2 });

export const formatValue = (value: number | null | undefined, unit = ''): string => {
  if (value === null || value === undefined) {
    return '—';
  }

  return [valueFormatter.format(value), unit].filter(Boolean).join(' ');
};
