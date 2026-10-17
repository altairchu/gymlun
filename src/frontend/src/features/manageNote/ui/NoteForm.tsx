import { zodResolver } from '@hookform/resolvers/zod';
import CancelIcon from '@mui/icons-material/Cancel';
import EditIcon from '@mui/icons-material/Edit';
import {
  Box,
  CircularProgress,
  IconButton,
  InputAdornment,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { type ReactNode, type FC, type MouseEventHandler } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { productModel } from '@/entities/product';
import { noteSchema, type NoteFormValues, type NoteFormValuesProduct } from '../model';

interface Props {
  formId: string;
  defaultValues: NoteFormValues;
  productForEditLoading: boolean;
  isSubmitting: boolean;
  submitDisabled: boolean;
  onSubmit: OnSubmitNoteFn;
  onLoadProductForEdit: OnEditProductFn;
  onDiscardProduct: () => void;
}

const toOptionalNutritionValues = ({
  protein,
  fats,
  carbs,
  sugar,
  salt,
}: NoteFormValuesProduct): productModel.OptionalNutritionValues => ({
  protein,
  fats,
  carbs,
  sugar,
  salt,
});

export type OnSubmitNoteFn = (note: NoteFormValues) => Promise<void>;

export type OnEditProductFn = (productId: number) => Promise<void>;

export const NoteForm: FC<Props> = ({
  formId,
  defaultValues,
  productForEditLoading,
  isSubmitting,
  submitDisabled,
  onSubmit,
  onLoadProductForEdit,
  onDiscardProduct,
}) => {
  const { control, handleSubmit, getValues } = useForm({
    mode: 'onSubmit',
    resolver: zodResolver(noteSchema),
    defaultValues,
  });

  const handleEditProduct: MouseEventHandler = () => {
    const { product } = getValues();

    if (product) {
      onLoadProductForEdit(product.id);
    }
  };

  const renderHelperText = (): ReactNode => {
    const { product } = getValues();

    if (!product) {
      return ' ';
    }

    const nutritionValues = toOptionalNutritionValues(product);

    if (productModel.hasMissingNutritionValues(nutritionValues)) {
      return (
        <Typography variant="caption" component="span" color="warning">
          缺少營養數值
        </Typography>
      );
    }

    return ' ';
  };

  return (
    <form id={formId} onSubmit={handleSubmit(values => onSubmit(values))}>
      <TextField
        label="食物"
        value={defaultValues.product?.name}
        variant="outlined"
        fullWidth
        margin="normal"
        helperText={renderHelperText()}
        slotProps={{
          input: {
            readOnly: true,
            endAdornment: (
              <InputAdornment position="end">
                {productForEditLoading ? (
                  <Box
                    sx={{
                      p: 1,
                    }}
                  >
                    <CircularProgress size={20} />
                  </Box>
                ) : (
                  <Tooltip title="編輯食物">
                    <IconButton
                      disabled={isSubmitting || submitDisabled}
                      onClick={handleEditProduct}
                    >
                      <EditIcon />
                    </IconButton>
                  </Tooltip>
                )}
                <Tooltip title="捨棄並選擇其他食物">
                  <IconButton
                    edge="end"
                    disabled={isSubmitting || submitDisabled}
                    onClick={onDiscardProduct}
                  >
                    <CancelIcon />
                  </IconButton>
                </Tooltip>
              </InputAdornment>
            ),
          },
        }}
      />
      <Controller
        name="quantity"
        control={control}
        render={({ field, fieldState }) => (
          <TextField
            {...field}
            fullWidth
            margin="normal"
            label="份量"
            placeholder="食物份量 (g)"
            error={!!fieldState.error}
            helperText={fieldState.error?.message ?? ' '}
            onFocus={event => event.target.select()}
            slotProps={{
              input: {
                endAdornment: <InputAdornment position="end">g</InputAdornment>,
              },
              htmlInput: {
                type: 'text',
                inputMode: 'numeric',
              },
            }}
          />
        )}
      />
    </form>
  );
};
