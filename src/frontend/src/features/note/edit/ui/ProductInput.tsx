import { Autocomplete, TextField } from '@mui/material';
import { type FC } from 'react';
import { type Product } from '@/entities/note';

interface Props {
  value: Product;
  options: Product[];
  loading: boolean;
  onChange: (product: Product) => void;
}

export const ProductInput: FC<Props> = ({ value, options, loading, onChange }) => {
  // MUI warns about a value missing from the options, which the logged food is while foods load
  const optionsWithValue = options.some(option => option.id === value.id)
    ? options
    : [value, ...options];

  return (
    <Autocomplete<Product, false, true>
      disableClearable
      autoHighlight
      blurOnSelect="touch"
      value={value}
      options={optionsWithValue}
      loading={loading}
      loadingText="載入緊食物…"
      noOptionsText="搵唔到呢樣食物"
      getOptionLabel={option => option.name}
      isOptionEqualToValue={(option, selected) => option.id === selected.id}
      onChange={(_, product) => onChange(product)}
      renderInput={params => (
        <TextField
          {...params}
          size="small"
          slotProps={{
            ...params.slotProps,
            htmlInput: { ...params.slotProps.htmlInput, 'aria-label': '食物' },
          }}
        />
      )}
    />
  );
};
