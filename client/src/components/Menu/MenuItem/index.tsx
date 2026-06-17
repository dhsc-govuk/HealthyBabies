import React, { useState } from 'react';

import { ExpandMore as ExpandMoreIcon, KeyboardArrowRight as KeyboardArrowRightIcon } from '@mui/icons-material';

import { Collapse, List, ListItemIcon, ListItemText, Popover, SvgIconTypeMap, Theme, Typography } from '@mui/material';

import MenuItemComponent from '../MenuItemComponent';
import { OverridableComponent } from '@mui/material/OverridableComponent';
import { TreeView, TreeItem } from '@mui/lab';
import { useLocation, useNavigate } from 'react-router-dom';

export interface MenuItemProps {
  name: string;
  items: MenuItemProps[];
  link?: string;
  action?: () => void;
  Icon?: OverridableComponent<SvgIconTypeMap<{}, 'svg'>> & {
    muiName: string;
  };
  sidebarOpen?: boolean;
}

const MenuItem = ({ name, link, items = [], Icon, sidebarOpen, action }: MenuItemProps): React.ReactElement => {
  const location = useLocation();
  const navigate = useNavigate();
  const isActive = Boolean(location.pathname === link!);

  const [open, setOpen] = useState(isActive);
  const [ref, setRef] = useState<HTMLElement | null>(null);

  const isExpandable = items && items.length > 0;

  const handleClick = (e: React.MouseEvent<HTMLElement>) => {
    setOpen(!open);
  };

  const handleRef = (e: React.MouseEvent<HTMLElement>) => {
    setRef(e.currentTarget);
  };

  const renderTree = (item: MenuItemProps) => {
    return (
      <TreeItem
        sx={{
          '& .MuiTreeItem-content': {
            borderRadius: (theme: Theme) => theme.spacing(1),
          },
        }}
        key={item.name}
        label={item.name}
        nodeId={item.name}
        {...(!item.items && item.link ? { onClick: () => navigate(item.link as string) } : {})}>
        {item.items && items.length && item.items.map((elem) => renderTree(elem))}
      </TreeItem>
    );
  };

  return (
    <>
      <MenuItemComponent link={link} action={action} onClick={handleClick} area-describedby={'menu-list-parent-item'} isActive={isActive} isExpandable={isExpandable}>
        {Icon && (
          <ListItemIcon
            sx={{
              minWidth: (theme) => theme.spacing(4),
              '& svg': { width: 18 },
            }}
            onMouseOver={handleRef}>
            <Icon />
          </ListItemIcon>
        )}
        <ListItemText
          sx={{
            '& .MuiTypography-root': { fontSize: 15 },
          }}
          primary={<Typography component="span">{name}</Typography>}
        />
        {isExpandable && sidebarOpen && (!open ? <KeyboardArrowRightIcon /> : <ExpandMoreIcon />)}
      </MenuItemComponent>
      {isExpandable && sidebarOpen && (
        <Collapse in={open} timeout="auto" unmountOnExit>
          <List component="div" disablePadding sx={{ ml: 2 }}>
            {items.map((item, index) => (
              <MenuItem key={`collapsibleMenuItem${index}`} {...item} sidebarOpen={sidebarOpen} />
            ))}
          </List>
        </Collapse>
      )}
      {isExpandable && !sidebarOpen && (
        <Popover
          id={'menu-list-parent-item'}
          open={open && Boolean(ref)}
          onClose={handleClick}
          anchorOrigin={{
            vertical: 'top',
            horizontal: 'right',
          }}
          anchorEl={ref}
          sx={{ '& .MuiPaper-root': { padding: (theme) => theme.spacing(1) } }}>
          <TreeView defaultCollapseIcon={<ExpandMoreIcon />} defaultExpandIcon={<KeyboardArrowRightIcon />}>
            {renderTree({ name, link, items })}
          </TreeView>
        </Popover>
      )}
    </>
  );
};

export default MenuItem;
