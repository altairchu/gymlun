import AddIcon from '@mui/icons-material/Add';
import { type FC } from 'react';
import { useToggle } from '@/shared/hooks';
import { Button } from '@/shared/ui';
import { LogWorkoutDialog } from './LogWorkoutDialog';

interface Props {
  date: string;
}

export const LogWorkoutButton: FC<Props> = ({ date }) => {
  const [dialogVisible, toggleDialog] = useToggle();

  return (
    <>
      <Button startIcon={<AddIcon />} color="primary" variant="contained" onClick={toggleDialog}>
        記錄訓練
      </Button>
      {dialogVisible && <LogWorkoutDialog date={date} onClose={toggleDialog} />}
    </>
  );
};
