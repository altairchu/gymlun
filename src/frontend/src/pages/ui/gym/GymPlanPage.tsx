import { Alert, Button, LinearProgress } from '@mui/material';
import { type FC, type FormEvent, useState } from 'react';
import {
  type GymContext,
  useGetGymContextQuery,
  useUpdateGymContextMutation,
} from '@/entities/gym/gymApi';
import { PageContainer } from '@/shared/ui';
import { formatValue, readNumber, readText } from './fields';

const goals = [
  ['lose', '減脂'],
  ['maintain', '維持'],
  ['gain', '增重'],
  ['custom', '自訂'],
] as const;

const targetFields = [
  ['targetKcal', '每日攝取目標（kcal）', 1, 15000, 1],
  ['targetWeightKg', '目標體重（kg）', 1, 500, 0.01],
  ['proteinPerKg', '每公斤體重蛋白質（g）', 0.01, 10, 0.01],
  ['targetCarbsG', '每日碳水（g）', 0, 2000, 0.1],
  ['targetFatG', '每日脂肪（g）', 0, 2000, 0.1],
] as const;

const missingLabels: Record<string, string> = {
  weightKg: '體重',
  weight: '體重',
  ageYears: '年齡',
  heightCm: '身高',
  sex: '公式使用嘅性別',
  activityFactor: '活動量',
};

interface PlanFormProps {
  context: GymContext;
  onRefresh: () => Promise<GymContext>;
}

const PlanForm: FC<PlanFormProps> = ({ context, onRefresh }) => {
  const [draftSource, setDraftSource] = useState(context);
  const [save, { isLoading }] = useUpdateGymContextMutation();
  const [feedback, setFeedback] = useState<'saved' | 'error' | 'conflict' | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const refresh = async (): Promise<void> => {
    setIsRefreshing(true);
    try {
      setDraftSource(await onRefresh());
      setFeedback(null);
    } catch {
      setFeedback('error');
    } finally {
      setIsRefreshing(false);
    }
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const sex = readText(form, 'sex');
    setFeedback(null);

    try {
      const saved = await save({
        version: draftSource.version,
        goal: readText(form, 'goal'),
        eatingApproach: readText(form, 'eatingApproach'),
        targetWeightKg: readNumber(form, 'targetWeightKg'),
        targetKcal: readNumber(form, 'targetKcal'),
        proteinPerKg: readNumber(form, 'proteinPerKg'),
        targetCarbsG: readNumber(form, 'targetCarbsG'),
        targetFatG: readNumber(form, 'targetFatG'),
        ageYears: readNumber(form, 'ageYears'),
        heightCm: readNumber(form, 'heightCm'),
        sex: sex === 'm' || sex === 'f' ? sex : null,
        activityFactor: readNumber(form, 'activityFactor'),
      }).unwrap();
      setDraftSource(saved);
      setFeedback('saved');
    } catch (error) {
      const conflict =
        typeof error === 'object' && error !== null && 'status' in error && error.status === 409;
      setFeedback(conflict ? 'conflict' : 'error');
    }
  };

  return (
    <form
      key={draftSource.version}
      className="gym-plan-form"
      onSubmit={event => {
        void handleSubmit(event);
      }}
    >
      <fieldset className="gym-plan-fieldset">
        <legend>目標同飲食做法</legend>
        <label>
          目標
          <select name="goal" defaultValue={draftSource.goal ?? ''}>
            <option value="">未設定</option>
            {goals.map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </label>
        <label>
          目前飲食做法
          <textarea
            name="eatingApproach"
            rows={3}
            maxLength={1000}
            defaultValue={draftSource.eatingApproach ?? ''}
          />
        </label>
      </fieldset>
      <fieldset className="gym-plan-fieldset">
        <legend>每日營養目標</legend>
        <div className="gym-form-grid">
          {targetFields.map(([key, label, min, max, step]) => (
            <label key={key}>
              {label}
              <input
                name={key}
                type="number"
                min={min}
                max={max}
                step={step}
                inputMode="decimal"
                defaultValue={draftSource[key] ?? ''}
              />
            </label>
          ))}
        </div>
      </fieldset>
      <fieldset className="gym-plan-fieldset">
        <legend>估算每日所需</legend>
        <div className="gym-form-grid">
          <label>
            年齡
            <input
              name="ageYears"
              type="number"
              inputMode="numeric"
              min={18}
              max={125}
              defaultValue={draftSource.ageYears ?? ''}
            />
          </label>
          <label>
            身高（cm）
            <input
              name="heightCm"
              type="number"
              inputMode="decimal"
              min={50}
              max={300}
              step={0.1}
              defaultValue={draftSource.heightCm ?? ''}
            />
          </label>
          <label>
            公式使用嘅性別
            <select name="sex" defaultValue={draftSource.sex ?? ''}>
              <option value="">未設定</option>
              <option value="m">男</option>
              <option value="f">女</option>
            </select>
          </label>
          <label>
            活動量
            <input
              name="activityFactor"
              list="gym-activity-levels"
              type="number"
              step="any"
              min={1}
              max={3}
              inputMode="decimal"
              defaultValue={draftSource.activityFactor ?? ''}
            />
            <datalist id="gym-activity-levels">
              <option value="1.2" label="久坐" />
              <option value="1.375" label="輕度" />
              <option value="1.55" label="中度" />
              <option value="1.725" label="高度" />
              <option value="1.9" label="非常活躍" />
            </datalist>
          </label>
        </div>
      </fieldset>
      {feedback === 'conflict' || context.version > draftSource.version ? (
        <Alert
          severity="warning"
          action={
            <Button
              disabled={isRefreshing}
              onClick={() => {
                void refresh();
              }}
            >
              載入最新設定
            </Button>
          }
        >
          其他裝置或者 AI 已修改設定。載入最新版本會清除呢次未儲存嘅更改。
        </Alert>
      ) : null}
      {feedback === 'saved' ? <Alert severity="success">已儲存目標</Alert> : null}
      {feedback === 'error' ? (
        <Alert severity="error">未能儲存目標。請檢查欄位，或者重新載入最新版本再試。</Alert>
      ) : null}
      <div>
        <Button type="submit" variant="contained" disabled={isLoading}>
          儲存目標
        </Button>
      </div>
    </form>
  );
};

export const GymPlanPage: FC = () => {
  const { data, isError, isLoading, refetch } = useGetGymContextQuery();

  return (
    <PageContainer className="gym-feature-page gym-plan">
      <h2>目標同飲食</h2>
      {isLoading ? <LinearProgress aria-label="讀取目標" /> : null}
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
          未能讀取目標
        </Alert>
      ) : null}
      {data ? (
        <div className="gym-plan-layout">
          <PlanForm context={data} onRefresh={() => refetch().unwrap()} />
          <div className="gym-plan-sidebar">
            <dl className="gym-plan-summary">
              <div>
                <dt>估算每日維持熱量</dt>
                <dd>{formatValue(data.dailyNeeds.maintenanceKcal, 'kcal')}</dd>
              </div>
              <div>
                <dt>靜息消耗估算</dt>
                <dd>{formatValue(data.dailyNeeds.restingKcal, 'kcal')}</dd>
              </div>
              <div>
                <dt>每日攝取目標</dt>
                <dd>{formatValue(data.targetKcal, 'kcal')}</dd>
              </div>
              <div>
                <dt>蛋白質目標</dt>
                <dd>{formatValue(data.dailyNeeds.targetProteinG, 'g')}</dd>
              </div>
            </dl>
            <p className="gym-plan-method">
              {data.latestWeight
                ? `最新體重 ${formatValue(data.latestWeight.weightKg, 'kg')}（${data.latestWeight.date}）。`
                : '未有體重記錄。'}
              {data.dailyNeeds.missing.length > 0
                ? `計算所需資料：${data.dailyNeeds.missing.map(key => missingLabels[key] ?? key).join('、')}。`
                : '維持熱量按個人資料同活動量估算。'}
            </p>
          </div>
        </div>
      ) : null}
    </PageContainer>
  );
};
