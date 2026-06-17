import { useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  Dashboard as DashboardIcon,
  HomeWorkOutlined as OrganisationIcon,
} from '@mui/icons-material';
export function withRouter(Component: any) {
  function ComponentWithRouterProp(props: any) {
    let location = useLocation();
    let navigate = useNavigate();
    let params = useParams();
    return <Component {...props} history={{ location, navigate, params }} />;
  }

  return ComponentWithRouterProp;
}

export const listItems = [
  {
    name: 'admin',
    payload: {
      key: 'admin',
      title: 'Admin',
      items: [
        { label: 'Dashboard', icon: DashboardIcon, to: '/admin/home' },
        { label: 'Local Authorities', icon: OrganisationIcon, to: '/admin/organisations' },
      ],
    },
  },
  {
    name: 'local authority admin',
    payload: {
      key: 'org-admin',
      title: 'Local Authority Admin',
      items: [
        { label: 'Dashboard', icon: DashboardIcon, to: '/organisation-admin/core-data' },
      ],
    },
  },
];
