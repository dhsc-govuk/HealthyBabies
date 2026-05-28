import React from 'react';
import { Box, Typography, Grid } from '@mui/material';
import { Button } from 'govuk-react';

interface DashboardCardProps {
  title: string;
  counter?: number;
  gridSize?: number | boolean | 'auto';
  editLabel?: string;
  description?: string;
  fill?: boolean;
  handleEdit?: () => void;
  handleView?: () => void;
}

const DashboardCard = ({
  title,
  counter,
  gridSize = 4,
  editLabel = 'Create',
  description,
  fill,
  handleEdit,
  handleView,
}: DashboardCardProps): React.ReactElement => {
  return (
    <Grid item xs={12} sm={6} md={gridSize}>
      <Box
        sx={{
          backgroundColor: '#ffffff',
          border: '1px solid #b1b4b6',
          borderLeft: '4px solid #1d70b8',
          padding: 3,
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Typography
          variant="h3"
          sx={{
            fontFamily: '"GDS Transport", arial, sans-serif',
            fontSize: '24px',
            fontWeight: 700,
            color: '#0b0c0c',
            mb: 1,
          }}
        >
          {title}
        </Typography>

        {counter !== undefined && (
          <Typography
            sx={{
              fontFamily: '"GDS Transport", arial, sans-serif',
              fontSize: '48px',
              fontWeight: 700,
              color: '#1d70b8',
              mb: 2,
            }}
          >
            {counter}
          </Typography>
        )}

        {description && (
          <Typography
            sx={{
              fontFamily: '"GDS Transport", arial, sans-serif',
              fontSize: '16px',
              color: '#505a5f',
              mb: 2,
              flexGrow: 1,
            }}
          >
            {description}
          </Typography>
        )}

        <Box sx={{ display: 'flex', gap: 2, mt: 'auto' }}>
          {handleEdit && (
            <Button onClick={handleEdit}>
              {editLabel}
            </Button>
          )}
          {handleView && (
            <Button buttonColour="#f3f2f1" buttonTextColour="#0b0c0c" onClick={handleView}>
              View all
            </Button>
          )}
        </Box>
      </Box>
    </Grid>
  );
};

export default DashboardCard;
