import { List, styled } from '@mui/material';

export const HiddenList = styled(List)(({ theme }) => ({
  '& .MuiListItemButton-root': {
    borderRadius: '50%',
    margin: `0 auto ${theme.spacing(1)} auto`,
    width: theme.spacing(6),
    height: theme.spacing(6),
    padding: `calc(${theme.spacing(3)} / 2)`,
    '& .MuiListItemIcon-root': {
      minWidth: 0,
      ' svg': {
        width: theme.spacing(3),
        height: theme.spacing(3),
      },
    },
    '& span.MuiTypography-root': {
      display: 'none',
    },
  },
}));
