import React from 'react';
import { ThemeProvider, createTheme } from '@mui/material/styles';

interface ThemeModes {
  primary: {
    main: string;
    light: string;
    contrastText: string;
  };
  secondary: {
    main: string;
    light: string;
    contrastText: string;
  };
  light: {
    background: {
      paper: string;
      default: string;
    };
    text: {
      primary: string;
      secondary: string;
    };
  };
  dark: {
    background: {
      paper: string;
      default: string;
    };
    text: {
      primary: string;
      secondary: string;
    };
  };
}

interface CustomThemeProviderProps {
  children: React.ReactNode;
  modes: ThemeModes;
  storageProvider?: unknown;
}

const CustomThemeProvider = ({ children, modes }: CustomThemeProviderProps): React.ReactElement => {
  const theme = createTheme({
    palette: {
      mode: 'light',
      primary: modes.primary,
      secondary: modes.secondary,
      background: modes.light.background,
      text: modes.light.text,
      // MUI's stock info (#0288d1) and warning (#ed6c02) give 3.86:1 and 3.11:1 against
      // white, so any filled Chip or Alert using them fails WCAG 1.4.3. These are the
      // GOV.UK palette equivalents, all at or above 4.5:1 with white text.
      info: { main: '#1d70b8', contrastText: '#ffffff' },
      warning: { main: '#a8480a', contrastText: '#ffffff' },
      error: { main: '#d4351c', contrastText: '#ffffff' },
      success: { main: '#00703c', contrastText: '#ffffff' },
    },
    typography: {
      fontFamily: '"GDS Transport", arial, sans-serif',
    },
  });

  return <ThemeProvider theme={theme}>{children}</ThemeProvider>;
};

export { CustomThemeProvider };
export type { ThemeModes };
export default CustomThemeProvider;
