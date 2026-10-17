import DeleteIcon from '@mui/icons-material/Delete';
import { Box, IconButton, Stack, Tooltip, Typography } from '@mui/material';
import { useState, type FC } from 'react';
import { productLib } from '@/entities/product';
import { AddProduct } from '@/features/product/addEdit';
import DeleteProductsDialog from './DeleteProductsDialog';
import { SearchByCategory } from './SearchByCategory';
import { SearchByName } from './SearchByName';

const ProductsTableToolbar: FC = () => {
  const checkedProductIds = productLib.useCheckedProductIds();
  const [isDeleteDialogOpened, setIsDeleteDialogOpened] = useState(false);
  const isSelectionActive = checkedProductIds.length > 0;

  const handleDeleteClick = (): void => {
    setIsDeleteDialogOpened(true);
  };

  return (
    <Box
      sx={{
        p: 2,
      }}
    >
      {isSelectionActive && (
        <Stack
          direction="row"
          spacing={2}
          sx={{
            alignItems: 'center',
          }}
        >
          <Typography
            sx={{
              flexGrow: 1,
            }}
          >
            已選取 {checkedProductIds.length} 項
          </Typography>
          <Tooltip title="刪除食物">
            <IconButton onClick={handleDeleteClick} size="large" aria-label="刪除已選取的食物">
              <DeleteIcon />
            </IconButton>
          </Tooltip>
        </Stack>
      )}
      {!isSelectionActive && (
        <Stack
          spacing={{ xs: 3, sm: 2 }}
          direction={{ xs: 'column', sm: 'row' }}
          sx={{
            width: '100%',
            alignItems: { xs: 'flex-start', sm: 'center' },
            justifyContent: 'space-between',
          }}
        >
          <SearchByName />
          <SearchByCategory />
          <AddProduct />
        </Stack>
      )}
      <DeleteProductsDialog isOpened={isDeleteDialogOpened} setIsOpened={setIsDeleteDialogOpened} />
    </Box>
  );
};

export default ProductsTableToolbar;
