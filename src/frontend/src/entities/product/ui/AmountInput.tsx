import AddIcon from '@mui/icons-material/Add';
import RemoveIcon from '@mui/icons-material/Remove';
import {
  IconButton,
  InputAdornment,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
} from '@mui/material';
import { type FC } from 'react';
import { QUANTITY_UNIT_SHORT_LABELS, QuantityUnit } from '../model';

interface Props {
  productName: string;
  amount: string;
  unit: QuantityUnit;
  invalid: boolean;
  onAmountChange: (amount: string) => void;
  onUnitChange: (unit: QuantityUnit) => void;
  onStep: (direction: 1 | -1) => void;
}

export const AmountInput: FC<Props> = ({
  productName,
  amount,
  unit,
  invalid,
  onAmountChange,
  onUnitChange,
  onStep,
}) => (
  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
    <IconButton size="small" aria-label={`減少${productName}`} onClick={() => onStep(-1)}>
      <RemoveIcon fontSize="small" />
    </IconButton>
    <TextField
      size="small"
      value={amount}
      error={invalid}
      onChange={event => onAmountChange(event.target.value)}
      onFocus={event => event.target.select()}
      slotProps={{
        htmlInput: {
          inputMode: 'decimal',
          'aria-label': `${productName}數量`,
          sx: { textAlign: 'center', py: 0.75 },
        },
        input: {
          endAdornment: (
            <InputAdornment position="end" sx={{ ml: 0 }}>
              {unit === QuantityUnit.Servings ? '份' : 'g'}
            </InputAdornment>
          ),
        },
      }}
      sx={{ width: 88 }}
    />
    <IconButton size="small" aria-label={`增加${productName}`} onClick={() => onStep(1)}>
      <AddIcon fontSize="small" />
    </IconButton>
    <ToggleButtonGroup
      exclusive
      size="small"
      value={unit}
      aria-label={`${productName}計法`}
      onChange={(_event, value: QuantityUnit | null) => {
        if (value !== null) {
          onUnitChange(value);
        }
      }}
    >
      <ToggleButton value={QuantityUnit.Servings} sx={{ px: 1.5, py: 0.5 }}>
        {QUANTITY_UNIT_SHORT_LABELS[QuantityUnit.Servings]}
      </ToggleButton>
      <ToggleButton value={QuantityUnit.Grams} sx={{ px: 1.5, py: 0.5 }}>
        {QUANTITY_UNIT_SHORT_LABELS[QuantityUnit.Grams]}
      </ToggleButton>
    </ToggleButtonGroup>
  </Stack>
);
