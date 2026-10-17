import { Chip, Stack, TextField } from '@mui/material';
import { type FC } from 'react';
import { useNow } from '../lib';
import { QUICK_TIMES, formatTime, parseTime } from '../model';

interface Props {
  value: number;
  onChange: (minutes: number) => void;
}

export const TimeOfDayPicker: FC<Props> = ({ value, onChange }) => {
  const now = useNow().minutes;
  const matchesQuickTime = QUICK_TIMES.some(quickTime => quickTime.minutes === value);

  return (
    <Stack spacing={1.5}>
      <Stack
        direction="row"
        useFlexGap
        role="group"
        aria-label="快速揀時間"
        sx={{ flexWrap: 'wrap', gap: 1 }}
      >
        {QUICK_TIMES.map(quickTime => (
          <Chip
            key={quickTime.label}
            label={`${quickTime.label} ${formatTime(quickTime.minutes)}`}
            color={quickTime.minutes === value ? 'primary' : 'default'}
            variant={quickTime.minutes === value ? 'filled' : 'outlined'}
            aria-pressed={quickTime.minutes === value}
            onClick={() => onChange(quickTime.minutes)}
            sx={{ height: 40, borderRadius: 20 }}
          />
        ))}
        <Chip
          label={`而家 ${formatTime(now)}`}
          color={!matchesQuickTime && value === now ? 'primary' : 'default'}
          variant={!matchesQuickTime && value === now ? 'filled' : 'outlined'}
          aria-pressed={!matchesQuickTime && value === now}
          onClick={() => onChange(now)}
          sx={{ height: 40, borderRadius: 20 }}
        />
      </Stack>
      <TextField
        type="time"
        label="時間"
        size="small"
        value={formatTime(value)}
        onChange={event => {
          if (event.target.value) {
            onChange(parseTime(event.target.value));
          }
        }}
        slotProps={{ htmlInput: { step: 300 } }}
        sx={{ width: 140 }}
      />
    </Stack>
  );
};
