import { DashboardCard, ActionCard } from '../../../../components/GovUKComponents';
import { ListItem } from '..';

interface Props {
  items: ListItem[];
  gridSize?: number | boolean | 'auto';
  searchValue: string;
  activeFiltered: boolean;
  handleCreate: () => void;
  handleEdit: (id: string) => void;
  handleView: (id: string) => void;
}

const GridViewComponent = ({ items, gridSize, searchValue, activeFiltered, handleCreate, handleEdit, handleView }: Props) => {
  return (
    <>
      <ActionCard gridSize={gridSize} fill={true} onClick={handleCreate} />
      {items
        .filter((i) => i.fullName.toLowerCase().includes(searchValue.toLowerCase()))
        .filter((i) => i.active === activeFiltered)
        .map((user) => (
          <DashboardCard
            fill={true}
            key={user.id}
            gridSize={gridSize}
            title={user.fullName}
            description={user.email}
            handleEdit={() => handleEdit(user.id)}
            handleView={() => handleView(user.id)}
          />
        ))}
    </>
  );
};

export default GridViewComponent;
