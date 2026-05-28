import { Paper, styled } from '@mui/material';

export const SearchIconContainer = styled('div')(({ theme }) => ({
  cursor: 'pointer',
  width: 24,
  height: 24,
  marginLeft: theme.spacing(1),
  marginRight: theme.spacing(1),
}));

export const SearchPaper = styled(Paper)(({ theme }) => ({
  marginLeft: theme.spacing(2),
  padding: theme.spacing(0.75),
}));
