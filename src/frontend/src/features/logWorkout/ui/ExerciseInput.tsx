import { Autocomplete, TextField } from '@mui/material';
import { type FC } from 'react';

interface Props {
  value: string;
  options: string[];
  loading: boolean;
  error: string | undefined;
  onChange: (value: string) => void;
  onBlur: () => void;
}

export const ExerciseInput: FC<Props> = ({ value, options, loading, error, onChange, onBlur }) => (
  <Autocomplete<string, false, false, true>
    freeSolo
    blurOnSelect="touch"
    // Autocomplete resets an uncontrolled input to its value's label on mount, so both are bound
    value={value}
    inputValue={value}
    options={options}
    loading={loading}
    loadingText="載入緊動作…"
    onChange={(_, exercise) => onChange(exercise ?? '')}
    onInputChange={(_, exercise) => onChange(exercise)}
    onBlur={onBlur}
    renderInput={params => (
      <TextField
        {...params}
        label="動作"
        placeholder="例如 Bench Press"
        error={error !== undefined}
        helperText={error ?? ' '}
      />
    )}
  />
);
