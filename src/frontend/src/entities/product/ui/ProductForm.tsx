import { zodResolver } from '@hookform/resolvers/zod';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Autocomplete,
  CircularProgress,
  FormControlLabel,
  Grid,
  InputAdornment,
  Snackbar,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { type FC, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { type SelectOption } from '@/shared/types';
import { useNutritionSuggestions } from '../lib/useNutritionSuggestions';
import {
  type ProductFormValues,
  productNameSchema,
  productSchema,
  nutritionValuesConfig,
  toPer100g,
  toPerServing,
} from '../model';
import { NutritionValueSchema } from '../model/NutritionValueSchema';
import { NutritionSuggestButton } from './NutritionSuggestButton';
import { NutritionValueIcon } from './NutritionValueIcon';
import { NutritionValueInput } from './NutritionValueInput';

interface Props {
  formId: string;
  defaultValues: ProductFormValues;
  categories: SelectOption[];
  categoriesLoading: boolean;
  autoFocus: boolean;
  onSubmit: OnSubmitProductFn;
  onNutritionSuggestingChange?: (nutritionSuggesting: boolean) => void;
}

export type OnSubmitProductFn = (product: ProductFormValues) => Promise<void>;

interface SnackbarState {
  severity: 'error' | 'info';
  message: string;
}

const OPTIONAL_NUTRITION_FIELDS = ['protein', 'fats', 'carbs', 'sugar', 'salt'] as const;

const capitalize = (value: string): string => value.charAt(0).toUpperCase() + value.slice(1);

export const ProductForm: FC<Props> = ({
  formId,
  defaultValues,
  categories,
  categoriesLoading,
  autoFocus,
  onSubmit,
  onNutritionSuggestingChange,
}) => {
  const { control, handleSubmit, getValues, setValue } = useForm({
    mode: 'onSubmit',
    resolver: zodResolver(productSchema),
    defaultValues,
  });

  const [snackbar, setSnackbar] = useState<SnackbarState | null>(null);
  const [perServing, setPerServing] = useState(false);

  const scaledFieldNames = ['calories', ...OPTIONAL_NUTRITION_FIELDS] as const;

  const handlePerServingChange = (checked: boolean): void => {
    const servingGrams = Number(getValues('defaultQuantity'));
    const calories = Number(getValues('calories'));
    const parsed = OPTIONAL_NUTRITION_FIELDS.map(field =>
      NutritionValueSchema.safeParse(getValues(field)),
    );

    if (!(servingGrams > 0) || isNaN(calories) || parsed.some(result => !result.success)) {
      return;
    }

    const [protein, fats, carbs, sugar, salt] = parsed.map(result => result.data ?? null);
    const values = { calories, protein, fats, carbs, sugar, salt };
    const converted = checked
      ? toPerServing(values, servingGrams)
      : toPer100g(values, servingGrams);

    scaledFieldNames.forEach(field => setValue(field, converted[field]));
    setPerServing(checked);
  };

  const submit = (data: ProductFormValues): Promise<void> =>
    onSubmit(perServing ? { ...data, ...toPer100g(data, data.defaultQuantity) } : data);

  const nutritionBasis = perServing ? 'per serving' : 'per 100 g';

  const atLeastOneNutritionFieldHasValue = getValues([
    'protein',
    'fats',
    'carbs',
    'sugar',
    'salt',
  ]).some(value => value !== null);

  const [nutritionExpanded, setNutritionExpanded] = useState(atLeastOneNutritionFieldHasValue);

  const { suggestingField, isSuggesting, handleSuggestClick } = useNutritionSuggestions({
    getName: () => getValues('name').trim(),
    getFieldValue: field => getValues(field),
    setFieldValue: (field, value) =>
      setValue(field, value, { shouldValidate: true, shouldDirty: true }),
    onNutritionSuggestingChange,
    onHasMacroSuggestion: () => setNutritionExpanded(true),
    onError: message => setSnackbar({ severity: 'error', message }),
    onInfo: message => setSnackbar({ severity: 'info', message }),
  });

  const name = useWatch({ control, name: 'name' });
  const suggestDisabled = !productNameSchema.safeParse(name).success || isSuggesting;

  return (
    <form id={formId} onSubmit={handleSubmit(submit)}>
      <Controller
        name="name"
        control={control}
        render={({ field, fieldState }) => (
          <TextField
            {...field}
            fullWidth
            autoFocus={autoFocus}
            disabled={isSuggesting}
            label="Name"
            placeholder="Product name"
            margin="normal"
            error={!!fieldState.error}
            helperText={fieldState.error?.message ?? ' '}
          />
        )}
      />
      <Controller
        name="category"
        control={control}
        render={({ field, fieldState }) => (
          <Autocomplete
            {...field}
            onChange={(_, value) => field.onChange(value)}
            disabled={isSuggesting}
            blurOnSelect="touch"
            options={categories}
            getOptionLabel={option => option.name}
            isOptionEqualToValue={(first, second) => first.name === second.name}
            renderInput={params => (
              <TextField
                {...params}
                label="Category"
                placeholder="Choose category"
                error={!!fieldState.error}
                helperText={fieldState.error?.message ?? ' '}
                margin="normal"
                slotProps={{
                  ...params.slotProps,
                  input: {
                    ...params.slotProps.input,
                    endAdornment: categoriesLoading ? (
                      <CircularProgress color="inherit" size={20} />
                    ) : (
                      params.slotProps.input.endAdornment
                    ),
                  },
                }}
              />
            )}
          />
        )}
      />
      <Grid container spacing={2}>
        <Grid size={6}>
          <Controller
            name="calories"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                disabled={isSuggesting}
                label={`Calories ${nutritionBasis}, ${nutritionValuesConfig.calories.unit}`}
                placeholder="Calories per 100 g"
                margin="normal"
                error={!!fieldState.error}
                helperText={fieldState.error?.message ?? ' '}
                onFocus={event => event.target.select()}
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <NutritionValueIcon type="calories" size="medium" />
                      </InputAdornment>
                    ),
                    endAdornment: (
                      <InputAdornment position="end">
                        <NutritionSuggestButton
                          label="Calories"
                          suggesting={suggestingField === 'calories'}
                          disabled={suggestDisabled}
                          onClick={handleSuggestClick('calories')}
                        />
                      </InputAdornment>
                    ),
                  },
                  htmlInput: {
                    type: 'text',
                    inputMode: 'numeric',
                  },
                }}
              />
            )}
          />
        </Grid>
        <Grid size={6}>
          <Controller
            name="defaultQuantity"
            control={control}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                fullWidth
                disabled={isSuggesting}
                label={perServing ? 'Serving, g' : 'Default quantity, g'}
                placeholder="Default quantity"
                margin="normal"
                error={!!fieldState.error}
                helperText={fieldState.error?.message ?? ' '}
                onFocus={event => event.target.select()}
                slotProps={{
                  htmlInput: {
                    type: 'text',
                    inputMode: 'numeric',
                  },
                }}
              />
            )}
          />
        </Grid>
      </Grid>
      <Stack sx={{ mb: 2 }}>
        <FormControlLabel
          control={
            <Switch
              checked={perServing}
              disabled={isSuggesting}
              onChange={(_, checked) => handlePerServingChange(checked)}
            />
          }
          label="Enter nutrition per serving"
        />
        <Controller
          name="isFavorite"
          control={control}
          render={({ field }) => (
            <FormControlLabel
              control={
                <Switch
                  checked={field.value}
                  disabled={isSuggesting}
                  onChange={(_, checked) => field.onChange(checked)}
                />
              }
              label="Eat often: show in quick add"
            />
          )}
        />
      </Stack>
      <Accordion
        variant="outlined"
        expanded={nutritionExpanded}
        onChange={(_, expanded) => setNutritionExpanded(expanded)}
      >
        <AccordionSummary
          expandIcon={<ExpandMoreIcon />}
          aria-controls="nutrition-components-panel-content"
          id="nutrition-components-panel-header"
        >
          <Typography component="span">Nutrition {nutritionBasis}</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Grid container spacing={2}>
            {OPTIONAL_NUTRITION_FIELDS.map(fieldName => (
              <Grid key={fieldName} size={6}>
                <Controller
                  name={fieldName}
                  control={control}
                  render={({ field, fieldState }) => (
                    <NutritionValueInput
                      {...field}
                      type={fieldName}
                      label={capitalize(fieldName)}
                      placeholder={capitalize(fieldName)}
                      disabled={isSuggesting}
                      suggesting={suggestingField === fieldName}
                      suggestDisabled={suggestDisabled}
                      onSuggest={handleSuggestClick(fieldName)}
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message ?? ' '}
                    />
                  )}
                />
              </Grid>
            ))}
          </Grid>
        </AccordionDetails>
      </Accordion>
      <Snackbar open={snackbar !== null} autoHideDuration={6000} onClose={() => setSnackbar(null)}>
        <Alert
          severity={snackbar?.severity ?? 'info'}
          onClose={() => setSnackbar(null)}
          sx={{ width: '100%' }}
        >
          {snackbar?.message ?? ''}
        </Alert>
      </Snackbar>
    </form>
  );
};
