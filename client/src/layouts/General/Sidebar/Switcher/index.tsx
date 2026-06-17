import { Box, IconButton } from '@mui/material';
import { KeyboardArrowRight as KeyboardArrowRightIcon, KeyboardArrowLeft as KeyboardArrowLeftIcon } from '@mui/icons-material';
import { hiddenSidebarSpacing } from '../common';

interface Props {
  drawerWidth: number;
  open: boolean;
  setOpenDrawer: (open: boolean) => void;
}

const DrawerSwitch = ({ open, setOpenDrawer, drawerWidth }: Props) => {
  return (
    <Box
      id="drawer-switch-button"
      sx={(theme) => ({
        position: 'fixed',
        opacity: 0,
        zIndex: theme.zIndex.drawer + 1,
        transition: theme.transitions.create(['left', 'opacity'], {
          easing: theme.transitions.easing.sharp,
          duration: theme.transitions.duration.leavingScreen,
        }),
        left: open ? `calc(${drawerWidth}px - ${theme.spacing(2)})` : `calc(${theme.spacing(hiddenSidebarSpacing - 1)})`,
        ...(open && {
          transition: theme.transitions.create(['left', 'opacity'], {
            easing: theme.transitions.easing.sharp,
            duration: theme.transitions.duration.enteringScreen,
          }),
        }),
        bottom: theme.spacing(10),
      })}>
      <IconButton
        aria-label="open drawer"
        onClick={() => setOpenDrawer(!open)}
        sx={(theme) => ({
          background: theme.palette.primary.main,
          borderRadius: '50%',
          '&:hover': {
            background: theme.palette.primary.light,
          },
          '& svg': {
            fill: theme.palette.background.default,
            width: theme.spacing(2),
            height: theme.spacing(2),
          },
        })}>
        {open ? <KeyboardArrowLeftIcon /> : <KeyboardArrowRightIcon />}
      </IconButton>
    </Box>
  );
};

export default DrawerSwitch;
