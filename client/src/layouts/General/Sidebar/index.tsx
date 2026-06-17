import { Box, CircularProgress, Divider, Drawer, Grid, Toolbar } from '@mui/material';
import AppMenu from '../../../components/Menu';
import DrawerSwitch from './Switcher';
import { closedMixin, drawerWidth, openedMixin } from './common';
import { Logout as LogoutIcon, People as UsersIcon, Tune as ConfigIcon } from '@mui/icons-material';
import { EnumUserRole, useAuthProvider } from '../../../components/AuthProvider';
import { MenuItemProps } from '../../../components/Menu/MenuItem';
import { listItems } from '../../../components/DashboardProviderArgs';
import SettingsIcon from '@mui/icons-material/Settings';

interface Props {
  open: boolean;
  setOpenDrawer: (open: boolean) => void;
}

const Sidebar = ({ open, setOpenDrawer }: Props) => {
  const { userRole, signOut } = useAuthProvider();

  const menu =
    listItems
      .find((item) => item.name === userRole)
      ?.payload.items
      .map((item) => ({
        name: item.label,
        link: item.to,
        Icon: item.icon,
        items: [],
      })) ?? [];

  const footerMenu = [
    ...(userRole === EnumUserRole.ADMIN
      ? [
          {
            name: 'User Management',
            Icon: UsersIcon,
            link: '/admin/la-users',
          },
          {
            name: 'Configuration',
            Icon: ConfigIcon,
            link: '/admin/configuration/lookup-data',
          },
        ]
      : []),
    ...(userRole === EnumUserRole.ORGANISATION_ADMIN
      ? [
          {
            name: 'Settings',
            Icon: SettingsIcon,
            link: '/organisation-admin/settings',
          },
        ]
      : []),
    {
      name: 'Log out',
      Icon: LogoutIcon,
      action: () => {
        signOut();
      },
    },
  ].filter(Boolean) as MenuItemProps[];

  return (
    <Drawer
      variant="permanent"
      open={open}
      sx={(theme) => ({
        '& .MuiToolbar-root': {
          minHeight: '50px',
        },
        width: drawerWidth,
        flexShrink: 0,
        border: 0,
        ...(open
          ? {
              ...openedMixin(theme),
              '& .MuiDrawer-paper': openedMixin(theme),
            }
          : {
              ...closedMixin(theme),
              '& .MuiDrawer-paper': closedMixin(theme),
            }),
        '&:hover #drawer-switch-button': {
          opacity: 1,
        },
      })}>
      <Toolbar />
      <Box
        sx={(theme) => ({
          padding: theme.spacing(2),
          height: '100%',
        })}>
        {userRole ? (
          <Grid container direction="column" justifyContent="space-between" height="100%">
            <Grid width={'100%'} item>
              <AppMenu sidebarOpen={open} subHeader="" menuItems={menu} />
            </Grid>
            <Grid width={'100%'} item>
              <Divider />
              <AppMenu sidebarOpen={open} subHeader="Platform management" menuItems={footerMenu} />
            </Grid>
          </Grid>
        ) : (
          <Box
            sx={{
              display: 'flex',
              width: '100%',
              height: '30vh',
              alignItems: 'center',
              justifyContent: 'center',
              ml: 2,
            }}>
            <CircularProgress
              sx={{
                height: '64px !important',
                width: '64px !important',
                color: '#FF612B',
              }}
            />
          </Box>
        )}
      </Box>
      <DrawerSwitch open={open} setOpenDrawer={setOpenDrawer} drawerWidth={drawerWidth} />
    </Drawer>
  );
};

export default Sidebar;
