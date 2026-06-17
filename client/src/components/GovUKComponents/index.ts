// Re-export govuk-react components for convenience
export {
  Button,
  BackLink,
  Breadcrumbs,
  Checkbox,
  DateField,
  Details,
  ErrorSummary,
  Fieldset,
  FormGroup,
  GridCol,
  GridRow,
  Heading,
  HintText,
  Input,
  InputField,
  InsetText,
  Label,
  LabelText,
  Link,
  LoadingBox,
  MultiChoice,
  Pagination,
  Panel,
  PhaseBanner,
  Radio,
  SearchBox,
  Select,
  Table,
  Tabs,
  Tag,
  TextArea,
  WarningText,
  GlobalStyle,
} from 'govuk-react';

// Typography components — native govuk-frontend classes, GDS Transport font (replaces govuk-react H1–H4 etc. which inject the legacy "nta" font)
export { H1, H2, H3, H4, Paragraph, LeadParagraph, UnorderedList, OrderedList, ListItem } from './GovUKTypography';

// Custom components (not available in govuk-react or with extended functionality)
export { default as LoadingSpinner } from './LoadingSpinner';
export { default as GovUKStepper } from './GovUKStepper';
export { default as GovUKFieldset } from './GovUKFieldset';
export type { RadioOption, CheckboxOption } from './GovUKFieldset';
export { default as GovUKRadio } from './GovUKRadio';
export { default as GovUKCheckbox } from './GovUKCheckbox';
export { default as GovUKDateField } from './GovUKDateField';
export { default as GovUKBreadcrumbs } from './GovUKBreadcrumbs';
export { default as GovUKNotificationBanner } from './GovUKNotificationBanner';
export { default as GovUKBackLink } from './GovUKBackLink';
export { default as GovUKButton } from './GovUKButton';
export { default as DashboardCard } from './DashboardCard';
export { default as ActionCard } from './ActionCard';
export { default as ProtectedRoleRoute } from './ProtectedRoleRoute';
export { default as NotAuthorised } from './NotAuthorised';
export { default as NotFound } from './NotFound';
export { default as DashboardProvider } from './DashboardProvider';
export { default as DeleteConfirmDialog } from './DeleteConfirmDialog';
export { default as SummaryList } from './SummaryList';
export { default as GovUKSummaryCard } from './GovUKSummaryCard';
export type { SummaryCardAction } from './GovUKSummaryCard';
export { default as GovUKTable, NameLink, StatusCell } from './GovUKTable';
export type { Column, GovUKTableProps, FilterOption } from './GovUKTable';
export { CustomThemeProvider } from './ThemeProvider';
export type { ThemeModes } from './ThemeProvider';
export { ViewToggleType } from './types';

// GOV.UK Design System compliant components using official HTML/classes
export { default as GovUKTag } from './GovUKTag';
export type { TagColour } from './GovUKTag';
export { default as GovUKHeader } from './GovUKHeader';
export { default as GovUKFooter } from './GovUKFooter';
export { default as GovUKInsetText } from './GovUKInsetText';
export { default as GovUKPanel } from './GovUKPanel';
export { default as GovUKWarningText } from './GovUKWarningText';
export { default as GovUKDetails } from './GovUKDetails';
export { default as GovUKServiceNavigation } from './GovUKServiceNavigation';
export { default as GovUKTaskList } from './GovUKTaskList';
export type { TaskListItem, TaskListItemStatus } from './GovUKTaskList';
export { default as GovUKAutocomplete } from './GovUKAutocomplete';
export type { AutocompleteOption, GovUKAutocompleteProps } from './GovUKAutocomplete';
export { default as GovUKActionMenu } from './GovUKActionMenu';
export type { ActionMenuItem, GovUKActionMenuProps } from './GovUKActionMenu';
export { default as GovUKSideNavigation } from './GovUKSideNavigation';
export type { SideNavigationItem, SideNavigationSection } from './GovUKSideNavigation';
export { default as GovUKFileUpload } from './GovUKFileUpload';
export { default as GovUKInputWithSuffix } from './GovUKInputWithSuffix';
export { default as GovUKDynamicQuestionRenderer } from './GovUKDynamicQuestionRenderer';
export type {
  FormQuestion,
  FormQuestionOption,
  FormState,
  ValidationErrors as DynamicQuestionValidationErrors,
  GovUKDynamicQuestionRendererProps,
} from './GovUKDynamicQuestionRenderer';
export { default as FormFieldRenderer } from './FormFieldRenderer';
export { default as GovUKGroupedInputs } from './GovUKGroupedInputs';
export { default as FormAnswersView } from './FormAnswersView';
export type { FormAnswerItem, FormAnswersViewProps } from './FormAnswersView';
export { GovUKNotificationProvider, useGovUKNotification, GovUKNotificationDisplay } from './GovUKNotificationProvider';
export type { GovUKNotification } from './GovUKNotificationProvider';
export { default as GovUKSkipLink } from './GovUKSkipLink';
