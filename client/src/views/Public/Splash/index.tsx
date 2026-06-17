import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { styled } from '@mui/material/styles';
import { Fade } from '@mui/material';
import { Logo } from '../../../components/Logos';

const Root = styled('div')(({ theme }) => ({
  height: '100%',
  width: '100%',
  backgroundColor: theme.palette.background.paper,
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
}));

const Icon = styled('div')(() => ({
  display: 'block',
  '& svg': {
    width: '28rem',
    height: '28rem',
    flex: '0 0 auto',
    padding: '0 3rem',
  },
}));

const LoadingText = styled('div')({
  fontSize: '24px',
  fontWeight: 500,
  color: '#333',
});

const Splash = (): React.ReactElement => {
  const navigate = useNavigate();
  useEffect(() => {
    let mounted = true;
    if (mounted) {
      setTimeout(() => {
        navigate('/sign-in', { replace: true });
      }, 5000);
    }
    return () => {
      mounted = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <Root>
      <Fade in timeout={2700}>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <Icon>
            <Logo />
          </Icon>
          <LoadingText>Loading Family Hubs...</LoadingText>
        </div>
      </Fade>
    </Root>
  );
};

export default Splash;
