import StarIcon from '@mui/icons-material/Star';
import { Chip, Stack } from '@mui/material';
import { type FC } from 'react';
import { type ProductSelectOption } from '@/entities/product';
import { type SelectOption } from '@/shared/types';
import { type FoodFilter, filterFoods, isFilterActive, toggleCategory } from '../lib';

interface Props {
  products: ProductSelectOption[];
  categories: SelectOption[];
  filter: FoodFilter;
  onChange: (filter: FoodFilter) => void;
}

export const CategoryFilter: FC<Props> = ({ products, categories, filter, onChange }) => {
  const usedCategories = categories.filter(category =>
    products.some(product => product.categoryIds.includes(category.id)),
  );

  const hasMatches = (nextFilter: FoodFilter): boolean =>
    filterFoods(products, { ...nextFilter, query: '' }).length > 0;

  return (
    <Stack
      direction="row"
      role="group"
      aria-label="按分類篩選"
      useFlexGap
      sx={{
        gap: 1,
        flexWrap: { xs: 'nowrap', md: 'wrap' },
        overflowX: { xs: 'auto', md: 'visible' },
        mx: { xs: -2, md: 0 },
        px: { xs: 2, md: 0 },
        pb: 0.5,
        scrollbarWidth: 'none',
      }}
    >
      <Chip
        icon={<StarIcon />}
        label="常食"
        color={filter.favoritesOnly ? 'primary' : 'default'}
        variant={filter.favoritesOnly ? 'filled' : 'outlined'}
        aria-pressed={filter.favoritesOnly}
        disabled={!filter.favoritesOnly && !hasMatches({ ...filter, favoritesOnly: true })}
        onClick={() => onChange({ ...filter, favoritesOnly: !filter.favoritesOnly })}
        sx={{ height: 36, borderRadius: 18, flexShrink: 0 }}
      />
      {usedCategories.map(category => {
        const selected = filter.categoryIds.includes(category.id);

        return (
          <Chip
            key={category.id}
            label={category.name}
            color={selected ? 'primary' : 'default'}
            variant={selected ? 'filled' : 'outlined'}
            aria-pressed={selected}
            disabled={!selected && !hasMatches(toggleCategory(filter, category.id))}
            onClick={() => onChange(toggleCategory(filter, category.id))}
            sx={{ height: 36, borderRadius: 18, flexShrink: 0 }}
          />
        );
      })}
      {isFilterActive(filter) && (
        <Chip
          label="清除篩選"
          variant="outlined"
          onClick={() => onChange({ ...filter, categoryIds: [], favoritesOnly: false })}
          sx={{ height: 36, borderRadius: 18, flexShrink: 0, borderStyle: 'dashed' }}
        />
      )}
    </Stack>
  );
};
