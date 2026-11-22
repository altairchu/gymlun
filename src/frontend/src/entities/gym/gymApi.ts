import { api } from '@/shared/api';

export interface GymContextInput {
  goal: string | null;
  eatingApproach: string | null;
  targetWeightKg: number | null;
  targetKcal: number | null;
  proteinPerKg: number | null;
  targetCarbsG: number | null;
  targetFatG: number | null;
  ageYears: number | null;
  heightCm: number | null;
  sex: 'm' | 'f' | null;
  activityFactor: number | null;
  version?: number;
}

export interface GymContext extends GymContextInput {
  version: number;
  latestWeight: { date: string; weightKg: number; source: string } | null;
  dailyNeeds: {
    restingKcal: number | null;
    maintenanceKcal: number | null;
    targetProteinG: number | null;
    missing: string[];
    method: string | null;
  };
}

export interface BodyMeasurementInput {
  version?: number;
  date: string;
  source?: string | null;
  metrics: Record<string, number | null>;
  profile?: {
    ageYears?: number | null;
    heightCm?: number | null;
    waistCm?: number | null;
    hipCm?: number | null;
    bodyShape?: string | null;
    bodyType?: string | null;
  };
  segments?: Record<string, { fatMassKg?: number | null; muscleMassKg?: number | null }>;
  flags?: Record<string, string>;
}

export interface BodyMeasurementResult extends BodyMeasurementInput {
  id: number;
  version: number;
  derivedMetrics?: Record<string, number>;
}

export interface McpConnectionsResponse {
  serverUrl: string | null;
  connections: { id: string; clientName: string; authorizedAt: string }[];
}

const contextUrl = '/api/v1/gym-context';
const bodyUrl = '/api/v1/body-composition';
const connectionsUrl = '/api/v1/mcp-connections';

export const gymApi = api
  .enhanceEndpoints({
    addTagTypes: ['gymContext', 'bodyComposition', 'mcpConnection'],
  })
  .injectEndpoints({
    endpoints: create => ({
      getGymContext: create.query<GymContext, void>({
        query: () => contextUrl,
        providesTags: ['gymContext', 'weightLog'],
      }),
      updateGymContext: create.mutation<GymContext, GymContextInput>({
        query: body => ({ url: contextUrl, method: 'PUT', body }),
        invalidatesTags: ['gymContext'],
      }),
      getBodyComposition: create.query<BodyMeasurementResult[], void>({
        query: () => bodyUrl,
        providesTags: ['bodyComposition'],
      }),
      createBodyComposition: create.mutation<BodyMeasurementResult, BodyMeasurementInput>({
        query: body => ({ url: bodyUrl, method: 'POST', body }),
        invalidatesTags: ['gymContext', 'bodyComposition'],
      }),
      updateBodyComposition: create.mutation<
        BodyMeasurementResult,
        { id: number; body: BodyMeasurementInput }
      >({
        query: ({ id, body }) => ({ url: `${bodyUrl}/${id}`, method: 'PUT', body }),
        invalidatesTags: ['gymContext', 'bodyComposition'],
      }),
      deleteBodyComposition: create.mutation<void, number>({
        query: id => ({ url: `${bodyUrl}/${id}`, method: 'DELETE' }),
        invalidatesTags: ['gymContext', 'bodyComposition'],
      }),
      getMcpConnections: create.query<McpConnectionsResponse, void>({
        query: () => connectionsUrl,
        providesTags: ['mcpConnection'],
      }),
      revokeMcpConnection: create.mutation<void, string>({
        query: id => ({ url: `${connectionsUrl}/${encodeURIComponent(id)}`, method: 'DELETE' }),
        invalidatesTags: ['mcpConnection'],
      }),
    }),
  });

export const {
  useGetGymContextQuery,
  useUpdateGymContextMutation,
  useGetBodyCompositionQuery,
  useCreateBodyCompositionMutation,
  useUpdateBodyCompositionMutation,
  useDeleteBodyCompositionMutation,
  useGetMcpConnectionsQuery,
  useRevokeMcpConnectionMutation,
} = gymApi;
