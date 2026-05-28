import { ListItemButton, styled } from '@mui/material';

export const ActiveItem = styled(ListItemButton)(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: '#ffffff',
  marginBottom: `calc(${theme.spacing(1)} / 2)`,
  '& svg': {
    fill: '#ffffff',
  },
  '&:hover': {
    backgroundColor: theme.palette.primary.dark,
  },
}));
