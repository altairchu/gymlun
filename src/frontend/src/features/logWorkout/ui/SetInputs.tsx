import { InputAdornment, TextField } from '@mui/material';
import { type FC } from 'react';

interface Props {
  value: string | number;
  error: string | undefined;
  onChange: (value: string) => void;
  onBlur: () => void;
}

export const RepsInput: FC<Props> = ({ value, error, onChange, onBlur }) => (
  <TextField
    value={value}
    label="次數"
    fullWidth
    error={error !== undefined}
    helperText={error ?? ' '}
    onChange={event => onChange(event.target.value)}
    onBlur={onBlur}
    onFocus={event => {
      event.target.select();
    }}
    slotProps={{
      input: {
        endAdornment: <InputAdornment position="end">次</InputAdornment>,
      },
      htmlInput: {
        type: 'text',
        inputMode: 'numeric',
      },
    }}
  />
);

export const WeightKgInput: FC<Props> = ({ value, error, onChange, onBlur }) => (
  <TextField
    value={value}
    label="重量"
    fullWidth
    error={error !== undefined}
    helperText={error ?? ' '}
    onChange={event => onChange(event.target.value)}
    onBlur={onBlur}
    onFocus={event => {
      event.target.select();
    }}
    slotProps={{
      input: {
        endAdornment: <InputAdornment position="end">kg</InputAdornment>,
      },
      htmlInput: {
        type: 'text',
        inputMode: 'decimal',
      },
    }}
  />
);
