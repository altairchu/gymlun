let expectedAccountId: string | null = null;

export const getExpectedGymAccount = (): string | null => expectedAccountId;

export const verifyGymAccount = (accountId: string | null | undefined): boolean => {
  if (expectedAccountId !== null && expectedAccountId !== accountId) {
    return false;
  }

  expectedAccountId = accountId ?? null;
  return true;
};
