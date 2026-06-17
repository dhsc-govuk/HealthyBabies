import React, { useCallback, DragEvent, useState } from 'react';
import { Box, IconButton, Stack, Typography } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import DragIndicatorIcon from '@mui/icons-material/DragIndicator';
import { Button, GovUKFieldset } from '../../../../../components/GovUKComponents';
import { ServiceFormQuestionOption } from './types';
import { QuestionFormAction } from './reducer';

interface Props {
  options: ServiceFormQuestionOption[];
  dispatch: React.Dispatch<any>;
  error?: string | boolean;
}

const OptionsManager = ({ options, dispatch, error }: Props): React.ReactElement => {
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);

  const handleAddOption = useCallback(() => {
    dispatch({ type: QuestionFormAction.ADD_OPTION });
  }, [dispatch]);

  const handleRemoveOption = useCallback(
    (index: number) => {
      dispatch({ type: QuestionFormAction.REMOVE_OPTION, index });
    },
    [dispatch]
  );

  const handleUpdateOption = useCallback(
    (index: number, field: 'value' | 'label', value: string) => {
      dispatch({ type: QuestionFormAction.UPDATE_OPTION, index, field, value });
    },
    [dispatch]
  );

  const handleDragStart = (e: DragEvent<HTMLDivElement>, index: number) => {
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', index.toString());
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>, index: number) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    if (draggedIndex !== null && draggedIndex !== index) {
      setDragOverIndex(index);
    }
  };

  const handleDragLeave = () => {
    setDragOverIndex(null);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>, toIndex: number) => {
    e.preventDefault();
    const fromIndex = parseInt(e.dataTransfer.getData('text/plain'), 10);
    if (fromIndex !== toIndex) {
      dispatch({ type: QuestionFormAction.REORDER_OPTIONS, fromIndex, toIndex });
    }
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  const handleDragEnd = () => {
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  return (
    <Box sx={{ mt: 2 }}>
      <Typography variant="h6" sx={{ mb: 1, fontWeight: 600 }}>
        Options
      </Typography>
      {error && (
        <Typography color="error" sx={{ mb: 1 }}>
          {String(error)}
        </Typography>
      )}
      <Stack spacing={1}>
        {options.map((option, index) => (
          <Box
            key={index}
            draggable
            onDragStart={(e) => handleDragStart(e, index)}
            onDragOver={(e) => handleDragOver(e, index)}
            onDragLeave={handleDragLeave}
            onDrop={(e) => handleDrop(e, index)}
            onDragEnd={handleDragEnd}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              p: 1,
              border: '1px solid',
              borderColor: dragOverIndex === index ? 'primary.main' : 'grey.300',
              borderRadius: 1,
              backgroundColor: draggedIndex === index ? 'action.hover' : 'background.paper',
              opacity: draggedIndex === index ? 0.5 : 1,
              cursor: 'grab',
              '&:active': {
                cursor: 'grabbing',
              },
            }}>
            <DragIndicatorIcon sx={{ color: 'grey.500', cursor: 'grab' }} />
            <Box sx={{ flex: 1, '& .nhsuk-form-group': { mb: 0 } }}>
              <GovUKFieldset.Input
                id={`option-value-${index}`}
                name={`option-value-${index}`}
                label="Value"
                value={option.value}
                onChange={(e) => handleUpdateOption(index, 'value', e.target.value)}
              />
            </Box>
            <Box sx={{ flex: 1, '& .nhsuk-form-group': { mb: 0 } }}>
              <GovUKFieldset.Input
                id={`option-label-${index}`}
                name={`option-label-${index}`}
                label="Label"
                value={option.label}
                onChange={(e) => handleUpdateOption(index, 'label', e.target.value)}
              />
            </Box>
            <IconButton
              onClick={() => handleRemoveOption(index)}
              color="error"
              aria-label="Remove option"
              disabled={options.length === 1}>
              <DeleteIcon />
            </IconButton>
          </Box>
        ))}
      </Stack>
      <Box sx={{ mt: 2 }}>
        <Button variant="secondary" onClick={handleAddOption}>
          Add Option
        </Button>
      </Box>
    </Box>
  );
};

export default OptionsManager;
