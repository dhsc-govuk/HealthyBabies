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
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Typography,
  Chip,
  Stack,
  Paper,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import DragIndicatorIcon from '@mui/icons-material/DragIndicator';
import { Button, LoadingSpinner, GovUKButton, useGovUKNotification } from '../../../../../components/GovUKComponents';
import { getAllServiceFormQuestions } from './queries';
import { deleteServiceFormQuestion, reorderServiceFormQuestions } from './mutations';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { SettingsLayout } from '../../../../../layouts';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../../styles/govuk-global';
import '../../../../../components/GovUKComponents/GovUKTable/styles.css';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { ServiceFormQuestion, questionTypeLabels } from '../Common/types';
import usePageTitle from '../../../../../hooks/usePageTitle';

const ServiceFormQuestionsList = (): React.ReactElement => {
  usePageTitle('Service form questions');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [itemToDelete, setItemToDelete] = useState<ServiceFormQuestion | null>(null);
  const [draggedItem, setDraggedItem] = useState<ServiceFormQuestion | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<{ step: number; index: number } | null>(null);
  const [searchTerm, setSearchTerm] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['service-form-questions-list'],
    queryFn: getAllServiceFormQuestions,
  });

  const { mutateAsync: deleteMutation, isLoading: deleting } = useMutation({
    mutationKey: ['service-form-question-delete'],
    mutationFn: deleteServiceFormQuestion,
    onSuccess() {
      queryClient.invalidateQueries(['service-form-questions-list']);
      setNotification({ type: 'success', title: 'Question deleted successfully' });
      setDeleteDialogOpen(false);
      setItemToDelete(null);
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const { mutateAsync: reorderMutation } = useMutation({
    mutationKey: ['service-form-questions-reorder'],
    mutationFn: reorderServiceFormQuestions,
    onSuccess() {
      queryClient.invalidateQueries(['service-form-questions-list']);
      setNotification({ type: 'success', title: 'Questions reordered successfully' });
    },
    onError(error) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const filteredQuestions = useMemo(() => {
    let questions = data?.data ?? [];
    
    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      questions = questions.filter(
        (q) =>
          q.code.toLowerCase().includes(term) ||
          q.label.toLowerCase().includes(term)
      );
    }
    
    return questions;
  }, [data, searchTerm]);

  const questionsByStep = useMemo(() => {
    return {
      step1: filteredQuestions.filter((q) => q.step === 1).sort((a, b) => a.displayOrder - b.displayOrder),
      step2: filteredQuestions.filter((q) => q.step === 2).sort((a, b) => a.displayOrder - b.displayOrder),
    };
  }, [filteredQuestions]);

  const handleEdit = (id: string) => {
    navigate(`/admin/configuration/service-form-questions/${id}/edit`);
  };

  const handleCreate = () => {
    navigate('/admin/configuration/service-form-questions/create');
  };

  const handleDeleteClick = (item: ServiceFormQuestion) => {
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

  const handleDragStart = (e: DragEvent<HTMLDivElement>, item: ServiceFormQuestion) => {
    setDraggedItem(item);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', item.id);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>, step: number, index: number) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    if (draggedItem && draggedItem.step === step) {
      setDragOverIndex({ step, index });
    }
  };

  const handleDragLeave = () => {
    setDragOverIndex(null);
  };

  const handleDrop = useCallback(
    async (e: DragEvent<HTMLDivElement>, step: number, toIndex: number) => {
      e.preventDefault();
      if (!draggedItem || draggedItem.step !== step) {
        setDraggedItem(null);
        setDragOverIndex(null);
        return;
      }

      const questions = step === 1 ? [...questionsByStep.step1] : [...questionsByStep.step2];
      const fromIndex = questions.findIndex((q) => q.id === draggedItem.id);

      if (fromIndex !== toIndex) {
        const [removed] = questions.splice(fromIndex, 1);
        questions.splice(toIndex, 0, removed);

        const reorderData = {
          step,
          questions: questions.map((q, i) => ({ id: q.id, displayOrder: i + 1 })),
        };
        await reorderMutation(reorderData);
      }

      setDraggedItem(null);
      setDragOverIndex(null);
    },
    [draggedItem, questionsByStep, reorderMutation]
  );

  const handleDragEnd = () => {
    setDraggedItem(null);
    setDragOverIndex(null);
  };

  const renderQuestionCard = (question: ServiceFormQuestion, step: number, index: number) => {
    const isDragging = draggedItem?.id === question.id;
    const isDragOver = dragOverIndex?.step === step && dragOverIndex?.index === index;

    return (
      <Paper
        key={question.id}
        draggable
        onDragStart={(e) => handleDragStart(e, question)}
        onDragOver={(e) => handleDragOver(e, step, index)}
        onDragLeave={handleDragLeave}
        onDrop={(e) => handleDrop(e, step, index)}
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
            <Typography variant="subtitle1" fontWeight={600}>
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
            Edit
          </Button>
          {!question.isPredefined && (
            <Button size="small" color="error" onClick={() => handleDeleteClick(question)}>
              Delete
            </Button>
          )}
        </Stack>
      </Paper>
    );
  };

  return (
    <LoadingSpinner loading={isLoading || deleting} label={isLoading ? 'Loading questions' : 'Deleting question'}>
      <SettingsLayout
        breadcrumbs={[
          { label: 'Home', link: '/admin/home' },
          { label: 'Settings', link: '/admin/settings' },
        ]}>
        <PageHeaderContainer>
          <PageTitle>Service Form</PageTitle>
          <PageHeaderActions>
            <GovUKButton onClick={handleCreate}>
              Add Question
            </GovUKButton>
          </PageHeaderActions>
        </PageHeaderContainer>

        <div className="govuk-table-filter">
          <div>
            <label htmlFor="search-input" className="govuk-table-filter__label">
              Search service form questions
            </label>
            <div className="govuk-table-filter__search">
              <input
                id="search-input"
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="govuk-table-filter__input"
              />
              <button
                type="button"
                className="govuk-table-filter__search-button"
                aria-label="Search"
              >
                <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                  <circle cx="8" cy="8" r="6" stroke="white" strokeWidth="2" fill="none" />
                  <line x1="12.5" y1="12.5" x2="18" y2="18" stroke="white" strokeWidth="2" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        <Grid container spacing={4}>
          <Grid item xs={12}>
            <Accordion defaultExpanded>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="h6">Step 1 - Service Details ({questionsByStep.step1.length})</Typography>
              </AccordionSummary>
              <AccordionDetails>
                {questionsByStep.step1.length === 0 ? (
                  <Typography color="text.secondary">No questions in this step</Typography>
                ) : (
                  questionsByStep.step1.map((q, i) => renderQuestionCard(q, 1, i))
                )}
              </AccordionDetails>
            </Accordion>
            <Accordion defaultExpanded sx={{ mt: 2 }}>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Typography variant="h6">Step 2 - Additional Information ({questionsByStep.step2.length})</Typography>
              </AccordionSummary>
              <AccordionDetails>
                {questionsByStep.step2.length === 0 ? (
                  <Typography color="text.secondary">No questions in this step</Typography>
                ) : (
                  questionsByStep.step2.map((q, i) => renderQuestionCard(q, 2, i))
                )}
              </AccordionDetails>
            </Accordion>
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

    </LoadingSpinner>
  );
};

export default ServiceFormQuestionsList;
