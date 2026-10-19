import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, InputAdornment, Stack, TextField } from '@mui/material';
import { useState, type FC } from 'react';
import { Controller, type SubmitHandler, useForm } from 'react-hook-form';
import { type WeightLogItem, weightLogsApi } from '@/entities/weightLog';
import { dateLib } from '@/shared/lib';
import { Button, Dialog } from '@/shared/ui';
import { type EditFormValues, editSchema } from '../model';

const formId = 'edit-weight-log-form';

interface Props {
  weightLog: WeightLogItem;
  onClose: () => void;
}

export const EditWeightLogDialog: FC<Props> = ({ weightLog, onClose }) => {
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [updateWeightLog, updateResult] = weightLogsApi.useUpdateMutation();
  const [deleteWeightLog, deleteResult] = weightLogsApi.useDeleteMutation();

  const { control, handleSubmit, formState } = useForm({
    mode: 'onChange',
    resolver: zodResolver(editSchema),
    defaultValues: { weight: weightLog.value },
  });

  const busy = updateResult.isLoading || deleteResult.isLoading;

  const onSubmit: SubmitHandler<EditFormValues> = async ({ weight }) => {
    const { error } = await updateWeightLog({ date: weightLog.date, body: { value: weight } });

    if (!error) {
      onClose();
    }
  };

  const handleDelete = async (): Promise<void> => {
    if (!confirmingDelete) {
      setConfirmingDelete(true);
      return;
    }

    const { error } = await deleteWeightLog(weightLog.date);

    if (!error) {
      onClose();
    }
  };

  return (
    <Dialog
      opened
      title={dateLib.formatToUserFriendlyString(weightLog.date)}
      onClose={onClose}
      content={
        <Stack spacing={2} sx={{ pt: 1 }}>
          <form id={formId} onSubmit={handleSubmit(onSubmit)}>
            <Controller
              name="weight"
              control={control}
              render={({ field, fieldState }) => (
                <TextField
                  {...field}
                  onFocus={event => {
                    event.target.select();
                  }}
                  label="體重"
                  placeholder="輸入你的體重"
                  fullWidth
                  autoFocus
                  error={!!fieldState.error}
                  helperText={fieldState.error?.message ?? ' '}
                  slotProps={{
                    input: {
                      endAdornment: <InputAdornment position="end">kg</InputAdornment>,
                    },
                    htmlInput: {
                      type: 'text',
                      inputMode: 'decimal',
                    },
                  }}
                />
              )}
            />
          </form>
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
          type="submit"
          form={formId}
          disabled={!formState.isValid || deleteResult.isLoading}
          loading={updateResult.isLoading}
        >
          儲存
        </Button>
      )}
    />
  );
};
