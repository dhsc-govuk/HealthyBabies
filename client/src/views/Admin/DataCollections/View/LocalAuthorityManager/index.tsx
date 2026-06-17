import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { Button, LoadingBox, Link } from 'govuk-react';
import { Dialog, DialogTitle, DialogContent, DialogActions } from '@mui/material';
import { GovUKTable, DeleteConfirmDialog, GovUKButton, GovUKDateField, useGovUKNotification } from '../../../../../components/GovUKComponents';
import type { Column } from '../../../../../components/GovUKComponents';
import { getOrganisations, Organisation } from '../../../../Admin/Organisations/List/queries';
import {
  LocalAuthorityAssignment,
  updateDataCollectionLocalAuthorities,
  removeLocalAuthorityFromDataCollection,
  updateLocalAuthorityEndDate,
} from '../../List/queries';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import './styles.css';

interface LocalAuthorityManagerProps {
  dataCollectionId: string;
  assignedLocalAuthorities: LocalAuthorityAssignment[];
  collectionStartDate: string;
  collectionEndDate: string;
  onUpdate: () => void;
}

interface EditEndDateModal {
  open: boolean;
  laId: string;
  laName: string;
  value: string;
  error?: string;
}

function LocalAuthorityManager({
  dataCollectionId,
  assignedLocalAuthorities,
  collectionStartDate,
  collectionEndDate,
  onUpdate,
}: LocalAuthorityManagerProps): React.ReactElement {
  const navigate = useNavigate();
  const { setNotification } = useGovUKNotification();
  const [isAddMode, setIsAddMode] = useState(false);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [editEndDateModal, setEditEndDateModal] = useState<EditEndDateModal>({
    open: false,
    laId: '',
    laName: '',
    value: '',
    error: undefined,
  });
  const [deleteConfirm, setDeleteConfirm] = useState<{ open: boolean; laId: string; laName: string }>({
    open: false,
    laId: '',
    laName: '',
  });
  const queryClient = useQueryClient();

  const { data: organisationsData, isLoading: loadingOrganisations } = useQuery({
    queryKey: ['organisations-for-assignment'],
    queryFn: getOrganisations,
    enabled: isAddMode,
  });

  const assignedIds = useMemo(
    () => new Set(assignedLocalAuthorities.map((la) => la.id)),
    [assignedLocalAuthorities]
  );

  const availableOrganisations = useMemo(() => {
    if (!organisationsData?.data) return [];
    return organisationsData.data.filter((org) => org.isActive && !assignedIds.has(org.id));
  }, [organisationsData, assignedIds]);

  const { mutateAsync: updateMutation, isLoading: updating } = useMutation({
    mutationFn: (laIds: string[]) => updateDataCollectionLocalAuthorities(dataCollectionId, laIds),
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Local authorities updated successfully' });
      queryClient.invalidateQueries(['data-collection-las', dataCollectionId]);
      onUpdate();
      setIsAddMode(false);
      setSelectedIds([]);
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const { mutateAsync: removeMutation, isLoading: removing } = useMutation({
    mutationFn: (laId: string) => removeLocalAuthorityFromDataCollection(dataCollectionId, laId),
    onSuccess: () => {
      setNotification({ type: 'success', title: 'Local authority removed successfully' });
      queryClient.invalidateQueries(['data-collection-las', dataCollectionId]);
      onUpdate();
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const { mutateAsync: updateEndDateMutation, isLoading: updatingEndDate } = useMutation({
    mutationFn: ({ laId, endDate }: { laId: string; endDate: string | null }) =>
      updateLocalAuthorityEndDate(dataCollectionId, laId, endDate),
    onSuccess: () => {
      setNotification({ type: 'success', title: 'End date updated successfully' });
      queryClient.invalidateQueries(['data-collection-las', dataCollectionId]);
      onUpdate();
      handleCancelEdit();
    },
    onError: (error) => {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  const handleAddSelected = async () => {
    const allIds = [...assignedLocalAuthorities.map((la) => la.id), ...selectedIds];
    await updateMutation(allIds);
  };

  const handleRemoveClick = (laId: string, laName: string) => {
    setDeleteConfirm({ open: true, laId, laName });
  };

  const handleConfirmRemove = async () => {
    await removeMutation(deleteConfirm.laId);
    setDeleteConfirm({ open: false, laId: '', laName: '' });
  };

  const handleCancelRemove = () => {
    setDeleteConfirm({ open: false, laId: '', laName: '' });
  };

  const handleToggleSelect = (orgId: string) => {
    setSelectedIds((prev) =>
      prev.includes(orgId) ? prev.filter((id) => id !== orgId) : [...prev, orgId]
    );
  };

  const handleSelectAll = () => {
    if (selectedIds.length === availableOrganisations.length) {
      setSelectedIds([]);
    } else {
      setSelectedIds(availableOrganisations.map((org) => org.id));
    }
  };

  const isAllSelected = availableOrganisations.length > 0 && selectedIds.length === availableOrganisations.length;
  const isIndeterminate = selectedIds.length > 0 && selectedIds.length < availableOrganisations.length;

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  };

  const validateEndDate = (dateString: string): string | undefined => {
    if (!dateString) {
      return 'End date is required';
    }

    // Parse the date from DD/MM/YYYY format
    const parts = dateString.split('/');
    if (parts.length !== 3) {
      return 'Enter a valid date';
    }

    const day = parseInt(parts[0], 10);
    const month = parseInt(parts[1], 10);
    const year = parseInt(parts[2], 10);

    if (isNaN(day) || isNaN(month) || isNaN(year)) {
      return 'Enter a valid date';
    }

    const selectedDate = new Date(year, month - 1, day);
    const startDate = new Date(collectionStartDate);

    // Reset time parts for accurate date comparison
    selectedDate.setHours(0, 0, 0, 0);
    startDate.setHours(0, 0, 0, 0);

    if (selectedDate < startDate) {
      return `End date must be on or after the collection start date (${formatDate(collectionStartDate)})`;
    }

    return undefined;
  };

  const handleEditEndDate = (laId: string, laName: string, currentEndDate: string | null) => {
    // Convert ISO date to DD/MM/YYYY format for GovUKDateField
    let formattedValue = '';
    if (currentEndDate) {
      const date = new Date(currentEndDate);
      const day = String(date.getDate()).padStart(2, '0');
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const year = date.getFullYear();
      formattedValue = `${day}/${month}/${year}`;
    }
    setEditEndDateModal({
      open: true,
      laId,
      laName,
      value: formattedValue,
      error: undefined,
    });
  };

  const handleDateChange = (dateString: string) => {
    const error = dateString ? validateEndDate(dateString) : undefined;
    setEditEndDateModal({
      ...editEndDateModal,
      value: dateString,
      error,
    });
  };

  const handleSaveEndDate = async () => {
    // Validate the date value exists
    if (!editEndDateModal.value || editEndDateModal.value.trim() === '') {
      setEditEndDateModal({
        ...editEndDateModal,
        error: 'Please enter a valid date',
      });
      return;
    }

    // Validate against collection date range
    const error = validateEndDate(editEndDateModal.value);
    if (error) {
      setEditEndDateModal({
        ...editEndDateModal,
        error,
      });
      return;
    }

    // Convert DD/MM/YYYY to ISO format for API
    // GovUKDateField ensures the format is always DD/MM/YYYY
    const parts = editEndDateModal.value.split('/');
    if (parts.length !== 3) {
      setEditEndDateModal({
        ...editEndDateModal,
        error: 'Please enter a complete date',
      });
      return;
    }

    const isoDate = `${parts[2]}-${parts[1]}-${parts[0]}`;
    
    try {
      await updateEndDateMutation({ laId: editEndDateModal.laId, endDate: isoDate });
    } catch (err) {
      setEditEndDateModal({
        ...editEndDateModal,
        error: 'Failed to update end date. Please try again.',
      });
    }
  };

  const handleCancelEdit = () => {
    setEditEndDateModal({
      open: false,
      laId: '',
      laName: '',
      value: '',
      error: undefined,
    });
  };

  const assignedColumns: Column<LocalAuthorityAssignment>[] = [
    {
      key: 'name',
      header: 'Local authority',
      render: (la) => la.name,
    },
    {
      key: 'endDate',
      header: 'Extended due date',
      render: (la) => {
        if (!la.endDate) {
          return <span>Not set</span>;
        }
        return <span>{formatDate(la.endDate)}</span>;
      },
    },
    {
      key: 'actions',
      header: '',
      align: 'right',
      render: (la) => (
        <div className="la-manager__actions-cell">
          <Link
            href={`/admin/organisations/${la.id}`}
            onClick={(e: React.MouseEvent) => {
              e.preventDefault();
              navigate(`/admin/organisations/${la.id}`);
            }}
          >
            View<span className="govuk-visually-hidden"> {la.name}</span>
          </Link>
          <Link
            href="#"
            onClick={(e: React.MouseEvent) => {
              e.preventDefault();
              handleEditEndDate(la.id, la.name, la.endDate);
            }}
          >
            Change<span className="govuk-visually-hidden"> {la.name} end date</span>
          </Link>
          <Link
            href="#"
            onClick={(e: React.MouseEvent) => {
              e.preventDefault();
              if (!removing) handleRemoveClick(la.id, la.name);
            }}
          >
            Delete<span className="govuk-visually-hidden"> {la.name}</span>
          </Link>
        </div>
      ),
    },
  ];

  const availableColumns: Column<Organisation>[] = [
    {
      key: 'select',
      header: (
        <input
          type="checkbox"
          checked={isAllSelected}
          ref={(el) => {
            if (el) el.indeterminate = isIndeterminate;
          }}
          onChange={handleSelectAll}
          aria-label="Select all local authorities"
        />
      ) as unknown as string,
      render: (org) => (
        <input
          type="checkbox"
          checked={selectedIds.includes(org.id)}
          onChange={() => handleToggleSelect(org.id)}
          aria-label={`Select ${org.name}`}
        />
      ),
    },
    {
      key: 'name',
      header: 'Local Authority',
      render: (org) => org.name,
    },
    {
      key: 'onsCode',
      header: 'ONS Code',
      render: (org) => org.onsCode || '-',
    },
  ];

  if (isAddMode) {
    return (
      <div className="la-manager">
        <div className="la-manager__header">
          <h2 className="govuk-heading-m">Add local authorities</h2>
          <Button
            buttonColour="#f3f2f1"
            buttonTextColour="#0b0c0c"
            onClick={() => {
              setIsAddMode(false);
              setSelectedIds([]);
            }}
          >
            Cancel
          </Button>
        </div>

        <LoadingBox loading={loadingOrganisations}>
          {availableOrganisations.length === 0 ? (
            <p className="govuk-body">All active local authorities have already been assigned.</p>
          ) : (
            <>
              <p className="govuk-body">
                Select the local authorities to include in this data collection.
              </p>
              <GovUKTable<Organisation>
                data={availableOrganisations}
                columns={availableColumns}
                searchPlaceholder="Search local authorities"
                searchable={true}
                keyExtractor={(org) => org.id}
                emptyMessage="No local authorities available"
              />
              <div className="la-manager__actions">
                <Button onClick={handleAddSelected} disabled={selectedIds.length === 0 || updating}>
                  {updating ? 'Adding...' : `Add ${selectedIds.length} selected`}
                </Button>
              </div>
            </>
          )}
        </LoadingBox>
      </div>
    );
  }

  return (
    <div className="la-manager">
      <div className="la-manager__header">
        <h2 className="govuk-heading-m">Local authority due date extensions</h2>
        <Button
          buttonColour="#f3f2f1"
          buttonTextColour="#0b0c0c"
          onClick={() => setIsAddMode(true)}
        >
          Add extension
        </Button>
      </div>

      {assignedLocalAuthorities.length === 0 ? (
        <p className="govuk-body">No due date extensions have been added.</p>
      ) : (
        <GovUKTable<LocalAuthorityAssignment>
          data={assignedLocalAuthorities}
          columns={assignedColumns}
          searchPlaceholder="Search local authority by name"
          searchable={true}
          sortOptions={[
            { value: 'date-asc', label: 'By due date' },
            { value: 'name-asc', label: 'Alphabetically A-Z' },
          ]}
          sortLabel="Sort list"
          keyExtractor={(la) => la.id}
          emptyMessage="No due date extensions found"
        />
      )}

      <DeleteConfirmDialog
        open={deleteConfirm.open}
        title="Remove local authority"
        message="Are you sure you want to remove this local authority from the data collection?"
        itemName={deleteConfirm.laName}
        onConfirm={handleConfirmRemove}
        onCancel={handleCancelRemove}
        isLoading={removing}
      />

      <Dialog open={editEndDateModal.open} onClose={handleCancelEdit} maxWidth="sm" fullWidth>
        <DialogTitle className="govuk-heading-m" style={{ marginBottom: 0 }}>
          Edit end date for {editEndDateModal.laName}
        </DialogTitle>
        <DialogContent style={{ paddingTop: '20px' }}>
          <GovUKDateField
            id="edit-end-date"
            legend="End date"
            legendSize="s"
            value={editEndDateModal.value}
            error={editEndDateModal.error}
            hint={`Must be on or after ${formatDate(collectionStartDate)}`}
            disabled={updatingEndDate}
            onChange={handleDateChange}
          />
        </DialogContent>
        <DialogActions style={{ padding: '20px' }}>
          <GovUKButton
            className="govuk-button govuk-button--secondary"
            onClick={handleCancelEdit}
            disabled={updatingEndDate}
          >
            Cancel
          </GovUKButton>
          <GovUKButton
            onClick={handleSaveEndDate}
            disabled={updatingEndDate || !!editEndDateModal.error}
          >
            {updatingEndDate ? 'Saving...' : 'Save'}
          </GovUKButton>
        </DialogActions>
      </Dialog>
    </div>
  );
}

export default LocalAuthorityManager;