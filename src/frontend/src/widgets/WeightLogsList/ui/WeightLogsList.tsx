import EditIcon from '@mui/icons-material/Edit';
import { List, ListSubheader, ListItem, ListItemButton, ListItemText } from '@mui/material';
import { type FC, useState } from 'react';
import { type GetWeightLogsRequest, type WeightLogItem, weightLogsApi } from '@/entities/weightLog';
import { EditWeightLogDialog, LogWeightButton } from '@/features/logWeight';
import { dateLib } from '@/shared/lib';

interface Props {
  weightLogsRequest: GetWeightLogsRequest;
}

export const WeightLogsList: FC<Props> = ({ weightLogsRequest }) => {
  const [editedWeightLog, setEditedWeightLog] = useState<WeightLogItem | null>(null);

  const { weightLogs } = weightLogsApi.useWeightLogsQuery(weightLogsRequest, {
    selectFromResult: ({ data }) => ({
      weightLogs: data?.weightLogs ?? [],
    }),
  });

  return (
    <>
      <List>
        <ListSubheader
          disableGutters
          sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
        >
          <span>最近記錄</span>
          <LogWeightButton weightLogsRequest={weightLogsRequest} />
        </ListSubheader>
        {weightLogs.length === 0 && (
          <ListItem disableGutters disablePadding>
            <ListItemText
              primary="你尚未記錄任何體重"
              slotProps={{
                primary: { color: 'textSecondary' },
              }}
            />
          </ListItem>
        )}
        {weightLogs.map(weightLog => {
          const weight = `${weightLog.value} kg`;
          const date = dateLib.formatToUserFriendlyString(weightLog.date);

          return (
            <ListItem
              key={weightLog.date}
              disableGutters
              disablePadding
              aria-label={`${date}：${weight}`}
            >
              <ListItemButton
                disableGutters
                aria-label={`修改${date}：${weight}`}
                onClick={() => setEditedWeightLog(weightLog)}
              >
                <ListItemText primary={weight} secondary={date} />
                <EditIcon fontSize="small" color="action" />
              </ListItemButton>
            </ListItem>
          );
        })}
      </List>
      {editedWeightLog && (
        <EditWeightLogDialog weightLog={editedWeightLog} onClose={() => setEditedWeightLog(null)} />
      )}
    </>
  );
};
