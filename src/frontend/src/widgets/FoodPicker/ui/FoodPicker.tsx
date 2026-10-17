import AddIcon from '@mui/icons-material/Add';
import ClearIcon from '@mui/icons-material/Clear';
import SearchIcon from '@mui/icons-material/Search';
import {
  Box,
  ButtonBase,
  IconButton,
  InputAdornment,
  Skeleton,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useState, type FC } from 'react';
import { categoryLib } from '@/entities/category';
import { productApi, productModel, type ProductSelectOption } from '@/entities/product';
import { type FoodSelection } from '@/features/logFoods';
import { AddProduct } from '@/features/product/addEdit';
import { EMPTY_FOOD_FILTER, filterFoods, type FoodFilter } from '../lib';
import { CategoryFilter } from './CategoryFilter';
import { FoodCard } from './FoodCard';

interface Props {
  selection: FoodSelection;
}

const EMPTY_PRODUCTS: ProductSelectOption[] = [];

const GRID_SX = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fill, minmax(132px, 1fr))',
  gap: 1,
} as const;

export const FoodPicker: FC<Props> = ({ selection }) => {
  const { data: products = EMPTY_PRODUCTS, isLoading } =
    productApi.useProductsAutocompleteQuery(null);
  const { categories } = categoryLib.useCategoriesForSelect();
  const [filter, setFilter] = useState<FoodFilter>(EMPTY_FOOD_FILTER);
  const [fetchProducts] = productApi.useLazyProductsAutocompleteQuery();

  const foods = filterFoods(products, filter);
  const query = filter.query.trim();

  const selectCreatedProduct = async (productId: number): Promise<void> => {
    const result = await fetchProducts(null);
    const created = result.data?.find(product => product.id === productId);

    if (created) {
      selection.toggle(created);
    }
  };

  const selectedLabelOf = (product: ProductSelectOption): string | null => {
    const item = selection.items.find(selected => selected.product.id === product.id);
    return item
      ? `已揀 ${productModel.formatAmount(productModel.parseAmount(item.amount) ?? 0, item.unit)}`
      : null;
  };

  return (
    <Stack spacing={1.5} component="section" aria-label="揀食物">
      <TextField
        size="small"
        placeholder="搜尋食物"
        value={filter.query}
        onChange={event => setFilter({ ...filter, query: event.target.value })}
        slotProps={{
          htmlInput: { 'aria-label': '搜尋食物', type: 'search' },
          input: {
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon />
              </InputAdornment>
            ),
            endAdornment: filter.query && (
              <InputAdornment position="end">
                <IconButton
                  size="small"
                  aria-label="清除搜尋"
                  onClick={() => setFilter({ ...filter, query: '' })}
                >
                  <ClearIcon fontSize="small" />
                </IconButton>
              </InputAdornment>
            ),
          },
        }}
      />
      <CategoryFilter
        products={products}
        categories={categories}
        filter={filter}
        onChange={setFilter}
      />
      {isLoading ? (
        <Box sx={GRID_SX}>
          {Array.from({ length: 6 }, (_, index) => (
            <Skeleton key={index} variant="rounded" height={92} />
          ))}
        </Box>
      ) : (
        <>
          {foods.length === 0 && (
            <Typography color="textSecondary" sx={{ py: 1 }}>
              {products.length === 0 ? '未有食物，加一樣先啦。' : '冇符合嘅食物'}
            </Typography>
          )}
          <Box sx={GRID_SX}>
            {foods.map(product => (
              <FoodCard
                key={product.id}
                product={product}
                selectedLabel={selectedLabelOf(product)}
                onToggle={selection.toggle}
              />
            ))}
            <AddProduct
              initialName={query}
              onCreated={selectCreatedProduct}
              renderTrigger={open => (
                <ButtonBase
                  onClick={open}
                  sx={theme => ({
                    minHeight: 92,
                    m: '1px',
                    p: 1.5,
                    gap: 0.5,
                    borderRadius: 2,
                    border: `1px dashed ${theme.palette.primary.main}`,
                    color: theme.palette.primary.main,
                    flexDirection: 'column',
                  })}
                >
                  <AddIcon />
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {query && foods.length === 0 ? `新增「${query}」` : '新增食物'}
                  </Typography>
                </ButtonBase>
              )}
            />
          </Box>
        </>
      )}
    </Stack>
  );
};
