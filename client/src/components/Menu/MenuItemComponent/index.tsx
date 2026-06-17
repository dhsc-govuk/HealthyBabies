import React from 'react';
import { useNavigate } from 'react-router-dom';

import { ListItemButton } from '@mui/material';
import { ActiveItem } from './styles';

interface Props {
  children?: React.ReactNode;
  isActive: boolean;
  isExpandable: boolean;
  link?: string;
  action?: () => void;
  onClick?: (event: React.MouseEvent<HTMLElement>) => void;
}

const MenuItemComponent = ({ link, children, isActive, isExpandable, onClick, action }: Props) => {
  const navigate = useNavigate();

  return isActive ? (
    <ActiveItem
      children={children}
      sx={{
        '& .MuiListItemText-root': {
          overflow: 'hidden',
          whiteSpace: 'nowrap',
        },
      }}
      {...(isExpandable
        ? { onClick: onClick }
        : {
            ...(link ? { onClick: () => navigate(link) } : { onClick: action ?? onClick }),
          })}
    />
  ) : (
    <ListItemButton
      children={children}
      sx={{ marginBottom: (theme) => `calc(${theme.spacing(1)} / 2)` }}
      {...(isExpandable
        ? { onClick: onClick }
        : {
            ...(link ? { onClick: () => navigate(link) } : { onClick: action ?? onClick }),
          })}
    />
  );
};

export default MenuItemComponent;
