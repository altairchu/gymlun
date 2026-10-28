import { GET } from './wake-server';

const fetchMock = vi.fn<typeof fetch>();

beforeEach(() => {
  vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
  vi.unstubAllGlobals();
  fetchMock.mockReset();
});

test('wakes the API by requesting its health check', async () => {
  fetchMock.mockResolvedValue(new Response('Healthy', { status: 200 }));

  const response = await GET();

  expect(response.status).toBe(204);
  expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
    'https://gymlun-api.onrender.com/healthz/live',
    expect.objectContaining<Partial<RequestInit>>({
      signal: expect.any(AbortSignal) as AbortSignal,
    }),
  );
});

test('reports an API that answers with an error', async () => {
  fetchMock.mockResolvedValue(new Response(null, { status: 503 }));

  const response = await GET();

  expect(response.status).toBe(502);
});

test('reports an API that cannot be reached', async () => {
  fetchMock.mockRejectedValue(new TypeError('fetch failed'));

  const response = await GET();

  expect(response.status).toBe(502);
});
