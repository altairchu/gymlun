import { Alert, Button, LinearProgress } from '@mui/material';
import { type FC, type FormEvent, useState } from 'react';
import {
  type BodyMeasurementInput,
  type BodyMeasurementResult,
  useCreateBodyCompositionMutation,
  useDeleteBodyCompositionMutation,
  useGetBodyCompositionQuery,
  useUpdateBodyCompositionMutation,
} from '@/entities/gym/gymApi';
import { dateLib } from '@/shared/lib';
import { PageContainer } from '@/shared/ui';
import { bodyFieldGroups, bodyProfileFields, bodyRegions, segmentFields } from './bodyFields';
import { BodyNavigation } from './BodyNavigation';
import { formatValue, readNumber, readText } from './fields';

const metrics = bodyFieldGroups.flatMap(([, fields]) => [...fields]);

interface FormProps {
  entry: BodyMeasurementResult | null;
  onClose: () => void;
  onSaved: () => void;
}

const BodyCompositionForm: FC<FormProps> = ({ entry, onClose, onSaved }) => {
  const [create, { isLoading: isCreating }] = useCreateBodyCompositionMutation();
  const [update, { isLoading: isUpdating }] = useUpdateBodyCompositionMutation();
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setError(null);
    const form = new FormData(event.currentTarget);
    const date = readText(form, 'date');
    if (date === null) {
      setError('請填寫量度日期。');
      return;
    }

    const body: BodyMeasurementInput = {
      ...(entry ? { version: entry.version } : {}),
      date,
      source: readText(form, 'source'),
      metrics: Object.fromEntries(
        metrics
          .map(([key]) => [key, readNumber(form, key)] as const)
          .filter(([, value]) => value !== null),
      ),
      profile: {
        ageYears: readNumber(form, 'ageYears'),
        heightCm: readNumber(form, 'heightCm'),
        waistCm: readNumber(form, 'waistCm'),
        hipCm: readNumber(form, 'hipCm'),
        bodyShape: readText(form, 'bodyShape'),
        bodyType: readText(form, 'bodyType'),
      },
      segments: Object.fromEntries(
        bodyRegions.map(([region]) => [
          region,
          {
            fatMassKg: readNumber(form, `${region}-fatMassKg`),
            muscleMassKg: readNumber(form, `${region}-muscleMassKg`),
          },
        ]),
      ),
      ...(entry?.flags ? { flags: entry.flags } : {}),
    };

    if (
      Object.keys(body.metrics).length === 0 &&
      Object.values(body.segments ?? {}).every(
        segment => segment.fatMassKg === null && segment.muscleMassKg === null,
      )
    ) {
      setError('請填寫至少一項量度數值。');
      return;
    }

    try {
      await (entry ? update({ id: entry.id, body }) : create(body)).unwrap();
      onSaved();
    } catch {
      setError('未能儲存量度。請檢查數值後再試。');
    }
  };

  const today = dateLib.formatToISOStringWithoutTime(dateLib.getCurrentDate());

  return (
    <form
      className="gym-plan-form gym-card"
      onSubmit={event => {
        void handleSubmit(event);
      }}
    >
      <h3>{entry ? '修改身體量度' : '新增身體量度'}</h3>
      <p className="gym-muted">填寫手上有嘅數字，冇量度嘅欄位可以留空。</p>
      <div className="gym-form-grid">
        <label>
          量度日期
          <input name="date" type="date" required max={today} defaultValue={entry?.date ?? today} />
        </label>
        <label>
          來源或儀器
          <input name="source" maxLength={200} defaultValue={entry?.source ?? ''} />
        </label>
      </div>
      {bodyFieldGroups.map(([title, fields], index) => (
        <details
          className="gym-measurement-module"
          key={title}
          open={index === 0 ? true : undefined}
        >
          <summary>{title}</summary>
          <div className="gym-form-grid">
            {fields.map(([key, label, unit]) => (
              <label key={key}>
                {unit ? `${label}（${unit}）` : label}
                <input
                  name={key}
                  type="number"
                  min={key === 'weightKg' ? 0.01 : 0}
                  max={unit === '%' ? 100 : key === 'weightKg' ? 500 : 15000}
                  step="any"
                  inputMode="decimal"
                  defaultValue={entry?.metrics[key] ?? ''}
                />
              </label>
            ))}
          </div>
        </details>
      ))}
      <details className="gym-measurement-module">
        <summary>量度資料同體型</summary>
        <div className="gym-form-grid">
          {bodyProfileFields.map(([key, label, unit]) => (
            <label key={key}>
              {label}（{unit}）
              <input
                name={key}
                type="number"
                min={key === 'heightCm' ? 30 : key === 'ageYears' ? 0 : 0.01}
                max={key === 'ageYears' ? 125 : key === 'heightCm' ? 300 : 500}
                step={key === 'ageYears' ? 1 : 'any'}
                inputMode="decimal"
                defaultValue={entry?.profile?.[key] ?? ''}
              />
            </label>
          ))}
          <label>
            體型
            <input
              name="bodyShape"
              maxLength={100}
              defaultValue={entry?.profile?.bodyShape ?? ''}
            />
          </label>
          <label>
            身體類型
            <input name="bodyType" maxLength={100} defaultValue={entry?.profile?.bodyType ?? ''} />
          </label>
        </div>
      </details>
      <details className="gym-measurement-module">
        <summary>分部脂肪同肌肉</summary>
        <div className="gym-form-grid">
          {bodyRegions.flatMap(([region, regionLabel]) =>
            segmentFields.map(([key, label]) => (
              <label key={`${region}-${key}`}>
                {regionLabel}
                {label}（kg）
                <input
                  name={`${region}-${key}`}
                  type="number"
                  min={0}
                  max={500}
                  step="any"
                  inputMode="decimal"
                  defaultValue={entry?.segments?.[region]?.[key] ?? ''}
                />
              </label>
            )),
          )}
        </div>
      </details>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <div className="gym-action-row">
        <Button disabled={isCreating || isUpdating} onClick={onClose}>
          取消
        </Button>
        <Button type="submit" variant="contained" disabled={isCreating || isUpdating}>
          儲存量度
        </Button>
      </div>
    </form>
  );
};

interface RecordProps {
  entry: BodyMeasurementResult;
  onEdit: (entry: BodyMeasurementResult) => void;
  onDelete: (entry: BodyMeasurementResult) => void;
}

const BodyRecord: FC<RecordProps> = ({ entry, onEdit, onDelete }) => {
  const recordedMetrics = metrics.flatMap(([key, label, unit]) => {
    const value = entry.metrics[key] ?? entry.derivedMetrics?.[key];
    return value === null || value === undefined
      ? []
      : [{ key, label, unit, value, calculated: entry.metrics[key] == null }];
  });
  const profileRows = bodyProfileFields.flatMap(([key, label, unit]) => {
    const value = entry.profile?.[key];
    return value === null || value === undefined ? [] : [{ key, label, unit, value }];
  });
  const segmentRows = bodyRegions.flatMap(([region, regionLabel]) =>
    segmentFields.flatMap(([key, label]) => {
      const value = entry.segments?.[region]?.[key];
      return value === null || value === undefined
        ? []
        : [{ key: `${region}-${key}`, label: `${regionLabel}${label}`, value, unit: 'kg' }];
    }),
  );

  return (
    <details className="gym-card gym-record">
      <summary>
        <time dateTime={entry.date}>{entry.date}</time>
        {entry.source ? <span>{entry.source}</span> : null}
      </summary>
      <dl className="gym-measurement-grid">
        {[...recordedMetrics, ...profileRows, ...segmentRows].map(row => (
          <div key={row.key}>
            <dt>{row.label}</dt>
            <dd>
              {formatValue(row.value, row.unit)}
              {'calculated' in row && row.calculated === true ? '（計算）' : ''}
              {entry.flags?.[row.key] ? (
                <small className="gym-measurement-flag">{entry.flags[row.key]}</small>
              ) : null}
            </dd>
          </div>
        ))}
        {entry.profile?.bodyShape ? (
          <div>
            <dt>體型</dt>
            <dd>{entry.profile.bodyShape}</dd>
          </div>
        ) : null}
        {entry.profile?.bodyType ? (
          <div>
            <dt>身體類型</dt>
            <dd>{entry.profile.bodyType}</dd>
          </div>
        ) : null}
      </dl>
      <div className="gym-action-row">
        <Button color="error" onClick={() => onDelete(entry)}>
          刪除
        </Button>
        <Button onClick={() => onEdit(entry)}>修改</Button>
      </div>
    </details>
  );
};

export const BodyCompositionPage: FC = () => {
  const { data, isError, isLoading, refetch } = useGetBodyCompositionQuery();
  const [remove, { isLoading: isDeleting }] = useDeleteBodyCompositionMutation();
  const [editor, setEditor] = useState<BodyMeasurementResult | 'new' | null>(null);
  const [deleteEntry, setDeleteEntry] = useState<BodyMeasurementResult | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const confirmDelete = async (): Promise<void> => {
    if (deleteEntry === null) {
      return;
    }

    try {
      await remove(deleteEntry.id).unwrap();
      setDeleteEntry(null);
      setNotice('已刪除量度');
    } catch {
      setNotice('未能刪除量度。請再試。');
    }
  };

  const entries = data?.toSorted((a, b) => b.date.localeCompare(a.date) || b.id - a.id);

  return (
    <PageContainer className="gym-feature-page">
      <BodyNavigation />
      <div className="gym-feature-heading">
        <h2>身體量度</h2>
        <Button
          variant="contained"
          onClick={() => {
            setEditor('new');
            setNotice(null);
          }}
        >
          新增量度
        </Button>
      </div>
      {notice ? (
        <p className="gym-notice" role="status">
          {notice}
        </p>
      ) : null}
      {editor !== null ? (
        <BodyCompositionForm
          key={editor === 'new' ? 'new' : editor.id}
          entry={editor === 'new' ? null : editor}
          onClose={() => setEditor(null)}
          onSaved={() => {
            setEditor(current => (current === editor ? null : current));
            setNotice('已儲存量度');
          }}
        />
      ) : null}
      {deleteEntry ? (
        <Alert
          severity="warning"
          action={
            <>
              <Button onClick={() => setDeleteEntry(null)}>取消</Button>
              <Button
                color="error"
                disabled={isDeleting}
                onClick={() => {
                  void confirmDelete();
                }}
              >
                刪除
              </Button>
            </>
          }
        >
          刪除 {deleteEntry.date} 呢次身體量度？
        </Alert>
      ) : null}
      {isLoading ? <LinearProgress aria-label="讀取身體量度" /> : null}
      {isError ? (
        <Alert
          severity="error"
          action={
            <Button
              onClick={() => {
                void refetch();
              }}
            >
              重試
            </Button>
          }
        >
          未能讀取身體量度
        </Alert>
      ) : null}
      {entries?.length === 0 ? <p className="gym-card gym-muted">仲未有身體量度記錄。</p> : null}
      {entries?.map(entry => (
        <BodyRecord key={entry.id} entry={entry} onEdit={setEditor} onDelete={setDeleteEntry} />
      ))}
    </PageContainer>
  );
};
