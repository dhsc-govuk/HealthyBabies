import { Theme } from '@mui/material';
import { CSSObject } from '@emotion/react';

export const drawerWidth = 300;
export const hiddenSidebarSpacing = 10;

export const openedMixin = (theme: Theme): CSSObject => ({
  width: drawerWidth,
  transition: theme.transitions.create('width', {
    easing: theme.transitions.easing.sharp,
    duration: theme.transitions.duration.enteringScreen,
  }),
  boxSizing: 'border-box',
  boxShadow: '-2px 0px 4px 0px rgba(0, 0, 0, 0.15) inset',
  border: 0,
  overflow: 'hidden',
});

export const closedMixin = (theme: Theme): CSSObject => ({
  transition: theme.transitions.create('width', {
    easing: theme.transitions.easing.sharp,
    duration: theme.transitions.duration.leavingScreen,
  }),
  overflowX: 'hidden',
  width: `calc(${theme.spacing(hiddenSidebarSpacing)} + 1px)`,
  [theme.breakpoints.up('sm')]: {
    width: `calc(${theme.spacing(hiddenSidebarSpacing + 1)} + 1px)`,
  },
  boxSizing: 'border-box',
  boxShadow: '-2px 0px 4px 0px rgba(0, 0, 0, 0.15) inset',
  border: 0,
  overflow: 'hidden',
});
