import React, { useState } from 'react';
import { ViewToggleType } from '../../../../components/GovUKComponents';
import { Search as SearchIcon, Clear as ClearSearchIcon } from '@mui/icons-material/';
import { Grid, GridSize, InputBase, Typography } from '@mui/material';
import useLocalStorage from '../../../../hooks/useLocalStorage';
import { SearchContainer, SearchPaper, SearchIconContainer } from './styled';
import TableViewComponent from './table';
import GridViewComponent from './grid';
import { ListItem } from '..';

interface Props {
  items: ListItem[];
  gridSize?: GridSize;
  tableTitle: string;
  viewModeKey: string;
  handleCreate: () => void;
  handleEdit: (id: string) => void;
  handleView: (id: string) => void;
}

const ListViewComponent = ({ items, gridSize = 4, tableTitle, viewModeKey, handleCreate, handleEdit, handleView }: Props): React.ReactElement => {
  const [searchValue, setSearchValue] = useState<string>('');
  const [activeFiltered, setActiveFiltered] = useState<boolean>(true);
  const [viewMode] = useLocalStorage<ViewToggleType>(viewModeKey, ViewToggleType.TABLE);
  const title = tableTitle;

  return (
    <>
      <Grid container alignItems="center" style={{ marginBottom: '5px' }}>
        <Grid item xs={2}>
          {viewMode === ViewToggleType.GRID && (
            <Typography variant="h5" component="h2">
              {items.length} {title}
            </Typography>
          )}
        </Grid>
        <Grid item xs={10}>
          <Grid container spacing={2} justifyContent="right" alignItems="center">
            {viewMode === ViewToggleType.GRID && (
              <Grid item>
                <SearchContainer>
                  <SearchPaper variant="outlined">
                    <InputBase
                      sx={{ width: '100%' }}
                      placeholder="Search"
                      value={searchValue}
                      onChange={(event: React.ChangeEvent<HTMLInputElement>) => setSearchValue(event.target.value || '')}
                      startAdornment={
                        <SearchIconContainer>
                          <SearchIcon />
                        </SearchIconContainer>
                      }
                      endAdornment={
                        <SearchIconContainer>
                          <ClearSearchIcon onClick={() => setSearchValue('')} />
                        </SearchIconContainer>
                      }
                      inputProps={{ 'aria-label': 'search list' }}
                    />
                  </SearchPaper>
                </SearchContainer>
              </Grid>
            )}
            <Grid item>
              {/* View toggle placeholder */}
            </Grid>
          </Grid>
        </Grid>
      </Grid>
      <Grid container spacing={2}>
        {viewMode === ViewToggleType.TABLE ? (
          <Grid item xs={12}>
            <TableViewComponent
              items={items}
              tableTitle={title}
              activeFiltered={activeFiltered}
              setActiveFiltered={setActiveFiltered}
              handleCreate={handleCreate}
              handleEdit={handleEdit}
              handleView={handleView}
            />
          </Grid>
        ) : (
          <GridViewComponent
            items={items}
            gridSize={gridSize}
            searchValue={searchValue}
            activeFiltered={activeFiltered}
            handleCreate={handleCreate}
            handleEdit={handleEdit}
            handleView={handleView}
          />
        )}
      </Grid>
    </>
  );
};

export default ListViewComponent;
