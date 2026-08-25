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
import { ROUTES } from '../../../../../constants/routes';

const DataCollectionFormQuestionsList = (): React.ReactElement => {
  usePageTitle('Data collection form questions');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedModuleId, setSelectedModuleId] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [fieldToDelete, setFieldToDelete] = useState<FormField | null>(null);
  const [draggedFieldId, setDraggedFieldId] = useState<string | null>(null);
  const [dragOverFieldId, setDragOverFieldId] = useState<string | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const { data: allModules = [] } = useQuery({
    queryKey: ['dataCollectionFormModules'],
    queryFn: getAllModules,
  });
  const modules = useMemo(() => allModules.filter((m) => m.type === 'management-information'), [allModules]);

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

  // One normalised term drives both the filter and the reorder guard below. They must agree: a
  // guard that trims while the filter does not would treat a whitespace-only search as "no search"
  // and leave drag-and-drop enabled over a list that is quietly hiding every single-word question.
  const normalisedSearchTerm = searchTerm.trim().toLowerCase();

  const filteredFields = useMemo(() => {
    if (!selectedModule?.fields) return [];
    return selectedModule.fields.filter((field) => field.fieldKey.toLowerCase().includes(normalisedSearchTerm) || field.label.toLowerCase().includes(normalisedSearchTerm));
  }, [selectedModule?.fields, normalisedSearchTerm]);

  // Reordering a filtered list would renumber only the questions that happen to match the search,
  // silently reshuffling the ones hidden between them, so drag-and-drop is only offered on the
  // complete list.
  const isReorderable = normalisedSearchTerm === '';

  // Section number for a field, used to order both the accordions and the module-wide renumbering
  // below. A field with no section counts as 0, matching how the submission API sorts it, so the
  // admin list shows the same sequence LAs would see.
  const sectionNumberOf = useCallback((sectionId: string | null) => selectedModule?.sections.find((s) => s.id === sectionId)?.sectionNumber ?? 0, [selectedModule?.sections]);

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
    // Object key order is insertion order, which follows however the API happened to return the
    // fields. Rebuild the record in section order so the accordions read 1, 2, 3, ..., with any
    // sectionless questions first where the form would put them.
    return Object.fromEntries(Object.entries(groups).sort(([a], [b]) => sectionNumberOf(a === 'no-section' ? null : a) - sectionNumberOf(b === 'no-section' ? null : b)));
  }, [filteredFields, sectionNumberOf]);

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
    navigate(ROUTES.admin.configuration.dataCollectionFormQuestions.edit(fieldId));
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

  // A drop is only meaningful when one of our own rows is being moved within the section it
  // already belongs to: the reorder endpoint rewrites display_order and nothing else.
  const isDropAllowed = (target: FormField) => {
    if (!isReorderable || draggedFieldId === null) return false;
    const dragged = selectedModule?.fields.find((f) => f.id === draggedFieldId);
    return !!dragged && (dragged.formSectionId ?? null) === (target.formSectionId ?? null);
  };

  // The drag payload carries the question's id, not its row index. Indexes are per-accordion, so
  // an index from one section means something different in another, and a drag that started
  // outside the page carries no index at all - both used to be accepted and acted on.
  const handleDragStart = (e: DragEvent<HTMLDivElement>, field: FormField) => {
    setDraggedFieldId(field.id);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', field.id);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>, field: FormField) => {
    // Only accept the drop when it is one of our rows moving within its own section. Calling
    // preventDefault() unconditionally made every row a drop target for anything on the desktop.
    if (!isDropAllowed(field)) return;

    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    if (draggedFieldId !== field.id) {
      setDragOverFieldId(field.id);
    }
  };

  const handleDragLeave = () => {
    setDragOverFieldId(null);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>, target: FormField, sectionFields: FormField[]) => {
    e.preventDefault();
    const sourceId = e.dataTransfer.getData('text/plain') || draggedFieldId;
    const fromIndex = sectionFields.findIndex((f) => f.id === sourceId);
    const toIndex = sectionFields.findIndex((f) => f.id === target.id);

    // findIndex returns -1 for a foreign drag or one that started in a different section; the
    // reorder endpoint only rewrites display_order, so moving a question between sections is not
    // something this screen can express. Either way, do nothing rather than guess.
    if (fromIndex !== -1 && toIndex !== -1 && fromIndex !== toIndex && selectedModuleId && selectedModule) {
      const reorderedSection = [...sectionFields];
      const [removed] = reorderedSection.splice(fromIndex, 1);
      reorderedSection.splice(toIndex, 0, removed);

      // displayOrder is a single sequence per module, not per section. Numbering the dragged
      // section 1..n on its own collides with every other section, and the submission review
      // screens - which flatten the module into one list - then interleave questions from
      // different sections (BLUM-7219). So rebuild every section, swap in the dragged one, and
      // renumber the module end to end.
      const sectionKeyOf = (field: FormField) => field.formSectionId ?? 'no-section';
      const draggedSectionKey = sectionKeyOf(target);

      const bySection = new Map<string, FormField[]>();
      selectedModule.fields.forEach((field) => {
        const key = sectionKeyOf(field);
        const group = bySection.get(key);
        if (group) {
          group.push(field);
        } else {
          bySection.set(key, [field]);
        }
      });
      bySection.set(draggedSectionKey, reorderedSection);

      const fieldsWithNewOrder = [...bySection.entries()]
        // Comparing section numbers alone keeps this a total order. Sections that share a number
        // (nothing enforces uniqueness) compare equal and Array.prototype.sort is stable, so they
        // simply hold their current relative position instead of shuffling unpredictably.
        .sort(([a], [b]) => sectionNumberOf(a === 'no-section' ? null : a) - sectionNumberOf(b === 'no-section' ? null : b))
        .flatMap(([key, fields]) => (key === draggedSectionKey ? fields : [...fields].sort((x, y) => x.displayOrder - y.displayOrder)))
        .map((field, idx) => ({ id: field.id, displayOrder: idx + 1 }));

      reorderMutation.mutate({
        formModuleId: selectedModuleId,
        fields: fieldsWithNewOrder,
      });
    }

    setDraggedFieldId(null);
    setDragOverFieldId(null);
  };

  const handleDragEnd = () => {
    setDraggedFieldId(null);
    setDragOverFieldId(null);
  };

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.admin.home },
        { label: 'Settings', link: ROUTES.admin.settings },
      ]}>
      <PageHeaderContainer>
        <PageTitle>Data Collection Form Questions</PageTitle>
        <PageHeaderActions>
          <Button onClick={() => navigate(ROUTES.admin.configuration.dataCollectionFormQuestions.create)}>Add Question</Button>
        </PageHeaderActions>
      </PageHeaderContainer>
      <Box sx={{ mb: 3 }}>
        <Typography variant="h6" component="h2" sx={{ mb: 2 }}>
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

          {!isReorderable && (
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Clear the search to reorder questions.
            </Typography>
          )}

          {moduleLoading ? (
            <Typography>Loading questions...</Typography>
          ) : (
            Object.entries(groupedFields).map(([sectionId, fields]) => (
              <Accordion key={sectionId} defaultExpanded>
                <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                  <Typography variant="h6" component="h3">{getSectionTitle(sectionId)}</Typography>
                  <Chip label={fields.length} size="small" sx={{ ml: 2 }} />
                </AccordionSummary>
                <AccordionDetails>
                  {fields.map((field) => (
                    <Box
                      key={field.id}
                      draggable={isReorderable}
                      onDragStart={isReorderable ? (e) => handleDragStart(e, field) : undefined}
                      onDragOver={isReorderable ? (e) => handleDragOver(e, field) : undefined}
                      onDragLeave={isReorderable ? handleDragLeave : undefined}
                      onDrop={isReorderable ? (e) => handleDrop(e, field, fields) : undefined}
                      onDragEnd={isReorderable ? handleDragEnd : undefined}
                      sx={{
                        display: 'flex',
                        alignItems: 'center',
                        p: 2,
                        mb: 1,
                        border: '1px solid',
                        // Keyed on the field id, not the row index: indexes restart in every
                        // accordion, so the old comparison highlighted one row per section.
                        borderColor: dragOverFieldId === field.id ? 'primary.main' : 'grey.300',
                        borderRadius: 1,
                        backgroundColor: draggedFieldId === field.id ? 'action.hover' : 'background.paper',
                        opacity: draggedFieldId === field.id ? 0.5 : 1,
                        cursor: isReorderable ? 'grab' : 'default',
                        '&:active': { cursor: isReorderable ? 'grabbing' : 'default' },
                      }}>
                      <DragIndicatorIcon sx={{ color: isReorderable ? 'grey.500' : 'grey.300', mr: 2 }} />
                      <Box sx={{ flex: 1 }}>
                        <Typography variant="subtitle1" component="h4" sx={{ fontWeight: 600 }}>
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
