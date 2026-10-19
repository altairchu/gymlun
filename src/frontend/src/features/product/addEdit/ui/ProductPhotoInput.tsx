import AddPhotoAlternateOutlinedIcon from '@mui/icons-material/AddPhotoAlternateOutlined';
import { Box, Button, Stack } from '@mui/material';
import { type ChangeEvent, type FC, useState } from 'react';
import { API_URL } from '@/shared/config';
import { convertToBase64String, imageLib } from '@/shared/lib';
import { type PhotoChange, PHOTO_REMOVED, PHOTO_UNCHANGED } from '../model';

interface Props {
  savedPhotoUrl: string | null;
  change: PhotoChange;
  disabled: boolean;
  onChange: (change: PhotoChange) => void;
}

const MAX_BYTES_SENT_AS_IS = 1024 * 1024;
const PREVIEW_SIZE = 72;

const shrink = async (file: File): Promise<Blob> => {
  if (file.size <= MAX_BYTES_SENT_AS_IS) {
    return file;
  }

  try {
    return await imageLib.resize(file, 1280, 1280, 0.85);
  } catch {
    // The server reads some formats a browser cannot draw
    return file;
  }
};

const previewUrlOf = (savedPhotoUrl: string | null, change: PhotoChange): string | null => {
  switch (change.type) {
    case 'set':
      return change.previewUrl;
    case 'removed':
      return null;
    case 'unchanged':
      return savedPhotoUrl === null ? null : `${API_URL}${savedPhotoUrl}`;
  }
};

export const ProductPhotoInput: FC<Props> = ({ savedPhotoUrl, change, disabled, onChange }) => {
  const [preparing, setPreparing] = useState(false);
  const previewUrl = previewUrlOf(savedPhotoUrl, change);

  const handleFileChange = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
    const file = event.target.files?.[0];
    // Lets the same file be chosen again after it was removed
    event.target.value = '';

    if (file === undefined) {
      return;
    }

    setPreparing(true);

    try {
      const photo = await shrink(file);
      onChange({ type: 'set', photo, previewUrl: await convertToBase64String(photo) });
    } finally {
      setPreparing(false);
    }
  };

  const handleRemove = (): void => {
    onChange(savedPhotoUrl === null ? PHOTO_UNCHANGED : PHOTO_REMOVED);
  };

  return (
    <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
      {previewUrl === null ? (
        <Box
          sx={{
            width: PREVIEW_SIZE,
            height: PREVIEW_SIZE,
            flexShrink: 0,
            display: 'grid',
            placeItems: 'center',
            borderRadius: 2,
            bgcolor: 'action.hover',
          }}
        >
          <AddPhotoAlternateOutlinedIcon color="action" />
        </Box>
      ) : (
        <Box
          component="img"
          src={previewUrl}
          alt="食物相片"
          sx={{
            width: PREVIEW_SIZE,
            height: PREVIEW_SIZE,
            flexShrink: 0,
            objectFit: 'cover',
            borderRadius: 2,
          }}
        />
      )}
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
        <Button
          component="label"
          variant="outlined"
          size="small"
          loading={preparing}
          disabled={disabled}
        >
          {previewUrl === null ? '加相' : '換相'}
          <input type="file" accept="image/*" hidden onChange={handleFileChange} />
        </Button>
        {previewUrl !== null && (
          <Button
            size="small"
            color="error"
            disabled={disabled || preparing}
            onClick={handleRemove}
          >
            移除相
          </Button>
        )}
      </Stack>
    </Stack>
  );
};
