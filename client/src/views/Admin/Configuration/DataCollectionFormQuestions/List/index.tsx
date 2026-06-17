import React, { useState, useMemo, useCallback, DragEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import {
  Box,
  Typography,
  IconButton,
  TextField,
  InputAdornment,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
} from '@mui/material';
import { Search as SearchIcon, Edit as EditIcon, Delete as DeleteIcon, DragIndicator as DragIndicatorIcon, ExpandMore as ExpandMoreIcon } from '@mui/icons-material';
import { Button, GovUKButton } from '../../../../../components/GovUKComponents';
import { SettingsLayout } from '../../../../../layouts';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../../styles/govuk-global';
import { getAllModules, getModuleWithFields } from './queries';
import { deleteFormField, reorderFormFields } from './mutations';
import { FormField, fieldTypeLabels } from '../Common/types';
import usePageTitle from '../../../../../hooks/usePageTitle';

const DataCollectionFormQuestionsList = (): React.ReactElement => {
  usePageTitle('Data collection form questions');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedModuleId, setSelectedModuleId] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [fieldToDelete, setFieldToDelete] = useState<FormField | null>(null);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const { data: modules = [] } = useQuery({
    queryKey: ['dataCollectionFormModules'],
    queryFn: getAllModules,
  });

  const { data: selectedModule, isLoading: moduleLoading } = useQuery({
    queryKey: ['dataCollectionFormModule', selectedModuleId],
    queryFn: () => getModuleWithFields(selectedModuleId!),
    enabled: !!selectedModuleId,
  });

  const deleteMutation = useMutation({
    mutationFn: deleteFormField,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModule', selectedModuleId] });
      setDeleteDialogOpen(false);
      setFieldToDelete(null);
      setDeleteError(null);
    },
    onError: (error: any) => {
      if (error?.response?.status === 409) {
        setDeleteError('This question cannot be deleted because it has submitted values. Please deactivate the question instead.');
      } else {
        setDeleteError('An error occurred while deleting the question. Please try again.');
      }
    },
  });

  const reorderMutation = useMutation({
    mutationFn: reorderFormFields,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModule', selectedModuleId] });
    },
  });

  const filteredFields = useMemo(() => {
    if (!selectedModule?.fields) return [];
    return selectedModule.fields.filter((field) => field.fieldKey.toLowerCase().includes(searchTerm.toLowerCase()) || field.label.toLowerCase().includes(searchTerm.toLowerCase()));
  }, [selectedModule?.fields, searchTerm]);

  const groupedFields = useMemo(() => {
    const groups: Record<string, FormField[]> = {};
    filteredFields.forEach((field) => {
      const sectionId = field.formSectionId || 'no-section';
      if (!groups[sectionId]) {
        groups[sectionId] = [];
      }
      groups[sectionId].push(field);
    });
    Object.keys(groups).forEach((key) => {
      groups[key].sort((a, b) => a.displayOrder - b.displayOrder);
    });
    return groups;
  }, [filteredFields]);

  const getSectionTitle = useCallback(
    (sectionId: string) => {
      if (sectionId === 'no-section') return 'No Section';
      const section = selectedModule?.sections.find((s) => s.id === sectionId);
      return section ? `${section.sectionNumber}. ${section.title}` : 'Unknown Section';
    },
    [selectedModule?.sections]
  );

  const handleModuleSelect = (moduleId: string) => {
    setSelectedModuleId(moduleId);
    setSearchTerm('');
  };

  const handleEdit = (fieldId: string) => {
    navigate(`/admin/configuration/data-collection-form-questions/edit/${fieldId}`);
  };

  const handleDeleteClick = (field: FormField) => {
    setFieldToDelete(field);
    setDeleteDialogOpen(true);
    setDeleteError(null);
  };

  const handleDeleteConfirm = () => {
    if (fieldToDelete) {
      deleteMutation.mutate(fieldToDelete.id);
    }
  };

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

  const handleDrop = (e: DragEvent<HTMLDivElement>, toIndex: number, sectionFields: FormField[]) => {
    e.preventDefault();
    const fromIndex = parseInt(e.dataTransfer.getData('text/plain'), 10);
    if (fromIndex !== toIndex && selectedModuleId) {
      const reorderedFields = [...sectionFields];
      const [removed] = reorderedFields.splice(fromIndex, 1);
      reorderedFields.splice(toIndex, 0, removed);

      const fieldsWithNewOrder = reorderedFields.map((field, idx) => ({
        id: field.id,
        displayOrder: idx + 1,
      }));

      reorderMutation.mutate({
        formModuleId: selectedModuleId,
        fields: fieldsWithNewOrder,
      });
    }
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  const handleDragEnd = () => {
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: '/admin/home' },
        { label: 'Settings', link: '/admin/settings' },
      ]}>
      <PageHeaderContainer>
        <PageTitle>Data Collection Form Questions</PageTitle>
        <PageHeaderActions>
          <Button onClick={() => navigate('/admin/configuration/data-collection-form-questions/create')}>Add Question</Button>
        </PageHeaderActions>
      </PageHeaderContainer>
      <Box sx={{ mb: 3 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Select a Form Module
        </Typography>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
          {modules.map((module) => (
            <Chip
              key={module.id}
              label={module.name}
              onClick={() => handleModuleSelect(module.id)}
              color={selectedModuleId === module.id ? 'primary' : 'default'}
              variant={selectedModuleId === module.id ? 'filled' : 'outlined'}
            />
          ))}
        </Box>
      </Box>

      {selectedModuleId && (
        <>
          <TextField
            fullWidth
            placeholder="Search questions..."
            label="Search questions"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{ mb: 3 }}
          />

          {moduleLoading ? (
            <Typography>Loading questions...</Typography>
          ) : (
            Object.entries(groupedFields).map(([sectionId, fields]) => (
              <Accordion key={sectionId} defaultExpanded>
                <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                  <Typography variant="h6">{getSectionTitle(sectionId)}</Typography>
                  <Chip label={fields.length} size="small" sx={{ ml: 2 }} />
                </AccordionSummary>
                <AccordionDetails>
                  {fields.map((field, index) => (
                    <Box
                      key={field.id}
                      draggable
                      onDragStart={(e) => handleDragStart(e, index)}
                      onDragOver={(e) => handleDragOver(e, index)}
                      onDragLeave={handleDragLeave}
                      onDrop={(e) => handleDrop(e, index, fields)}
                      onDragEnd={handleDragEnd}
                      sx={{
                        display: 'flex',
                        alignItems: 'center',
                        p: 2,
                        mb: 1,
                        border: '1px solid',
                        borderColor: dragOverIndex === index ? 'primary.main' : 'grey.300',
                        borderRadius: 1,
                        backgroundColor: draggedIndex === index ? 'action.hover' : 'background.paper',
                        opacity: draggedIndex === index ? 0.5 : 1,
                        cursor: 'grab',
                        '&:active': { cursor: 'grabbing' },
                      }}>
                      <DragIndicatorIcon sx={{ color: 'grey.500', mr: 2 }} />
                      <Box sx={{ flex: 1 }}>
                        <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                          {field.fieldKey}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {field.label}
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
                          <Chip label={fieldTypeLabels[field.fieldType] || field.fieldType} size="small" variant="outlined" />
                          {field.isRequired && <Chip label="Required" size="small" color="warning" />}
                          {!field.isActive && <Chip label="Inactive" size="small" color="error" />}
                        </Box>
                      </Box>
                      <IconButton onClick={() => handleEdit(field.id)} color="primary" aria-label={`Edit ${field.label}`}>
                        <EditIcon />
                      </IconButton>
                      <IconButton onClick={() => handleDeleteClick(field)} color="error" aria-label={`Delete ${field.label}`}>
                        <DeleteIcon />
                      </IconButton>
                    </Box>
                  ))}
                </AccordionDetails>
              </Accordion>
            ))
          )}
        </>
      )}

      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>{deleteError ? 'Cannot Delete Question' : 'Delete Question'}</DialogTitle>
        <DialogContent>
          {deleteError ? (
            <DialogContentText sx={{ color: 'error.main' }}>{deleteError}</DialogContentText>
          ) : (
            <DialogContentText>Are you sure you want to delete the question "{fieldToDelete?.fieldKey}"? This action cannot be undone.</DialogContentText>
          )}
        </DialogContent>
        <DialogActions>
          {deleteError ? (
            <Button onClick={() => setDeleteDialogOpen(false)}>Close</Button>
          ) : (
            <>
              <GovUKButton className="govuk-button govuk-button--secondary" onClick={() => setDeleteDialogOpen(false)}>
                Cancel
              </GovUKButton>
              <GovUKButton onClick={handleDeleteConfirm} isLoading={deleteMutation.isLoading}>
                Delete
              </GovUKButton>
            </>
          )}
        </DialogActions>
      </Dialog>
    </SettingsLayout>
  );
};

export default DataCollectionFormQuestionsList;
