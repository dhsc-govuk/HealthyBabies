import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Table, Link, Tag, Select } from 'govuk-react';
import { H2 } from '../GovUKTypography';
import { PageHeaderContainer, PageHeaderActions } from '../../../styles/govuk-global';
import SearchIcon from '../../Logos/SearchIcon';
import './styles.css';

export interface Column<T> {
  key: keyof T | string;
  header: string;
  render?: (item: T) => React.ReactNode;
  sortable?: boolean;
  align?: 'left' | 'right' | 'center';
}

export interface FilterOption {
  value: string;
  label: string;
}

export interface GovUKTableProps<T> {
  data: T[];
  columns: Column<T>[];
  title?: string;
  searchPlaceholder?: string;
  searchable?: boolean;
  sortOptions?: FilterOption[];
  sortLabel?: string;
  onSort?: (value: string) => void;
  filterOptions?: FilterOption[];
  filterLabel?: string;
  filterPlaceholder?: string;
  onFilter?: (value: string) => void;
  filterField?: keyof T | string;
  addButtonLabel?: string;
  addButtonHref?: string;
  onRowClick?: (item: T) => void;
  getRowHref?: (item: T) => string;
  keyExtractor: (item: T) => string;
  emptyMessage?: string;
  hideHeader?: boolean;
}

function GovUKTableComponent<T>({
  data,
  columns,
  title,
  searchPlaceholder = 'Search by name',
  searchable = true,
  sortOptions,
  sortLabel = 'Sort list',
  onSort,
  filterOptions,
  filterLabel = 'Filter by status',
  filterPlaceholder = 'Select status...',
  onFilter,
  filterField,
  addButtonLabel,
  addButtonHref,
  onRowClick,
  getRowHref,
  keyExtractor,
  emptyMessage = 'No results found',
  hideHeader = false,
}: GovUKTableProps<T>): React.ReactElement {
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState('');
  const [sortValue, setSortValue] = useState(sortOptions?.[0]?.value || '');
  const [filterValue, setFilterValue] = useState('');

  const filteredAndSortedData = useMemo(() => {
    let result = data;

    // Apply search filter
    if (searchTerm) {
      const lowerSearch = searchTerm.toLowerCase();
      result = result.filter((item) => {
        const record = item as Record<string, unknown>;
        return Object.values(record).some((value) => {
          if (typeof value === 'string') {
            return value.toLowerCase().includes(lowerSearch);
          }
          return false;
        });
      });
    }

    // Apply status/field filter
    if (filterValue && filterField) {
      result = result.filter((item) => {
        const record = item as Record<string, unknown>;
        const fieldValue = record[filterField as string];
        return fieldValue === filterValue;
      });
    }

    // Apply sorting
    if (sortValue) {
      const [field, direction] = sortValue.split('-');
      result = [...result].sort((a, b) => {
        const recordA = a as Record<string, unknown>;
        const recordB = b as Record<string, unknown>;

        let valueA: unknown;
        let valueB: unknown;

        // Handle common sort fields
        if (field === 'name') {
          // Try common name patterns
          valueA = recordA.name ?? `${recordA.firstName ?? ''} ${recordA.lastName ?? ''}`.trim();
          valueB = recordB.name ?? `${recordB.firstName ?? ''} ${recordB.lastName ?? ''}`.trim();
        } else if (field === 'status') {
          valueA = recordA.isActive;
          valueB = recordB.isActive;
        } else if (field === 'date') {
          valueA = recordA.startDate ?? recordA.createdAt ?? recordA.date;
          valueB = recordB.startDate ?? recordB.createdAt ?? recordB.date;
        } else {
          valueA = recordA[field];
          valueB = recordB[field];
        }

        // Compare values
        if (typeof valueA === 'string' && typeof valueB === 'string') {
          const comparison = valueA.toLowerCase().localeCompare(valueB.toLowerCase());
          return direction === 'desc' ? -comparison : comparison;
        }

        if (typeof valueA === 'boolean' && typeof valueB === 'boolean') {
          // Active items first for 'asc', inactive first for 'desc'
          if (valueA === valueB) return 0;
          if (direction === 'asc') return valueA ? -1 : 1;
          return valueA ? 1 : -1;
        }

        if (
          valueA instanceof Date ||
          valueB instanceof Date ||
          (typeof valueA === 'string' && typeof valueB === 'string' && !isNaN(Date.parse(valueA)) && !isNaN(Date.parse(valueB)))
        ) {
          const dateA = new Date(valueA as string).getTime();
          const dateB = new Date(valueB as string).getTime();
          return direction === 'desc' ? dateB - dateA : dateA - dateB;
        }

        return 0;
      });
    }

    return result;
  }, [data, searchTerm, sortValue, filterValue, filterField]);

  const handleSortChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setSortValue(value);
    onSort?.(value);
  };

  const handleFilterChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setFilterValue(value);
    onFilter?.(value);
  };

  const handleRowClick = (item: T) => {
    if (getRowHref) {
      navigate(getRowHref(item));
    } else if (onRowClick) {
      onRowClick(item);
    }
  };

  const renderCellValue = (item: T, column: Column<T>): React.ReactNode => {
    if (column.render) {
      return column.render(item);
    }
    const value = (item as Record<string, unknown>)[column.key as string];
    return value as React.ReactNode;
  };

  return (
    <div>
      {(title || addButtonLabel) && (
        <PageHeaderContainer>
          {title && <H2>{title}</H2>}
          {addButtonLabel && addButtonHref && (
            <PageHeaderActions>
              <Button onClick={() => navigate(addButtonHref)}>{addButtonLabel}</Button>
            </PageHeaderActions>
          )}
        </PageHeaderContainer>
      )}

      {(searchable || sortOptions || filterOptions) &&
        (() => {
          const hasSort = sortOptions && sortOptions.length > 0;
          const hasFilter = filterOptions && filterOptions.length > 0;
          const hasBothDropdowns = hasSort && hasFilter;
          const searchColumnClass = hasBothDropdowns ? 'govuk-grid-column-one-third' : 'govuk-grid-column-two-thirds';
          const dropdownColumnClass = 'govuk-grid-column-one-third';

          return (
            <div className="govuk-grid-row govuk-table-filter">
              {searchable && (
                <div className={searchColumnClass}>
                  <label htmlFor="table-search-input" className="govuk-label">
                    {searchPlaceholder}
                  </label>
                  <div className="govuk-table-filter__search">
                    <input
                      id="table-search-input"
                      type="search"
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="govuk-input govuk-table-filter__input"
                    />
                    <button type="button" className="govuk-table-filter__search-button" aria-label="Search" onClick={() => {}}>
                      <SearchIcon />
                    </button>
                  </div>
                </div>
              )}
              {hasSort && (
                <div className={`${dropdownColumnClass} govuk-table-filter__sort`}>
                  <label htmlFor="table-sort-select" className="govuk-label">
                    {sortLabel}
                  </label>
                  <select id="table-sort-select" className="govuk-select" value={sortValue} onChange={handleSortChange}>
                    {sortOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>
              )}
              {hasFilter && (
                <div className={`${dropdownColumnClass} govuk-table-filter__filter`}>
                  <Select
                    label={filterLabel}
                    input={{
                      value: filterValue,
                      onChange: handleFilterChange,
                    }}>
                    <option value="">{filterPlaceholder}</option>
                    {filterOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                </div>
              )}
            </div>
          );
        })()}

      <Table>
        {!hideHeader && (
          <Table.Row>
            {columns.map((column) => (
              <Table.CellHeader key={String(column.key)} style={column.align ? { textAlign: column.align } : undefined}>
                {column.header}
              </Table.CellHeader>
            ))}
          </Table.Row>
        )}
        {filteredAndSortedData.length === 0 ? (
          <Table.Row>
            <Table.Cell colSpan={columns.length}>{emptyMessage}</Table.Cell>
          </Table.Row>
        ) : (
          filteredAndSortedData.map((item) => (
            <Table.Row key={keyExtractor(item)} onClick={() => handleRowClick(item)} className={getRowHref || onRowClick ? 'govuk-table-row--clickable' : ''}>
              {columns.map((column) => (
                <Table.Cell key={String(column.key)} style={column.align ? { textAlign: column.align } : undefined}>
                  {renderCellValue(item, column)}
                </Table.Cell>
              ))}
            </Table.Row>
          ))
        )}
      </Table>
    </div>
  );
}

// Helper components for common cell renderers
export const NameLink = ({ name, href, onClick }: { name: string; href?: string; onClick?: () => void }): React.ReactElement => (
  <Link
    href={href}
    onClick={(e: React.MouseEvent) => {
      if (onClick) {
        e.preventDefault();
        onClick();
      }
    }}>
    {name}
  </Link>
);

export const StatusCell = ({ isActive }: { isActive: boolean }): React.ReactElement => <Tag tint={isActive ? 'GREEN' : 'YELLOW'}>{isActive ? 'Active' : 'Inactive'}</Tag>;

export default GovUKTableComponent;
