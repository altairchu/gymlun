import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import StarIcon from '@mui/icons-material/Star';
import { ButtonBase, Stack, Typography } from '@mui/material';
import { amber } from '@mui/material/colors';
import { alpha } from '@mui/material/styles';
import { type FC } from 'react';
import { ProductPhoto, productModel, type ProductSelectOption } from '@/entities/product';

interface Props {
  product: ProductSelectOption;
  selectedLabel: string | null;
  onToggle: (product: ProductSelectOption) => void;
}

const portionOf = ({ quantityUnit, defaultQuantity }: ProductSelectOption): string =>
  quantityUnit === productModel.QuantityUnit.Servings
    ? `每份 ${defaultQuantity} g`
    : `${defaultQuantity} g`;

export const FoodCard: FC<Props> = ({ product, selectedLabel, onToggle }) => {
  const selected = selectedLabel !== null;

  return (
    <ButtonBase
      aria-pressed={selected}
      onClick={() => onToggle(product)}
      sx={theme => ({
        display: 'flex',
        alignItems: 'flex-start',
        justifyContent: 'flex-start',
        textAlign: 'left',
        gap: 1.25,
        minHeight: 92,
        p: 1.5,
        borderRadius: 2,
        border: `${selected ? 2 : 1}px solid`,
        borderColor: selected ? theme.palette.primary.main : theme.palette.divider,
        bgcolor: selected
          ? alpha(theme.palette.primary.main, 0.08)
          : theme.palette.background.paper,
        // Keeps the text from shifting when the border gets thicker
        m: selected ? 0 : '1px',
        transition: theme.transitions.create(['background-color', 'border-color']),
      })}
    >
      {product.photoUrl !== null && (
        <ProductPhoto productName={product.name} photoUrl={product.photoUrl} size={56} />
      )}
      <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'flex-start' }}>
          <Typography
            sx={{
              flex: 1,
              fontWeight: 500,
              lineHeight: 1.3,
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical',
              overflow: 'hidden',
              wordBreak: 'break-word',
            }}
          >
            {product.name}
          </Typography>
          {selected ? (
            <CheckCircleIcon color="primary" fontSize="small" />
          ) : (
            product.isFavorite && (
              <StarIcon fontSize="small" sx={{ color: amber[600] }} aria-label="常食" />
            )
          )}
        </Stack>
        <Typography variant="body2" color="textSecondary">
          {productModel.caloriesOf(product.calories, product.defaultQuantity)} kcal ·{' '}
          {portionOf(product)}
        </Typography>
        {selected && (
          <Typography variant="body2" color="primary" sx={{ fontWeight: 500 }}>
            {selectedLabel}
          </Typography>
        )}
      </Stack>
    </ButtonBase>
  );
};
