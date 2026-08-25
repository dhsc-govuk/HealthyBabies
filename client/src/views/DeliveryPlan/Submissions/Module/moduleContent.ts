export interface ModuleContent {
  activityNoun: string;
  activityNounPlural: string;
  listHeading: string;
  addButtonText: string;
  helpLinkText: string;
  uploadLabel: string;
  downloadLabel: string;
  emptyText: string;
  instructionText: string;
  csvNoun: string;
  formIntroText: string;
}

export const MODULE_CONTENT: Record<string, ModuleContent> = {
  'delivery-grant': {
    activityNoun: 'activity',
    activityNounPlural: 'activities',
    listHeading: 'Activities',
    addButtonText: 'Add activity',
    helpLinkText: 'Help with adding or updating activities',
    uploadLabel: 'Upload activities with CSV',
    downloadLabel: 'Download activities as CSV',
    emptyText: "No activities added yet. Click 'Add activity' to get started.",
    instructionText: 'You can submit activity details in one of two ways:',
    csvNoun: 'activities',
    formIntroText: 'Tell us about the Transformation Grant activity you plan to carry out in the next year. This information will be included in your annual Delivery Plan submission.',
  },
  'parenting': {
    activityNoun: 'activity',
    activityNounPlural: 'activities',
    listHeading: 'Parenting activities',
    addButtonText: 'Add activity',
    helpLinkText: 'Help with adding or updating parenting activities',
    uploadLabel: 'Upload activities with CSV',
    downloadLabel: 'Download activities as CSV',
    emptyText: "No activities added yet. Click 'Add activity' to get started.",
    instructionText: 'You can submit parenting activity details in one of two ways:',
    csvNoun: 'parenting activities',
    formIntroText: 'Tell us about the parenting activity you plan to carry out in the next year. This information will be included in your annual Delivery Plan submission.',
  },
  'hle': {
    activityNoun: 'activity',
    activityNounPlural: 'activities',
    listHeading: 'HLE activities',
    addButtonText: 'Add activity',
    helpLinkText: 'Help with adding or updating HLE activities',
    uploadLabel: 'Upload activities with CSV',
    downloadLabel: 'Download activities as CSV',
    emptyText: "No activities added yet. Click 'Add activity' to get started.",
    instructionText: 'You can submit HLE activity details in one of two ways:',
    csvNoun: 'HLE activities',
    formIntroText: 'Tell us about the HLE activity you plan to carry out in the next year. This information will be included in your annual Delivery Plan submission.',
  },
  'send': {
    activityNoun: 'activity',
    activityNounPlural: 'activities',
    listHeading: 'SEND activities',
    addButtonText: 'Add activity',
    helpLinkText: 'Help with adding or updating SEND activities',
    uploadLabel: 'Upload activities with CSV',
    downloadLabel: 'Download activities as CSV',
    emptyText: "No activities added yet. Click 'Add activity' to get started.",
    instructionText: 'You can submit SEND activity details in one of two ways:',
    csvNoun: 'SEND activities',
    formIntroText: 'Tell us about the SEND activity you plan to carry out in the next year. This information will be included in your annual Delivery Plan submission.',
  },
  'pnmh-and-pir': {
    activityNoun: 'activity',
    activityNounPlural: 'activities',
    listHeading: 'PNMH and PIR activities',
    addButtonText: 'Add activity',
    helpLinkText: 'Help with adding or updating PNMH and PIR activities',
    uploadLabel: 'Upload activities with CSV',
    downloadLabel: 'Download activities as CSV',
    emptyText: "No activities added yet. Click 'Add activity' to get started.",
    instructionText: 'You can submit PNMH and PIR activity details in one of two ways:',
    csvNoun: 'PNMH and PIR activities',
    formIntroText: 'Tell us about the PNMH and PIR activity you plan to carry out in the next year. This information will be included in your annual Delivery Plan submission.',
  },
  'infant-feeding': {
    activityNoun: 'activity',
    activityNounPlural: 'activities',
    listHeading: 'Infant feeding activities',
    addButtonText: 'Add activity',
    helpLinkText: 'Help with adding or updating infant feeding activities',
    uploadLabel: 'Upload activities with CSV',
    downloadLabel: 'Download activities as CSV',
    emptyText: "No activities added yet. Click 'Add activity' to get started.",
    instructionText: 'You can submit infant feeding activity details in one of two ways:',
    csvNoun: 'infant feeding activities',
    formIntroText: 'Tell us about the infant feeding activity you plan to carry out in the next year. This information will be included in your annual Delivery Plan submission.',
  },
  'healthy-babies-offer': {
    activityNoun: 'activity',
    activityNounPlural: 'activities',
    listHeading: 'Healthy babies activities',
    addButtonText: 'Add activity',
    helpLinkText: 'Help with adding or updating healthy babies activities',
    uploadLabel: 'Upload activities with CSV',
    downloadLabel: 'Download activities as CSV',
    emptyText: "No activities added yet. Click 'Add activity' to get started.",
    instructionText: 'You can submit healthy babies activity details in one of two ways:',
    csvNoun: 'healthy babies activities',
    formIntroText: 'Tell us about the Healthy Babies Offer activity you plan to carry out in the next year. This information will be included in your annual Delivery Plan submission.',
  },
  'rollover-spend': {
    activityNoun: 'activity',
    activityNounPlural: 'activities',
    listHeading: 'Rollover spend',
    addButtonText: 'Add activity',
    helpLinkText: 'Help with adding or updating rollover spend activities',
    uploadLabel: 'Upload activities with CSV',
    downloadLabel: 'Download activities as CSV',
    emptyText: "No activities added yet. Click 'Add activity' to get started.",
    instructionText: 'You can submit rollover spend activity details in one of two ways:',
    csvNoun: 'rollover spend activities',
    formIntroText: 'Tell us about the rollover spend activity you plan to carry out. This information will be included in your annual Delivery Plan submission.',
  },
};

export const DEFAULT_CONTENT: ModuleContent = {
  activityNoun: 'activity',
  activityNounPlural: 'activities',
  listHeading: 'Activities',
  addButtonText: 'Add activity',
  helpLinkText: 'Help with adding or updating activities',
  uploadLabel: 'Upload with CSV',
  downloadLabel: 'Download copy in CSV',
  emptyText: "No activities added yet. Click 'Add activity' to get started.",
  instructionText: 'You can submit activity details in one of two ways:',
  csvNoun: 'activities',
  formIntroText: 'Tell us about the activity you plan to carry out in the next year. This information will be included in your annual Delivery Plan submission.',
};

export const getModuleContent = (moduleCode: string | undefined): ModuleContent =>
  moduleCode ? (MODULE_CONTENT[moduleCode] ?? DEFAULT_CONTENT) : DEFAULT_CONTENT;