import { styled } from '@mui/material/styles';
import { Paper } from '@mui/material';

export const Header = styled('div')(({ theme }) => ({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  minHeight: 60,
  marginBottom: theme.spacing(1),
}));

export const SearchContainer = styled('div')(() => ({
  display: 'flex',
  alignItems: 'center',
}));

export const SearchPaper = styled(Paper)(({ theme }) => ({
  marginLeft: theme.spacing(2),
  padding: theme.spacing(0.75),
}));

export const SearchIconContainer = styled('div')(({ theme }) => ({
  cursor: 'pointer',
  width: 24,
  height: 24,
  marginLeft: theme.spacing(1),
  marginRight: theme.spacing(1),
}));

export const StyledTable = styled('table')(() => ({
  width: '100%',
  borderCollapse: 'collapse',
}));

export const StyledTh = styled('th')(() => ({
  textAlign: 'left',
  padding: '8px',
  borderBottom: '1px solid #d8dde0',
}));

export const StyledTd = styled('td')(() => ({
  padding: '8px',
  borderBottom: '1px solid #d8dde0',
}));

export const ActionButton = styled('button')(({ theme }) => ({
  marginRight: theme.spacing(1),
  '&:last-child': {
    marginRight: 0,
  },
}));
