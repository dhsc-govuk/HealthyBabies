import React from 'react';
import { booleanToYesNo } from '../../../../helpers/stringUtils';
import { ToggleButton, ToggleButtonGroup } from '@mui/material';
import { ListItem } from '..';
import { StyledTable, StyledTh, StyledTd, ActionButton } from './styled';

interface Props {
  items: ListItem[];
  tableTitle: string;
  activeFiltered: boolean;
  setActiveFiltered: (value: boolean) => void;
  handleCreate: () => void;
  handleEdit: (id: string) => void;
  handleView: (id: string) => void;
}

const TableViewComponent = ({ items, activeFiltered, handleView, handleEdit, setActiveFiltered }: Props): React.ReactElement => {
  const rows = items.map((item) => {
    return {
      key: item.id,
      actions: [
        {
          label: 'View',
          onClick: () => handleView(item.id),
        },
        {
          label: 'Edit',
          onClick: () => handleEdit(item.id),
        },
      ],
      columns: [
        {
          key: 'full_name',
          label: item.fullName,
        },
        {
          key: 'email',
          label: item.email,
        },
        {
          key: 'active',
          label: booleanToYesNo(item.active),
        },
      ],
    };
  });

  const activeFilter: any = {
    key: 'activeFilter',
    label: 'Active',
    filter: (rows: any[]) =>
      rows.filter((r) => {
        const col = r.columns.find((c: { key: string; label: string }) => c.key === 'active');
        if (col) {
          return activeFiltered ? col.label === 'Yes' : col.label === 'No';
        }
        return true;
      }),
    component: (
      <ToggleButtonGroup size="medium" value={[activeFiltered]} color="primary" onChange={() => setActiveFiltered(!activeFiltered)}>
        <ToggleButton key="active" value={true}>
          Active
        </ToggleButton>

        <ToggleButton key="inactive" value={false}>
          Inactive
        </ToggleButton>
      </ToggleButtonGroup>
    ),
  };

  const filters = [activeFilter];

  const filterRows = (): any[] => {
    let filteredRows: any[] = rows;
    filters.forEach((f) => {
      filteredRows = f.filter(filteredRows);
    });
    return filteredRows;
  };

  return (
    <StyledTable>
      <thead>
        <tr>
          <StyledTh>Full name</StyledTh>
          <StyledTh>Email</StyledTh>
          <StyledTh>Active</StyledTh>
          <StyledTh>Actions</StyledTh>
        </tr>
      </thead>
      <tbody>
        {filterRows().map((row) => (
          <tr key={row.key}>
            {row.columns.map((col: { key: string; label: string }) => (
              <StyledTd key={col.key}>{col.label}</StyledTd>
            ))}
            <StyledTd>
              {row.actions.map((action: { label: string; onClick: () => void }) => (
                <ActionButton key={action.label} onClick={action.onClick}>
                  {action.label}
                </ActionButton>
              ))}
            </StyledTd>
          </tr>
        ))}
      </tbody>
    </StyledTable>
  );
};

export default TableViewComponent;
