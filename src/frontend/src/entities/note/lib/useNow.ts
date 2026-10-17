import { useEffect, useState } from 'react';
import { dateLib } from '@/shared/lib';
import { minutesOfDate } from '../model';

interface Now {
  date: string;
  minutes: number;
}

const readNow = (): Now => {
  const now = new Date();
  return { date: dateLib.formatToISOStringWithoutTime(now), minutes: minutesOfDate(now) };
};

export const useNow = (): Now => {
  const [now, setNow] = useState(readNow);

  useEffect(() => {
    const interval = setInterval(() => setNow(readNow()), 30_000);
    return () => clearInterval(interval);
  }, []);

  return now;
};
