import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import {
  Box,
  Typography,
  IconButton,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  ExpandMore as ExpandMoreIcon,
  Add as AddIcon,
} from '@mui/icons-material';
import { Button, GovUKButton, useGovUKNotification } from '../../../../../components/GovUKComponents';
import { SettingsLayout } from '../../../../../layouts';
import { PageHeaderContainer, PageHeaderActions, PageTitle } from '../../../../../styles/govuk-global';
import { getAllModules, getModuleWithFields } from './queries';
import {
  createModule,
  updateModule,
  deleteModule,
  changeModuleType,
  createSection,
  updateSection,
  deleteSection,
  CreateModuleRequest,
  UpdateModuleRequest,
  ChangeModuleTypeRequest,
  CreateSectionRequest,
  UpdateSectionRequest,
} from './mutations';
import { FormModule, FormModuleType, FormSection, formModuleTypeOptions } from '../Common/types';
import usePageTitle from '../../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../../constants/routes';

const TABS = [
  { label: 'Management Information', type: 'management-information', href: '#management-information' },
  { label: 'Delivery Plan', type: 'delivery-plan', href: '#delivery-plan' },
] as const;

const DataCollectionFormModules = (): React.ReactElement => {
  usePageTitle('Form modules');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { setNotification } = useGovUKNotification();

  const [tabIndex, setTabIndex] = useState(0);
  const [selectedModuleId, setSelectedModuleId] = useState<string | null>(null);
  const [moduleDialogOpen, setModuleDialogOpen] = useState(false);
  const [sectionDialogOpen, setSectionDialogOpen] = useState(false);
  const [deleteModuleDialogOpen, setDeleteModuleDialogOpen] = useState(false);
  const [deleteSectionDialogOpen, setDeleteSectionDialogOpen] = useState(false);
  const [editingModule, setEditingModule] = useState<FormModule | null>(null);
  const [editingSection, setEditingSection] = useState<FormSection | null>(null);
  const [moduleToDelete, setModuleToDelete] = useState<FormModule | null>(null);
  const [sectionToDelete, setSectionToDelete] = useState<FormSection | null>(null);

  const [moduleForm, setModuleForm] = useState<CreateModuleRequest & { code: string }>({ code: '', name: '', description: '', type: TABS[0].type });
  const [sectionForm, setSectionForm] = useState<CreateSectionRequest>({ title: '', description: '', helpText: '', helpUrl: '' });

  const { data: modules = [] } = useQuery({
    queryKey: ['dataCollectionFormModules'],
    queryFn: getAllModules,
  });

  const { data: selectedModule } = useQuery({
    queryKey: ['dataCollectionFormModule', selectedModuleId],
    queryFn: () => getModuleWithFields(selectedModuleId!),
    enabled: !!selectedModuleId,
  });

  const createModuleMutation = useMutation({
    mutationFn: createModule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModules'] });
      setNotification({ type: 'success', title: 'Module created successfully' });
      setModuleDialogOpen(false);
      resetModuleForm();
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to create module' });
    },
  });

  const updateModuleMutation = useMutation({
    mutationFn: ({ moduleId, request }: { moduleId: string; request: UpdateModuleRequest }) =>
      updateModule(moduleId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModules'] });
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModule', selectedModuleId] });
      setNotification({ type: 'success', title: 'Module updated successfully' });
      setModuleDialogOpen(false);
      setEditingModule(null);
      resetModuleForm();
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to update module' });
    },
  });

  const changeModuleTypeMutation = useMutation({
    mutationFn: ({ moduleId, request }: { moduleId: string; request: ChangeModuleTypeRequest }) =>
      changeModuleType(moduleId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModules'] });
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to change module type' });
    },
  });

  const deleteModuleMutation = useMutation({
    mutationFn: deleteModule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModules'] });
      setNotification({ type: 'success', title: 'Module deleted successfully' });
      setDeleteModuleDialogOpen(false);
      setModuleToDelete(null);
      if (selectedModuleId === moduleToDelete?.id) {
        setSelectedModuleId(null);
      }
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to delete module' });
    },
  });

  const createSectionMutation = useMutation({
    mutationFn: ({ moduleId, request }: { moduleId: string; request: CreateSectionRequest }) =>
      createSection(moduleId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModule', selectedModuleId] });
      setNotification({ type: 'success', title: 'Section created successfully' });
      setSectionDialogOpen(false);
      resetSectionForm();
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to create section' });
    },
  });

  const updateSectionMutation = useMutation({
    mutationFn: ({ moduleId, sectionId, request }: { moduleId: string; sectionId: string; request: UpdateSectionRequest }) =>
      updateSection(moduleId, sectionId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModule', selectedModuleId] });
      setNotification({ type: 'success', title: 'Section updated successfully' });
      setSectionDialogOpen(false);
      setEditingSection(null);
      resetSectionForm();
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to update section' });
    },
  });

  const deleteSectionMutation = useMutation({
    mutationFn: ({ moduleId, sectionId }: { moduleId: string; sectionId: string }) =>
      deleteSection(moduleId, sectionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataCollectionFormModule', selectedModuleId] });
      setNotification({ type: 'success', title: 'Section deleted successfully' });
      setDeleteSectionDialogOpen(false);
      setSectionToDelete(null);
    },
    onError: (error: any) => {
      setNotification({ type: 'important', title: 'Error', message: error.response?.data || 'Failed to delete section' });
    },
  });

  const resetModuleForm = (type: FormModuleType = TABS[tabIndex].type) => setModuleForm({ code: '', name: '', description: '', type });
  const resetSectionForm = () => setSectionForm({ title: '', description: '', helpText: '', helpUrl: '' });

  const handleTabClick = (e: React.MouseEvent<HTMLAnchorElement>, index: number) => {
    e.preventDefault();
    setTabIndex(index);
    setSelectedModuleId(null);
  };

  const handleOpenCreateModule = () => {
    setEditingModule(null);
    resetModuleForm();
    setModuleDialogOpen(true);
  };

  const handleOpenEditModule = (module: FormModule) => {
    setEditingModule(module);
    setModuleForm({ code: module.code, name: module.name, description: module.description || '', type: module.type });
    setModuleDialogOpen(true);
  };

  const handleOpenCreateSection = () => {
    setEditingSection(null);
    resetSectionForm();
    setSectionDialogOpen(true);
  };

  const handleOpenEditSection = (section: FormSection) => {
    setEditingSection(section);
    setSectionForm({
      title: section.title,
      description: section.description || '',
      helpText: section.helpText || '',
      helpUrl: section.helpUrl || '',
    });
    setSectionDialogOpen(true);
  };

  const handleSaveModule = () => {
    if (editingModule) {
      if (moduleForm.type !== editingModule.type) {
        changeModuleTypeMutation.mutate({ moduleId: editingModule.id, request: { type: moduleForm.type } });
      }
      updateModuleMutation.mutate({
        moduleId: editingModule.id,
        request: {
          name: moduleForm.name,
          description: moduleForm.description || undefined,
          isActive: editingModule.isActive,
        },
      });
    } else {
      createModuleMutation.mutate({
        name: moduleForm.name,
        description: moduleForm.description || undefined,
        type: moduleForm.type,
      });
    }
  };

  const handleSaveSection = () => {
    if (!selectedModuleId) return;
    const section = {
      title: sectionForm.title,
      description: sectionForm.description || undefined,
      helpText: sectionForm.helpText || undefined,
      helpUrl: sectionForm.helpUrl || undefined,
    };
    if (editingSection) {
      updateSectionMutation.mutate({
        moduleId: selectedModuleId,
        sectionId: editingSection.id,
        request: section,
      });
    } else {
      createSectionMutation.mutate({ moduleId: selectedModuleId, request: section });
    }
  };

  const handleDeleteModule = () => {
    if (moduleToDelete) {
      deleteModuleMutation.mutate(moduleToDelete.id);
    }
  };

  const handleDeleteSection = () => {
    if (sectionToDelete && selectedModuleId) {
      deleteSectionMutation.mutate({ moduleId: selectedModuleId, sectionId: sectionToDelete.id });
    }
  };

  const renderModuleList = (type: string) =>
    [...modules]
      .filter((m) => m.type === type)
      .sort((a, b) => a.sectionNumber - b.sectionNumber)
      .map((module) => (
        <Accordion
          key={module.id}
          expanded={selectedModuleId === module.id}
          onChange={() => setSelectedModuleId(selectedModuleId === module.id ? null : module.id)}>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Box sx={{ display: 'flex', alignItems: 'center', flex: 1, gap: 2 }}>
              <Typography variant="h6">{module.name}</Typography>
              <Chip label={module.code} size="small" variant="outlined" />
              {!module.isActive && <Chip label="Inactive" size="small" color="error" />}
            </Box>
            <Box sx={{ display: 'flex', gap: 1 }} onClick={(e: React.MouseEvent) => e.stopPropagation()}>
              <IconButton size="small" onClick={() => handleOpenEditModule(module)} aria-label={`Edit ${module.name} module`}>
                <EditIcon fontSize="small" />
              </IconButton>
              <IconButton
                size="small"
                color="error"
                aria-label={`Delete ${module.name} module`}
                onClick={() => {
                  setModuleToDelete(module);
                  setDeleteModuleDialogOpen(true);
                }}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Box>
          </AccordionSummary>
          <AccordionDetails>
            {module.description && (
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                {module.description}
              </Typography>
            )}

            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                Sections
              </Typography>
              <Button variant="secondary" onClick={handleOpenCreateSection}>
                <AddIcon sx={{ mr: 1 }} /> Add Section
              </Button>
            </Box>

            {selectedModule?.sections.length === 0 ? (
              <Typography color="text.secondary">No sections yet. Add a section to organize questions.</Typography>
            ) : (
              selectedModule?.sections
                .sort((a, b) => a.sectionNumber - b.sectionNumber)
                .map((section) => (
                  <Box
                    key={section.id}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      p: 2,
                      mb: 1,
                      border: '1px solid',
                      borderColor: 'grey.300',
                      borderRadius: 1,
                    }}>
                    <Box sx={{ flex: 1 }}>
                      <Typography variant="subtitle2">
                        {section.sectionNumber}. {section.title}
                      </Typography>
                      {section.description && (
                        <Typography variant="body2" color="text.secondary">
                          {section.description}
                        </Typography>
                      )}
                    </Box>
                    <IconButton size="small" onClick={() => handleOpenEditSection(section)} aria-label={`Edit ${section.title} section`}>
                      <EditIcon fontSize="small" />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="error"
                      aria-label={`Delete ${section.title} section`}
                      onClick={() => {
                        setSectionToDelete(section);
                        setDeleteSectionDialogOpen(true);
                      }}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Box>
                ))
            )}

            <Box sx={{ mt: 2 }}>
              <Button
                variant="secondary"
                onClick={() => {
                  const basePath =
                    module.type === 'delivery-plan'
                      ? ROUTES.admin.configuration.deliveryPlanFormQuestions.list
                      : ROUTES.admin.configuration.dataCollectionFormQuestions.list;
                  navigate(`${basePath}?moduleId=${module.id}`);
                }}>
                Manage Questions
              </Button>
            </Box>
          </AccordionDetails>
        </Accordion>
      ));

  return (
    <SettingsLayout
      breadcrumbs={[
        { label: 'Home', link: ROUTES.admin.home },
        { label: 'Settings', link: ROUTES.admin.settings },
        { label: 'Data Collection Form', link: ROUTES.admin.configuration.dataCollectionFormQuestions.list },
      ]}>
      <PageHeaderContainer>
        <PageTitle>Form Modules</PageTitle>
        <PageHeaderActions>
          <Button onClick={handleOpenCreateModule}>Add Module</Button>
        </PageHeaderActions>
      </PageHeaderContainer>

      <Typography variant="body1" sx={{ mb: 2 }}>
        Manage form modules and their sections. Click on a module to view and manage its sections.
      </Typography>

      <div className="govuk-tabs" data-module="govuk-tabs">
        <h2 className="govuk-tabs__title">Module types</h2>
        <ul className="govuk-tabs__list">
          {TABS.map(({ label, href }, index) => (
            <li key={href} className={`govuk-tabs__list-item${tabIndex === index ? ' govuk-tabs__list-item--selected' : ''}`}>
              <a className="govuk-tabs__tab" href={href} onClick={(e) => handleTabClick(e, index)}>
                {label}
              </a>
            </li>
          ))}
        </ul>
        {TABS.map(({ type, href }, index) => (
          <div key={href} className={`govuk-tabs__panel${tabIndex !== index ? ' govuk-tabs__panel--hidden' : ''}`} id={href.slice(1)}>
            {renderModuleList(type)}
          </div>
        ))}
      </div>

      {/* Module Dialog */}
      <Dialog open={moduleDialogOpen} onClose={() => setModuleDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editingModule ? 'Edit Module' : 'Create Module'}</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            {editingModule && (
              <TextField
                fullWidth
                label="Code"
                value={moduleForm.code}
                disabled
                helperText="System-generated unique identifier — cannot be changed"
                sx={{ mb: 2 }}
              />
            )}
            <FormControl fullWidth sx={{ mb: 2 }}>
              <InputLabel id="module-type-label">Type</InputLabel>
              <Select
                labelId="module-type-label"
                label="Type"
                value={moduleForm.type}
                onChange={(e) => setModuleForm({ ...moduleForm, type: e.target.value as FormModuleType })}>
                {formModuleTypeOptions.map((option) => (
                  <MenuItem key={option.value} value={option.value}>
                    {option.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              fullWidth
              label="Name"
              value={moduleForm.name}
              onChange={(e) => setModuleForm({ ...moduleForm, name: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              fullWidth
              label="Description"
              value={moduleForm.description}
              onChange={(e) => setModuleForm({ ...moduleForm, description: e.target.value })}
              multiline
              rows={3}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button variant="secondary" onClick={() => setModuleDialogOpen(false)}>
            Cancel
          </Button>
          <Button onClick={handleSaveModule} disabled={!moduleForm.name}>
            {editingModule ? 'Save Changes' : 'Create Module'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Section Dialog */}
      <Dialog open={sectionDialogOpen} onClose={() => setSectionDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editingSection ? 'Edit Section' : 'Create Section'}</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 1 }}>
            <TextField
              fullWidth
              label="Title"
              value={sectionForm.title}
              onChange={(e) => setSectionForm({ ...sectionForm, title: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              fullWidth
              label="Description"
              value={sectionForm.description}
              onChange={(e) => setSectionForm({ ...sectionForm, description: e.target.value })}
              multiline
              rows={2}
              sx={{ mb: 2 }}
            />
            <TextField
              fullWidth
              label="Help Text"
              value={sectionForm.helpText}
              onChange={(e) => setSectionForm({ ...sectionForm, helpText: e.target.value })}
              sx={{ mb: 2 }}
            />
            <TextField
              fullWidth
              label="Help URL"
              value={sectionForm.helpUrl}
              onChange={(e) => setSectionForm({ ...sectionForm, helpUrl: e.target.value })}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button variant="secondary" onClick={() => setSectionDialogOpen(false)}>
            Cancel
          </Button>
          <Button onClick={handleSaveSection} disabled={!sectionForm.title}>
            {editingSection ? 'Save Changes' : 'Create Section'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete Module Dialog */}
      <Dialog open={deleteModuleDialogOpen} onClose={() => setDeleteModuleDialogOpen(false)}>
        <DialogTitle>Delete Module</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete the module "{moduleToDelete?.name}"? This action cannot be undone. Note: You cannot delete a module that has questions.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <GovUKButton className="govuk-button govuk-button--secondary" onClick={() => setDeleteModuleDialogOpen(false)}>
            Cancel
          </GovUKButton>
          <GovUKButton onClick={handleDeleteModule} isLoading={deleteModuleMutation.isLoading}>
            Delete
          </GovUKButton>
        </DialogActions>
      </Dialog>

      {/* Delete Section Dialog */}
      <Dialog open={deleteSectionDialogOpen} onClose={() => setDeleteSectionDialogOpen(false)}>
        <DialogTitle>Delete Section</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete the section "{sectionToDelete?.title}"? This action cannot be undone. Note: You cannot delete a section that has questions assigned to it.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <GovUKButton className="govuk-button govuk-button--secondary" onClick={() => setDeleteSectionDialogOpen(false)}>
            Cancel
          </GovUKButton>
          <GovUKButton onClick={handleDeleteSection} isLoading={deleteSectionMutation.isLoading}>
            Delete
          </GovUKButton>
        </DialogActions>
      </Dialog>
    </SettingsLayout>
  );
};

export default DataCollectionFormModules;
