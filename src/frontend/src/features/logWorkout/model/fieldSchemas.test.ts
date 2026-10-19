import { exerciseSchema, repsSchema, weightKgSchema } from './fieldSchemas';

describe('exerciseSchema', () => {
  test.each<[string, boolean, string | undefined]>([
    ['', false, undefined],
    ['   ', false, undefined],
    ['a'.repeat(101), false, undefined],
    ['  Bench Press ', true, 'Bench Press'],
    ['a'.repeat(100), true, 'a'.repeat(100)],
  ])(`parse '%s' -> %s, %s`, (data, expectedSuccess, expectedData) => {
    const result = exerciseSchema.safeParse(data);

    expect(result.success).toBe(expectedSuccess);
    expect(result.data).toBe(expectedData);
  });
});

describe('repsSchema', () => {
  test.each<[string | number, boolean, number | undefined]>([
    ['', false, undefined],
    ['abc', false, undefined],
    ['0', false, undefined],
    ['1001', false, undefined],
    ['8.5', false, undefined],
    [8.5, false, undefined],
    ['1', true, 1],
    [' 10 ', true, 10],
    [1000, true, 1000],
  ])(`parse '%s' -> %s, %s`, (data, expectedSuccess, expectedData) => {
    const result = repsSchema.safeParse(data);

    expect(result.success).toBe(expectedSuccess);
    expect(result.data).toBe(expectedData);
  });
});

describe('weightKgSchema', () => {
  test.each<[string | number, boolean, number | undefined]>([
    ['', false, undefined],
    ['abc', false, undefined],
    ['-1', false, undefined],
    ['1000.5', false, undefined],
    ['60.', false, undefined],
    ['0', true, 0],
    [0, true, 0],
    ['62,5', true, 62.5],
    ['62.5', true, 62.5],
    ['1000', true, 1000],
  ])(`parse '%s' -> %s, %s`, (data, expectedSuccess, expectedData) => {
    const result = weightKgSchema.safeParse(data);

    expect(result.success).toBe(expectedSuccess);
    expect(result.data).toBe(expectedData);
  });
});
