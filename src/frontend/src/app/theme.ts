import { createTheme } from '@mui/material';

declare module '@mui/material/styles' {
  // eslint-disable-next-line @typescript-eslint/no-empty-object-type
  interface Theme {}

  // eslint-disable-next-line @typescript-eslint/no-empty-object-type
  interface ThemeOptions {}
}

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1e3a8a',
    },
    success: {
      main: '#1565c0',
    },
  },
  typography: {
    h1: {
      fontSize: '2rem',
      fontWeight: 400,
    },
    h2: {
      fontSize: '1.5rem',
      fontWeight: 400,
    },
  },
});
