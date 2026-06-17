import React from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material';
import GovUKButton from '../GovUKButton';
import './styles.css';

interface DeleteConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  itemName?: string;
  onConfirm: () => void;
  onCancel: () => void;
  isLoading?: boolean;
}

const DeleteConfirmDialog = ({
  open,
  title,
  message,
  itemName,
  onConfirm,
  onCancel,
  isLoading = false,
}: DeleteConfirmDialogProps): React.ReactElement => {
  return (
    <Dialog open={open} onClose={onCancel} maxWidth="sm" fullWidth>
      <DialogTitle className="delete-confirm-dialog__title">{title}</DialogTitle>
      <DialogContent className="delete-confirm-dialog__content">
        <p className="govuk-body">{message}</p>
        {itemName && (
          <p className="govuk-body delete-confirm-dialog__item-name">
            {itemName}
          </p>
        )}
      </DialogContent>
      <DialogActions className="delete-confirm-dialog__actions">
        <GovUKButton 
          className="govuk-button govuk-button--secondary" 
          onClick={onCancel} 
          disabled={isLoading}
        >
          Cancel
        </GovUKButton>
        <GovUKButton
          className="govuk-button govuk-button--warning"
          onClick={onConfirm}
          isLoading={isLoading}
        >
          Remove
        </GovUKButton>
      </DialogActions>
    </Dialog>
  );
};

export default DeleteConfirmDialog;
