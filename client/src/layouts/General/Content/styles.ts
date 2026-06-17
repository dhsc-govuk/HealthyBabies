import { Box, styled } from '@mui/material';

export const Root = styled(Box)(({ theme }) => ({
  minHeight: '100vh',
  padding: `${theme.spacing(10)} ${theme.spacing(3)}`,
  flexGrow: 1,
  zIndex: theme.zIndex.drawer - 1,
  transition: theme.transitions.create(['width', 'margin'], {
    easing: theme.transitions.easing.sharp,
    duration: theme.transitions.duration.leavingScreen,
  }),
  '& .MuiDataGrid-root': {
    background: theme.palette.primary.contrastText,
  },
}));
