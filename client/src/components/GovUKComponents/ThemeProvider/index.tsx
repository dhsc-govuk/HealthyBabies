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
