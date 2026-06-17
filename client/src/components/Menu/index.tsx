import React from 'react';

import { List, ListSubheader } from '@mui/material';
import MenuItem, { MenuItemProps } from './MenuItem';
import { HiddenList } from './styles';

interface Props {
  menuItems: MenuItemProps[];
  subHeader: string;
  sidebarOpen: boolean;
}

const AppMenu = ({ menuItems, subHeader, sidebarOpen }: Props): React.ReactElement => {
  const listProps = {
    component: 'nav',
    'aria-labelledby': 'menu-subheader',
    children: menuItems.map((item, index) => 
    <MenuItem
      {...item}
      sidebarOpen={sidebarOpen}
      key={`${subHeader}${index}`}
      />
    ),
  };

  return sidebarOpen ? (
    <List
      {...listProps}
      subheader={
        <ListSubheader
          sx={{
            lineHeight: 2,
            marginTop: (theme) => theme.spacing(1),
            paddingLeft: (theme) => theme.spacing(1),
            overflow: 'hidden',
            whiteSpace: 'nowrap',
          }}
          id="menu-subheader"
          component="nav">
          {subHeader}
        </ListSubheader>
      }
    />
  ) : (
    <HiddenList {...listProps} />
  );
};

export default AppMenu;
