import CloseIcon from '@mui/icons-material/Close';
import { Box, FormHelperText, IconButton, Stack, Typography } from '@mui/material';
import { type FC } from 'react';
import { AmountInput, productModel } from '@/entities/product';
import { type FoodSelection } from '../lib';
import { type SelectedFood, caloriesOf, gramsOf } from '../model';

interface Props {
  item: SelectedFood;
  selection: FoodSelection;
}

export const SelectedFoodItem: FC<Props> = ({ item, selection }) => {
  const { product, unit, amount } = item;
  const grams = gramsOf(item);

  return (
    <Box component="li" aria-label={product.name} sx={{ py: 1 }}>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Typography noWrap sx={{ flex: 1, fontWeight: 500 }}>
          {product.name}
        </Typography>
        <Typography variant="body2" color="textSecondary" sx={{ whiteSpace: 'nowrap' }}>
          {caloriesOf(item)} kcal
        </Typography>
        <IconButton
          size="small"
          edge="end"
          aria-label={`移除${product.name}`}
          onClick={() => selection.remove(product.id)}
        >
          <CloseIcon fontSize="small" />
        </IconButton>
      </Stack>
      <Stack
        direction="row"
        useFlexGap
        sx={{ alignItems: 'center', flexWrap: 'wrap', columnGap: 1.5, rowGap: 0.5, mt: 0.5 }}
      >
        <AmountInput
          productName={product.name}
          amount={amount}
          unit={unit}
          invalid={grams === null}
          onAmountChange={value => selection.changeAmount(product.id, value)}
          onUnitChange={value => selection.changeUnit(product.id, value)}
          onStep={direction => selection.stepAmount(product.id, direction)}
        />
        {unit === productModel.QuantityUnit.Servings && grams !== null && (
          <Typography variant="body2" color="textSecondary">
            = {grams} g
          </Typography>
        )}
      </Stack>
      {grams === null && (
        <FormHelperText error>
          請輸入 {productModel.MIN_GRAMS} 至 {productModel.MAX_GRAMS} g 之間嘅數量
        </FormHelperText>
      )}
    </Box>
  );
};
