import { Box, Card, Stack, Typography } from '@mui/material';
import { type FC } from 'react';
import { NutritionValueIcon, productModel } from '@/entities/product';

interface Props {
  nutritionValues: productModel.NutritionValues;
}

const NUTRIENTS: productModel.NutritionValueType[] = ['protein', 'fats', 'carbs', 'sugar', 'salt'];

const roundToTenths = (value: number): number => Math.round(value * 10) / 10;

export const NutritionSummaryWidget: FC<Props> = ({ nutritionValues }) => {
  const calories = productModel.nutritionValuesConfig.calories;

  return (
    <Card
      component="section"
      aria-label="每日營養摘要"
      sx={{
        px: 2,
        py: 1.5,
        display: 'grid',
        gridTemplateColumns: { xs: 'repeat(5, minmax(0, 1fr))', sm: 'auto repeat(5, auto)' },
        justifyContent: { sm: 'start' },
        alignItems: 'center',
        columnGap: { xs: 1, sm: 3 },
        rowGap: 1,
      }}
    >
      <Stack
        direction="row"
        spacing={0.75}
        sx={{ alignItems: 'baseline', gridColumn: { xs: '1 / -1', sm: 'auto' } }}
      >
        <NutritionValueIcon type="calories" size="medium" />
        <Typography
          variant="h5"
          component="span"
          sx={{ color: calories.color, fontWeight: 'bold' }}
        >
          {nutritionValues.calories}
        </Typography>
        <Typography variant="body2" component="span" color="textSecondary">
          {calories.unit}
        </Typography>
      </Stack>
      {NUTRIENTS.map(type => {
        const { shortLabel, unit, color } = productModel.nutritionValuesConfig[type];
        const value = nutritionValues[type];

        return (
          <Box key={type} sx={{ minWidth: 0 }}>
            <Typography variant="caption" component="div" color="textSecondary" noWrap>
              {shortLabel}
            </Typography>
            <Typography
              variant="subtitle1"
              component="div"
              noWrap
              sx={{ color, fontWeight: 'bold', lineHeight: 1.3 }}
            >
              {value === null ? <>&mdash;</> : roundToTenths(value)}
              <Typography
                variant="caption"
                component="span"
                color="textSecondary"
                sx={{ ml: 0.25 }}
              >
                {unit}
              </Typography>
            </Typography>
          </Box>
        );
      })}
    </Card>
  );
};
