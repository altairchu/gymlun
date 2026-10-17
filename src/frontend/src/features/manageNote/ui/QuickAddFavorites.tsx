import StarIcon from '@mui/icons-material/Star';
import { Alert, Chip, Snackbar, Stack } from '@mui/material';
import { type FC, useState } from 'react';
import { noteApi, noteModel } from '@/entities/note';
import { productApi, type ProductSelectOption } from '@/entities/product';

interface Props {
  date: string;
  mealType: noteModel.MealType;
}

interface SnackbarState {
  severity: 'success' | 'error';
  message: string;
}

export const QuickAddFavorites: FC<Props> = ({ date, mealType }) => {
  const { favorites } = productApi.useProductsAutocompleteQuery(null, {
    selectFromResult: ({ data }) => ({
      favorites: data?.filter(product => product.isFavorite) ?? [],
    }),
  });

  const { displayOrder, notesLoaded } = noteApi.useNotesQuery(
    { date },
    {
      selectFromResult: ({ isSuccess, data }) => ({
        notesLoaded: isSuccess,
        displayOrder: isSuccess ? noteModel.querySelectors.nextDisplayOrder(data, mealType) : 0,
      }),
    },
  );

  const [createNote, { isLoading: isAdding }] = noteApi.useCreateNoteMutation();
  const [snackbar, setSnackbar] = useState<SnackbarState | null>(null);

  if (favorites.length === 0) {
    return null;
  }

  const handleAdd = async (product: ProductSelectOption): Promise<void> => {
    const response = await createNote({
      date,
      mealType,
      displayOrder,
      productId: product.id,
      productQuantity: product.defaultQuantity,
    });

    setSnackbar(
      response.error
        ? { severity: 'error', message: `未能加入「${product.name}」` }
        : {
            severity: 'success',
            message: `已加入「${product.name}」，${product.defaultQuantity} g`,
          },
    );
  };

  return (
    <>
      <Stack
        direction="row"
        spacing={1}
        aria-label="快速加入常吃食物"
        sx={{ width: '100%', overflowX: 'auto', pb: 0.5 }}
      >
        {favorites.map(product => (
          <Chip
            key={product.id}
            icon={<StarIcon fontSize="small" />}
            label={`${product.name} · ${Math.round((product.calories * product.defaultQuantity) / 100)} kcal`}
            aria-label={`加入「${product.name}」，${product.defaultQuantity} g`}
            color="primary"
            variant="outlined"
            disabled={!notesLoaded || isAdding}
            onClick={() => void handleAdd(product)}
          />
        ))}
      </Stack>
      <Snackbar open={snackbar !== null} autoHideDuration={3000} onClose={() => setSnackbar(null)}>
        <Alert
          severity={snackbar?.severity ?? 'success'}
          onClose={() => setSnackbar(null)}
          sx={{ width: '100%' }}
        >
          {snackbar?.message ?? ''}
        </Alert>
      </Snackbar>
    </>
  );
};
