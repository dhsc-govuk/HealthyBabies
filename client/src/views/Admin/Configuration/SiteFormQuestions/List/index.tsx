import React, { useState, useMemo, useCallback, DragEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Grid,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Typography,
  Chip,
  Stack,
  Paper,
} from '@mui/material';
import DragIndicatorIcon from '@mui/icons-material/DragIndicator';
import { Button, LoadingSpinner, GovUKButton, useGovUKNotification } from '../../../../../components/GovUKComponents';
import { getAllSiteFormQuestions } from './queries';
import { deleteSiteFormQuestion, reorderSiteFormQuestions } from './mutations';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { SiteFormQuestion, questionTypeLabels } from '../Common/types';
import { ROUTES } from '../../../../../constants/routes';

const SiteFormQuestionsList = (): React.ReactElement => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [itemToDelete, setItemToDelete] = useState<SiteFormQuestion | null>(null);
  const [draggedItem, setDraggedItem] = useState<SiteFormQuestion | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['site-form-questions-list'],
    queryFn: getAllSiteFormQuestions,
  });

  const { mutateAsync: deleteMutation, isLoading: deleting } = useMutation({
    mutationKey: ['site-form-question-delete'],
    mutationFn: deleteSiteFormQuestion,
    onSuccess() {
      queryClient.invalidateQueries(['site-form-questions-list']);
      setNotification({ type: 'success', title: 'Question deleted successfully' });
      setDeleteDialogOpen(false);
      setItemToDelete(null);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const { mutateAsync: reorderMutation } = useMutation({
    mutationKey: ['site-form-questions-reorder'],
    mutationFn: reorderSiteFormQuestions,
    onSuccess() {
      queryClient.invalidateQueries(['site-form-questions-list']);
      setNotification({ type: 'success', title: 'Questions reordered successfully' });
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const sortedQuestions = useMemo(() => {
    const questions = data?.data ?? [];
    return [...questions].sort((a, b) => a.displayOrder - b.displayOrder);
  }, [data]);

  const handleEdit = (id: string) => {
    navigate(ROUTES.admin.configuration.siteFormQuestions.edit(id));
  };

  const handleCreate = () => {
    navigate(ROUTES.admin.configuration.siteFormQuestions.create);
  };

  const handleDeleteClick = (item: SiteFormQuestion) => {
    setItemToDelete(item);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (itemToDelete) {
      await deleteMutation(itemToDelete.id);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false);
    setItemToDelete(null);
  };

  const handleDragStart = (e: DragEvent<HTMLDivElement>, item: SiteFormQuestion) => {
    setDraggedItem(item);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', item.id);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>, index: number) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    if (draggedItem) {
      setDragOverIndex(index);
    }
  };

  const handleDragLeave = () => {
    setDragOverIndex(null);
  };

  const handleDrop = useCallback(
    async (e: DragEvent<HTMLDivElement>, toIndex: number) => {
      e.preventDefault();
      if (!draggedItem) {
        setDraggedItem(null);
        setDragOverIndex(null);
        return;
      }

      const questions = [...sortedQuestions];
      const fromIndex = questions.findIndex((q) => q.id === draggedItem.id);

      if (fromIndex !== toIndex) {
        const [removed] = questions.splice(fromIndex, 1);
        questions.splice(toIndex, 0, removed);

        const reorderData = {
          questions: questions.map((q, i) => ({ id: q.id, displayOrder: i + 1 })),
        };
        await reorderMutation(reorderData);
      }

      setDraggedItem(null);
      setDragOverIndex(null);
    },
    [draggedItem, sortedQuestions, reorderMutation]
  );

  const handleDragEnd = () => {
    setDraggedItem(null);
    setDragOverIndex(null);
  };

  const renderQuestionCard = (question: SiteFormQuestion, index: number) => {
    const isDragging = draggedItem?.id === question.id;
    const isDragOver = dragOverIndex === index;

    return (
      <Paper
        key={question.id}
        draggable
        onDragStart={(e) => handleDragStart(e, question)}
        onDragOver={(e) => handleDragOver(e, index)}
        onDragLeave={handleDragLeave}
        onDrop={(e) => handleDrop(e, index)}
        onDragEnd={handleDragEnd}
        elevation={isDragging ? 4 : 1}
        sx={{
          p: 2,
          mb: 1,
          display: 'flex',
          alignItems: 'center',
          gap: 2,
          cursor: 'grab',
          opacity: isDragging ? 0.5 : 1,
          borderLeft: isDragOver ? '4px solid' : '4px solid transparent',
          borderLeftColor: isDragOver ? 'primary.main' : 'transparent',
          transition: 'all 0.2s ease',
          '&:active': { cursor: 'grabbing' },
        }}>
        <DragIndicatorIcon sx={{ color: 'grey.500' }} />
        <Box sx={{ flex: 1 }}>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
            <Typography variant="subtitle1" component="h3" fontWeight={600}>
              {question.code}
            </Typography>
            <Chip size="small" label={questionTypeLabels[question.questionType]} color="primary" variant="outlined" />
            {!question.isActive && <Chip size="small" label="Inactive" color="warning" />}
            {question.isPredefined && <Chip size="small" label="Predefined" color="info" />}
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {question.label}
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          <Button size="small" onClick={() => handleEdit(question.id)}>
            {/* Every row repeats "Edit"/"Delete", so on their own a screen reader has no way to
                tell which question the button acts on — the question name is only plain text.
                Naming it here gives each button a unique accessible name (WCAG 2.4.4). */}
            Edit<span className="govuk-visually-hidden"> {question.label}</span>
          </Button>
          {!question.isPredefined && (
            <Button size="small" color="error" onClick={() => handleDeleteClick(question)}>
              Delete<span className="govuk-visually-hidden"> {question.label}</span>
            </Button>
          )}
        </Stack>
      </Paper>
    );
  };

  return (
    <>
      <SettingsLayout
        breadcrumbs={[
          { label: 'Admin', link: ROUTES.admin.home },
          { label: 'Configuration', link: ROUTES.admin.configuration.root },
        ]}
        currentPage="Delivery Location Form Questions">
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6" component="h2">
            Site Form Questions ({sortedQuestions.length})
          </Typography>
          <Button onClick={handleCreate} variant="secondary">
            Create Question
          </Button>
        </Box>
        <Grid container spacing={4}>
          <Grid item xs={12}>
            {sortedQuestions.length === 0 ? (
              <Typography color="text.secondary">No questions configured</Typography>
            ) : (
              sortedQuestions.map((q, i) => renderQuestionCard(q, i))
            )}
          </Grid>
        </Grid>
      </SettingsLayout>

      <Dialog open={deleteDialogOpen} onClose={handleDeleteCancel}>
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete the question "{itemToDelete?.code} - {itemToDelete?.label}"? This action
            cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <GovUKButton 
            className="govuk-button govuk-button--secondary" 
            onClick={handleDeleteCancel}
          >
            Cancel
          </GovUKButton>
          <GovUKButton 
            className="govuk-button govuk-button--warning" 
            onClick={handleDeleteConfirm}
          >
            Delete
          </GovUKButton>
        </DialogActions>
      </Dialog>

      {isLoading && <LoadingSpinner label="Loading questions" />}
      {deleting && <LoadingSpinner label="Deleting question" />}
    </>
  );
};

export default SiteFormQuestionsList;
