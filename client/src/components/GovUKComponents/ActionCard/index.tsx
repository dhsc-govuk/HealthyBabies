import React from 'react';
import { Box, Grid } from '@mui/material';
import { Add as AddIcon } from '@mui/icons-material';

interface ActionCardProps {
  gridSize?: number | boolean | 'auto';
  fill?: boolean;
  onClick?: () => void;
  ariaLabel?: string;
}

const ActionCard = ({
  gridSize = 4,
  fill = false,
  onClick,
  ariaLabel = 'Add new item',
}: ActionCardProps): React.ReactElement => {
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onClick?.();
    }
  };

  return (
    <Grid item xs={12} sm={6} md={gridSize}>
      <Box
        role="button"
        tabIndex={0}
        aria-label={ariaLabel}
        onClick={onClick}
        onKeyDown={handleKeyDown}
        sx={{
          backgroundColor: fill ? '#1d70b8' : '#ffffff',
          border: '2px dashed #1d70b8',
          borderRadius: 1,
          padding: 3,
          height: '100%',
          minHeight: 150,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          cursor: 'pointer',
          transition: 'all 0.2s ease',
          '&:hover, &:focus': {
            backgroundColor: fill ? '#003078' : '#f3f2f1',
            outline: '3px solid #ffdd00',
            outlineOffset: 0,
          },
        }}
      >
        <AddIcon
          aria-hidden="true"
          sx={{
            fontSize: 48,
            color: fill ? '#ffffff' : '#1d70b8',
          }}
        />
      </Box>
    </Grid>
  );
};

export default ActionCard;
