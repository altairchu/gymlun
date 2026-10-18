import { Alert, Stack, Typography } from '@mui/material';
import { useState, type FC } from 'react';
import { TimeOfDayPicker, type NoteItem, noteApi, noteModel } from '@/entities/note';
import {
  AmountInput,
  productApi,
  productModel,
  type ProductSelectOption,
} from '@/entities/product';
import { Button, Dialog } from '@/shared/ui';
import { ProductInput } from './ProductInput';

interface Props {
  note: NoteItem;
  onClose: () => void;
}

const EMPTY_PRODUCTS: ProductSelectOption[] = [];

export const EditNoteDialog: FC<Props> = ({ note, onClose }) => {
  const [product, setProduct] = useState(note.product);
  const [unit, setUnit] = useState(product.quantityUnit);
  const [amount, setAmount] = useState(() =>
    productModel.gramsToAmount(note.productQuantity, product.quantityUnit, product.defaultQuantity),
  );
  const [minutes, setMinutes] = useState(() => noteModel.parseTime(note.time));
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const { data: products = EMPTY_PRODUCTS, isLoading: productsLoading } =
    productApi.useProductsAutocompleteQuery(null);
  const [updateNote, updateResult] = noteApi.useUpdateNoteMutation();
  const [deleteNote, deleteResult] = noteApi.useDeleteNoteMutation();

  const grams = productModel.amountToGrams(amount, unit, product.defaultQuantity);
  const busy = updateResult.isLoading || deleteResult.isLoading;

  const handleSave = async (): Promise<void> => {
    if (grams === null) {
      return;
    }

    const result = await updateNote({
      id: note.id,
      note: {
        date: note.date,
        time: noteModel.toApiTime(minutes),
        productId: product.id,
        productQuantity: grams,
        displayOrder: note.displayOrder,
      },
      product,
    });

    if (!result.error) {
      onClose();
    }
  };

  const handleDelete = async (): Promise<void> => {
    if (!confirmingDelete) {
      setConfirmingDelete(true);
      return;
    }

    const result = await deleteNote(note.id);

    if (!result.error) {
      onClose();
    }
  };

  return (
    <Dialog
      opened
      title={product.name}
      onClose={onClose}
      content={
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          <Stack spacing={1}>
            <Typography variant="subtitle2" component="h3">
              食物
            </Typography>
            <ProductInput
              value={product}
              options={products}
              loading={productsLoading}
              onChange={setProduct}
            />
          </Stack>
          <Stack spacing={1}>
            <Typography variant="subtitle2" component="h3">
              數量
            </Typography>
            <AmountInput
              productName={product.name}
              amount={amount}
              unit={unit}
              invalid={grams === null}
              onAmountChange={setAmount}
              onUnitChange={nextUnit => {
                setAmount(
                  productModel.convertAmount(amount, unit, nextUnit, product.defaultQuantity),
                );
                setUnit(nextUnit);
              }}
              onStep={direction => setAmount(productModel.stepAmount(amount, unit, direction))}
            />
            <Typography variant="body2" color={grams === null ? 'error' : 'textSecondary'}>
              {grams === null
                ? `請輸入 ${productModel.MIN_GRAMS} 至 ${productModel.MAX_GRAMS} g 之間嘅數量`
                : `${grams} g · ${productModel.caloriesOf(product.calories, grams)} kcal`}
            </Typography>
          </Stack>
          <Stack spacing={1}>
            <Typography variant="subtitle2" component="h3">
              幾時食
            </Typography>
            <TimeOfDayPicker value={minutes} onChange={setMinutes} />
          </Stack>
          {(updateResult.isError || deleteResult.isError) && (
            <Alert severity="error">儲存唔到，請再試一次</Alert>
          )}
          <Button
            color="error"
            variant={confirmingDelete ? 'contained' : 'outlined'}
            loading={deleteResult.isLoading}
            disabled={updateResult.isLoading}
            onClick={handleDelete}
            sx={{ alignSelf: 'flex-start' }}
          >
            {confirmingDelete ? '確定刪除？' : '刪除呢項記錄'}
          </Button>
        </Stack>
      }
      renderCancel={props => (
        <Button {...props} onClick={onClose} disabled={busy}>
          取消
        </Button>
      )}
      renderSubmit={props => (
        <Button
          {...props}
          disabled={grams === null || deleteResult.isLoading}
          loading={updateResult.isLoading}
          onClick={handleSave}
        >
          儲存
        </Button>
      )}
    />
  );
};
