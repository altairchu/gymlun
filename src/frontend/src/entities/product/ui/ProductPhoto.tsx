import { Box } from '@mui/material';
import { type FC } from 'react';
import { API_URL } from '@/shared/config';

interface Props {
  productName: string;
  photoUrl: string;
  size: number;
}

export const ProductPhoto: FC<Props> = ({ productName, photoUrl, size }) => (
  <Box
    component="img"
    src={`${API_URL}${photoUrl}`}
    alt={`「${productName}」的相片`}
    loading="lazy"
    sx={{
      width: size,
      height: size,
      flexShrink: 0,
      objectFit: 'cover',
      borderRadius: 1.5,
      bgcolor: 'action.hover',
    }}
  />
);
