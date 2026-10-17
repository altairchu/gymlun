import { Typography } from '@mui/material';
import { type FC } from 'react';
import { type categoryModel } from '@/entities/category';

interface CategoryProductsCountProps {
  category: categoryModel.Category;
}

const ProductsCount: FC<CategoryProductsCountProps> = ({
  category,
}: CategoryProductsCountProps) => {
  const { countProducts } = category;
  const text = countProducts === 0 ? '沒有食物' : `${countProducts} 款食物`;

  return <Typography aria-label={`${category.name}：${text}`}>{text}</Typography>;
};

export default ProductsCount;
