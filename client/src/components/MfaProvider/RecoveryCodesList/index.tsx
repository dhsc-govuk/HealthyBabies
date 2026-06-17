import React from 'react';
import { Box, Typography, Paper, Button, Grid } from '@mui/material';
import { styled } from '@mui/material/styles';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import DownloadIcon from '@mui/icons-material/Download';
import { RecoveryCodesListProps } from '../types';

const CodePaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(1.5),
  textAlign: 'center',
  fontFamily: '"GDS Transport", arial, sans-serif',
  fontSize: '1rem',
  fontWeight: 600,
  backgroundColor: theme.palette.grey[100],
  border: `1px solid ${theme.palette.grey[300]}`,
}));

const WarningBox = styled(Box)(({ theme }) => ({
  backgroundColor: '#fff3cd',
  border: '1px solid #ffc107',
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2),
  marginBottom: theme.spacing(3),
}));

const RecoveryCodesList = ({ codes, onClose }: RecoveryCodesListProps): React.ReactElement => {
  const handleCopy = (): void => {
    const codesText = codes.join('\n');
    navigator.clipboard.writeText(codesText);
  };

  const handleDownload = (): void => {
    const codesText = codes.join('\n');
    const blob = new Blob([codesText], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'recovery-codes.txt';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  return (
    <Box>
      <WarningBox>
        <Typography variant="body1" fontWeight={600} gutterBottom>
          Save these recovery codes
        </Typography>
        <Typography variant="body2">
          Each code can only be used once. Store them in a safe place. If you lose access to your
          authenticator app, you can use these codes to sign in.
        </Typography>
      </WarningBox>

      <Grid container spacing={1} sx={{ mb: 3 }}>
        {codes.map((code, index) => (
          <Grid item xs={6} sm={4} key={index}>
            <CodePaper elevation={0}>{code}</CodePaper>
          </Grid>
        ))}
      </Grid>

      <Box display="flex" gap={2} justifyContent="center" flexWrap="wrap">
        <Button variant="outlined" startIcon={<ContentCopyIcon />} onClick={handleCopy}>
          Copy codes
        </Button>
        <Button variant="outlined" startIcon={<DownloadIcon />} onClick={handleDownload}>
          Download codes
        </Button>
        {onClose && (
          <Button variant="contained" onClick={onClose}>
            I have saved my codes
          </Button>
        )}
      </Box>
    </Box>
  );
};

export default RecoveryCodesList;
