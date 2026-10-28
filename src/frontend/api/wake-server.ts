// The API host stops sending itself keep-awake requests overnight and then sleeps; Vercel calls this each morning
// (crons in vercel.json) so the first visit of the day does not wait for it to start
const API_HEALTH_CHECK_URL = 'https://gymlun-api.onrender.com/healthz/live';

// Waking the API host can take about a minute
const WAKE_TIMEOUT_MS = 110_000;

export const GET = async (): Promise<Response> => {
  try {
    const response = await fetch(API_HEALTH_CHECK_URL, {
      signal: AbortSignal.timeout(WAKE_TIMEOUT_MS),
    });
    return new Response(null, { status: response.ok ? 204 : 502 });
  } catch {
    return new Response(null, { status: 502 });
  }
};
