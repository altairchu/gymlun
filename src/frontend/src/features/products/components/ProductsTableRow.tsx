import EditIcon from '@mui/icons-material/Edit';
import { Checkbox, Chip, IconButton, Stack, TableCell, TableRow, Tooltip } from '@mui/material';
import { type FC } from 'react';
import { useAppDispatch } from '@/app/store';
import { type Product, productLib, productModel } from '@/entities/product';
import { EditProduct } from '@/features/product/addEdit';

interface ProductsTableRowProps {
  product: Product;
}

const formatDefaultQuantity = ({ defaultQuantity, quantityUnit }: Product): string => {
  const grams = `${defaultQuantity}\u00A0g`;
  return quantityUnit === productModel.QuantityUnit.Servings ? `每份 ${grams}` : grams;
};

const ProductsTableRow: FC<ProductsTableRowProps> = ({ product }: ProductsTableRowProps) => {
  const dispatch = useAppDispatch();
  const checkedProductIds = productLib.useCheckedProductIds();
  const isChecked = checkedProductIds.some(id => id === product.id);
  const defaultQuantity = formatDefaultQuantity(product);

  const handleCheckedChange = (): void => {
    if (isChecked) {
      dispatch(productModel.actions.productUnchecked(product.id));
    } else {
      dispatch(productModel.actions.productChecked(product.id));
    }
  };

  return (
    <TableRow hover selected={isChecked}>
      <TableCell padding="checkbox">
        <Checkbox
          color="primary"
          checked={isChecked}
          onChange={handleCheckedChange}
          slotProps={{
            input: {
              'aria-label': `選取「${product.name}」`,
            },
          }}
        />
      </TableCell>
      <TableCell sx={{ overflowWrap: 'anywhere' }}>
        {product.name}
        {product.categories.length > 0 && (
          <Stack
            component="ul"
            direction="row"
            useFlexGap
            aria-label={`「${product.name}」的分類`}
            sx={{ flexWrap: 'wrap', gap: 0.5, listStyle: 'none', m: 0, mt: 0.5, p: 0 }}
          >
            {product.categories.map(category => (
              <Chip key={category.id} component="li" size="small" label={category.name} />
            ))}
          </Stack>
        )}
      </TableCell>
      <TableCell align="right" aria-label={`「${product.name}」的卡路里為 ${product.caloriesCost}`}>
        {product.caloriesCost}
      </TableCell>
      <TableCell
        align="right"
        aria-label={`「${product.name}」的份量為 ${defaultQuantity}`}
        sx={{ wordBreak: 'keep-all' }}
      >
        {defaultQuantity}
      </TableCell>
      <TableCell padding="checkbox">
        <EditProduct
          product={product}
          renderTrigger={openEditDialog => (
            <Tooltip title="編輯食物">
              <IconButton onClick={openEditDialog} aria-label={`編輯食物「${product.name}」`}>
                <EditIcon />
              </IconButton>
            </Tooltip>
          )}
        />
      </TableCell>
    </TableRow>
  );
};

export default ProductsTableRow;
