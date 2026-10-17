import { createTheme, CssBaseline, ThemeProvider } from '@mui/material';
import { zhHK as materialLocale } from '@mui/material/locale';
import { StyledEngineProvider } from '@mui/material/styles';
import { LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { zhHK as pickersLocale } from '@mui/x-date-pickers/locales';
import { type Store } from '@reduxjs/toolkit';
import { type PropsWithChildren, type FC } from 'react';
import { Provider } from 'react-redux';
import { dateLib } from '@/shared/lib';
import { theme } from './theme';

interface Props {
  store: Store;
}

const localizedTheme = createTheme(theme, materialLocale, pickersLocale, {
  components: {
    MuiPickersCalendarHeader: {
      defaultProps: { format: dateLib.DateFormat.MonthAndYear },
    },
  },
});

export const RootProvider: FC<PropsWithChildren<Props>> = ({ children, store }) => {
  return (
    <StyledEngineProvider injectFirst>
      <ThemeProvider theme={localizedTheme}>
        <LocalizationProvider dateAdapter={AdapterDateFns} adapterLocale={dateLib.locale}>
          <Provider store={store}>
            <CssBaseline />
            {children}
          </Provider>
        </LocalizationProvider>
      </ThemeProvider>
    </StyledEngineProvider>
  );
};
