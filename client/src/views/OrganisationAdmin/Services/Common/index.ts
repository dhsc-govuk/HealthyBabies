import { ServiceDto, ServiceFormQuestionDto, ServiceFormQuestionType, AnswerInputRequest, DateValidationRule } from '../../../../components/Global/Services';
import { encodeNullableForWaf } from '../../../../helpers/stringUtils';
import { evaluateConditionalRules } from '../../../../helpers/conditionalRules';
import { validateDateParts } from '../../../../helpers/validateDate';

// A question is visible when its rich conditional_rules pass (if set), otherwise it falls back to the
// legacy single-parent conditionalQuestionCode/conditionalValue check. No conditions => always visible.
export const isServiceQuestionVisible = (question: ServiceFormQuestionDto, answers: Record<string, string | undefined>): boolean => {
  if (question.conditionalRules) {
    return evaluateConditionalRules(question.conditionalRules, answers);
  }
  if (question.conditionalQuestionCode && question.conditionalValue) {
    const parentValue = answers[question.conditionalQuestionCode] || '';
    const conditionalValues = question.conditionalValue.split(',').map((v) => v.trim());
    const parentSelectedValues = parentValue.split(',').map((v) => v.trim());
    return conditionalValues.some((cv) => parentSelectedValues.includes(cv));
  }
  return true;
};

// isServiceQuestionVisible only looks at the immediate parent, so a stored answer for a question that
// is itself hidden would keep its own children alive: SMD08=parenting -> SMD10=exemption_other ->
// SMD10a, then SMD08 flips to infant_feeding. SMD10 goes hidden but its answer lingers, so SMD10a
// stays required with nothing on screen to answer it (BLUM-7221). Dropping hidden questions' answers
// makes that cascade. Pruning happens at read time, not in the reducer, so flipping SMD08 back to
// parenting restores the original SMD10 answer instead of destroying it.
export const getEffectiveServiceAnswers = (
  questions: ServiceFormQuestionDto[],
  answers: Record<string, string | undefined>
): Record<string, string | undefined> => {
  // displayOrder restarts on each step, so fold the step in to get a single form-wide ordering. This
  // matches the ordering the Admin question builder validates conditional references against.
  const formOrderOf = (question: ServiceFormQuestionDto) => question.step * 1000 + question.displayOrder;
  const ordered = [...questions].sort((a, b) => formOrderOf(a) - formOrderOf(b));
  const effective = { ...answers };

  // One ordered pass settles a chain whose conditions only reference earlier questions, which the
  // rules builder enforces for conditionalRules. The legacy "Indent under question" dropdown does not,
  // so repeat until a pass changes nothing. Answers are put back as well as taken away: a notEquals
  // rule becomes true when the question it references is itself pruned, and dropping an answer that
  // the form is about to show again would report it as missing while the field still displays a value.
  // The pass count is capped because a negated rule can oscillate rather than settle.
  for (let pass = 0; pass < ordered.length; pass++) {
    let changed = false;
    for (const question of ordered) {
      const visible = isServiceQuestionVisible(question, effective);
      const isPresent = effective[question.code] !== undefined;

      if (!visible && isPresent) {
        delete effective[question.code];
        changed = true;
      } else if (visible && !isPresent && answers[question.code] !== undefined) {
        effective[question.code] = answers[question.code];
        changed = true;
      }
    }
    if (!changed) break;
  }

  return effective;
};

export interface ServiceFormState {
  id?: string;
  name: string;
  answers: Record<string, string | undefined>;
}

export enum ServiceReducerAction {
  SET_NAME,
  SET_ANSWER,
  INIT,
  INIT_FROM_SERVICE,
}

export const initialServiceState: ServiceFormState = {
  name: '',
  answers: {},
};

export const serviceReducer = (state: ServiceFormState, action: { type: ServiceReducerAction; payload?: any }): ServiceFormState => {
  switch (action.type) {
    case ServiceReducerAction.SET_NAME:
      return { ...state, name: action.payload };
    case ServiceReducerAction.SET_ANSWER:
      return {
        ...state,
        answers: {
          ...state.answers,
          [action.payload.code]: action.payload.value,
        },
      };
    case ServiceReducerAction.INIT:
      return { ...initialServiceState };
    case ServiceReducerAction.INIT_FROM_SERVICE:
      return action.payload as ServiceFormState;
    default:
      throw new Error();
  }
};

export interface ValidationErrors {
  name?: string;
  [questionCode: string]: string | undefined;
}

export const shouldSkipServiceCharacteristics = (state: ServiceFormState): boolean => state.answers.SMD03 === 'no_longer_offered';

// `questions` are the ones being validated (a single step); `allQuestions` is the whole form and
// drives the visibility cascade, since a parent may live on an earlier step. It defaults to
// `questions` so a caller that omits it still gets the cascade for chains contained in one step.
export const validateStep = (
  state: ServiceFormState,
  questions: ServiceFormQuestionDto[],
  allQuestions: ServiceFormQuestionDto[] = questions
): ValidationErrors => {
  const errors: ValidationErrors = {};
  const effectiveAnswers = getEffectiveServiceAnswers(allQuestions, state.answers);

  // Validate name (always required, always first in step 1)
  if (!state.name || state.name.trim() === '') {
    errors.name = 'Please provide a service name';
  }

  // Validate each required question
  for (const question of questions) {
    if (!question.isRequired || !question.isActive) continue;

    // Skip SMD01 (name) as it's handled separately
    if (question.code === 'SMD01') continue;

    // Skip validation for questions hidden by their conditional rules, or by a hidden ancestor
    if (!isServiceQuestionVisible(question, effectiveAnswers)) {
      continue;
    }

    const value = effectiveAnswers[question.code];

    if (value === undefined || value === null || value === '') {
      errors[question.code] = getErrorMessage(question);
      continue;
    }

    // Structural validation always runs — see the note in Locations/Common. The rule only
    // selects the past/future policy; an unset rule defaults to past-only.
    if (question.questionType === ServiceFormQuestionType.Date) {
      const [day, month, year] = value.split('/');
      const result = validateDateParts(day, month, year, {
        allowFuture: question.dateValidationRule === DateValidationRule.NotInPast,
        disallowPast: question.dateValidationRule === DateValidationRule.NotInPast,
        legendText: question.label,
      });
      if (result.error) {
        errors[question.code] = result.error;
      }
    }
  }

  const ageRangeError = validateAgeRange(effectiveAnswers, questions);
  if (ageRangeError) {
    errors.SMD14 = ageRangeError;
  }

  return errors;
};

const validateAgeRange = (answers: Record<string, string | undefined>, questions: ServiceFormQuestionDto[]): string | undefined => {
  const lowestValue = answers.SMD13;
  const highestValue = answers.SMD14;

  if (!lowestValue || !highestValue || lowestValue.toLowerCase() === 'unknown' || highestValue.toLowerCase() === 'unknown') {
    return undefined;
  }

  const lowestQuestion = questions.find((q) => q.code === 'SMD13');
  const highestQuestion = questions.find((q) => q.code === 'SMD14');

  const lowestOrder = lowestQuestion?.options.find((o) => o.value.toLowerCase() === lowestValue.toLowerCase())?.displayOrder;
  const highestOrder = highestQuestion?.options.find((o) => o.value.toLowerCase() === highestValue.toLowerCase())?.displayOrder;

  if (lowestOrder === undefined || highestOrder === undefined || !lowestQuestion || !highestQuestion) {
    return undefined;
  }

  if (lowestOrder > highestOrder) {
    return `'${highestQuestion.label}' cannot be lower than '${lowestQuestion.label}'.`;
  }

  return undefined;
};

const getErrorMessage = (question: ServiceFormQuestionDto): string => {
  switch (question.questionType) {
    case ServiceFormQuestionType.Text:
      return `Please enter ${question.label.toLowerCase()}`;
    case ServiceFormQuestionType.Radio:
      return `Please select an option for "${question.label}"`;
    case ServiceFormQuestionType.Checkbox:
      return `Please select at least one option for "${question.label}"`;
    case ServiceFormQuestionType.Select:
      return `Please select an option for "${question.label}"`;
    case ServiceFormQuestionType.Date:
      return `Please enter a date for "${question.label}"`;
    default:
      return `Please answer "${question.label}"`;
  }
};

export const mapServiceToFormState = (service: ServiceDto): ServiceFormState => {
  const answers: Record<string, string | undefined> = {};

  for (const answer of service.answers) {
    answers[answer.questionCode] = answer.value ?? undefined;
  }

  return {
    id: service.id,
    name: service.name,
    answers,
  };
};

// See validateStep for what `allQuestions` is for.
export const buildAnswersForRequest = (
  state: ServiceFormState,
  questions: ServiceFormQuestionDto[],
  allQuestions: ServiceFormQuestionDto[] = questions
): AnswerInputRequest[] => {
  const answers: AnswerInputRequest[] = [];
  const effectiveAnswers = getEffectiveServiceAnswers(allQuestions, state.answers);

  for (const question of questions) {
    // Skip SMD01 (name) as it's handled separately
    if (question.code === 'SMD01') continue;

    const value = effectiveAnswers[question.code];

    // Don't include answers for questions hidden by their conditional rules, or by a hidden ancestor
    if (!isServiceQuestionVisible(question, effectiveAnswers)) {
      continue;
    }

    answers.push({
      questionCode: question.code,
      value: encodeNullableForWaf(value) ?? undefined,
    });
  }

  return answers;
};
