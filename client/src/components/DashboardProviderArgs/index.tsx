import { useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  Dashboard as DashboardIcon,
  HomeWorkOutlined as OrganisationIcon,
  StorageOutlined as CoreDataIcon,
  AssignmentOutlined as SubmissionsIcon,
  HelpOutline as HelpIcon,
} from '@mui/icons-material';
import { ROUTES } from '../../constants/routes';
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
        { label: 'Dashboard', icon: DashboardIcon, to: ROUTES.admin.home },
        { label: 'Local Authorities', icon: OrganisationIcon, to: ROUTES.admin.organisations.list },
      ],
    },
  },
  {
    name: 'local authority admin',
    payload: {
      key: 'org-admin',
      title: 'Local Authority Admin',
      items: [
        { label: 'Dashboard', icon: DashboardIcon, to: ROUTES.organisationAdmin.coreData },
      ],
    },
  },
  {
    name: 'delivery plan lead',
    payload: {
      key: 'delivery-plan',
      title: 'Delivery Plan Lead',
      items: [
        { label: 'Core Data', icon: CoreDataIcon, to: ROUTES.deliveryPlan.coreData },
        { label: 'Submissions', icon: SubmissionsIcon, to: ROUTES.deliveryPlan.submissions.list },
        { label: 'Help', icon: HelpIcon, to: ROUTES.deliveryPlan.help },
      ],
    },
  },
];
