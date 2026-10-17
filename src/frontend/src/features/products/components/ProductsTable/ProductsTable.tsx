import {
  Checkbox,
  Table,
  TableBody,
  TableCell,
  tableCellClasses,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { type SxProps, type Theme } from '@mui/material/styles';
import { type FC, type ReactElement } from 'react';
import { type Product } from '@/entities/product';
import ProductsTableRow from '../ProductsTableRow';

interface ProductsTableProps {
  products: Product[];
  checkedIds: number[];
  onCheckedChange: (products: Product[], newCheckedIds: number[]) => void;
}

const COLUMNS_COUNT = 5;

const tableSx: SxProps<Theme> = {
  tableLayout: 'fixed',
  [`& .${tableCellClasses.root}:not(.${tableCellClasses.paddingCheckbox})`]: {
    px: { xs: 1, sm: 2 },
  },
  [`& .${tableCellClasses.head}`]: {
    whiteSpace: 'nowrap',
  },
};

const ProductsTable: FC<ProductsTableProps> = ({ products, checkedIds, onCheckedChange }) => {
  const allProductsChecked = products.length > 0 && products.length === checkedIds.length;

  const handleCheckedIdsChange = (): void => {
    onCheckedChange(products, checkedIds);
  };

  const renderRows = (): ReactElement => {
    if (products.length === 0) {
      return (
        <TableRow>
          <TableCell colSpan={COLUMNS_COUNT} align="center">
            <Typography
              sx={{
                color: 'textSecondary',
              }}
            >
              找不到食物
            </Typography>
          </TableCell>
        </TableRow>
      );
    }

    return (
      <>
        {products.map(product => (
          <ProductsTableRow key={product.id} product={product} />
        ))}
      </>
    );
  };

  return (
    <TableContainer>
      <Table sx={tableSx}>
        <TableHead>
          <TableRow>
            <TableCell padding="checkbox">
              <Checkbox
                color="primary"
                indeterminate={checkedIds.length > 0 && checkedIds.length < products.length}
                checked={allProductsChecked}
                onChange={handleCheckedIdsChange}
                disabled={products.length === 0}
                slotProps={{
                  input: {
                    'aria-label': '全選',
                  },
                }}
              />
            </TableCell>
            <TableCell>名稱</TableCell>
            <TableCell align="right" sx={{ width: { xs: 64, sm: 96 } }}>
              卡路里
            </TableCell>
            <TableCell align="right" sx={{ width: { xs: 64, sm: 120 } }}>
              份量
            </TableCell>
            <TableCell padding="checkbox" />
          </TableRow>
        </TableHead>
        <TableBody>{renderRows()}</TableBody>
      </Table>
    </TableContainer>
  );
};

export default ProductsTable;
